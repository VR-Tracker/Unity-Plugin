using UnityEngine;
using System.Collections.Generic;
using System;
using WebSocketSharp;
using System.Text.RegularExpressions;
using UnityEngine.Networking;
using VRTracker.Network;
using System.Collections;
using VRTracker.Boundary;
using UnityEngine.Networking;
using System.Net;
using System.Net.NetworkInformation;

namespace VRTracker.Manager
{
	/// <summary>
	/// VR Tracker
	/// This script is the main component of VR Tracker
	/// It holds the websocket, and handle the communication and the treatment of the messages from the gateway
	/// </summary>
    public class VRT_WebsocketClient : MonoBehaviour
    {
        private WebSocket myws;
        private int timestamp = 0;

        private List<string> messagesToSend;

        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnNoGateway; // Event trigger if the Gateway could not be found
        [System.NonSerialized] public string serverIp = "";
        private bool serverIpReceived = false;
        private bool isSocketClosing = false;
        private bool isSocketRunning = false;
        public string localIp; //Store the local address of the device (192.168.42.xxx)
  

        private void Awake()
        {
        }

        // Use this for initialization
        void Start()
        {
            localIp = GetLocalIPAddress ();
            OpenWebsocket();
        }

        // Update is called once per frame
        void Update()
        {

        }

        /// <summary>
        /// Send a command to the Gateway to retreive the list of all available Tags
        /// </summary>
        public void GetAvailableTags()
        {
            myws.SendAsync("cmd=allavailabletag", OnSendComplete);
        }

		/// <summary>
		/// Raises the open handler event.
		/// Called when connection to Gateway is successfull
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
        private void OnOpenHandler(object sender, System.EventArgs e)
        {
            askedToClose = false;
            if (OnConnected != null)
                OnConnected();
            //Debug.LogWarning("COnnection to the gateway");
            myws.SendAsync("cmd=mac&uid=" + VRTracker.Manager.VRT_Manager.Instance.UserUID, OnSendComplete);
            myws.SendAsync("cmd=allavailabletag", OnSendComplete);
            isSocketRunning = true;

			AskServerIP();

            foreach (VRTracker.Manager.VRT_Tag tag in VRTracker.Manager.VRT_Manager.Instance.tags)
            {
                if (tag.UID != "Enter Your Tag UID")
                {
                    AssignTag(tag.UID);
                    //Debug.LogWarning("Assigning tag " + tag.UID);
                }
            }
            GetMagneticOffset();
        }

        private void OnErrorHandler(object sender, System.EventArgs e)
        {
            if(!isSocketClosing)
                Debug.LogError("Error with connection to the gateway");
            if (!isSocketRunning && !isSocketClosing)
            {
                Debug.LogWarning("Trying to reconnect to the gateway");
                UnityMainThreadDispatcher.Instance().Enqueue(TryToReconnect());
            }
        }

        // Handler for all messages from the Gateway
        private void OnMessageHandler(object sender, MessageEventArgs e)
		{
			//Debug.Log(e.Data);
            if (e.Data.Contains("cmd=position"))
            {
                Vector3 position = Vector3.zero;
                Vector3 orientation = Vector3.zero;
                Quaternion orientation_quat = Quaternion.identity;

                string[] datasbytag = e.Data.Split(new string[] { "&uid=" }, System.StringSplitOptions.RemoveEmptyEntries);
                for (int i = 1; i < datasbytag.Length; i++)
                {
                    bool positionUpdated = false;
                    bool orientationUpdated = false;
                    bool orientationQuaternion = false;
                    bool timestampUpdated = false;
                    string[] datas = datasbytag[i].Split('&');
                    string uid = datas[0];
                    foreach (string data in datas)
                    {
                        string[] datasplit = data.Split('=');
                        // Position
                        if (datasplit[0] == "x")
                        {
                            positionUpdated = true;
                            position.x = float.Parse(datasplit[1]);
                        }
                        else if (datasplit[0] == "z")
                        {
                            position.y = float.Parse(datasplit[1]);
                        }
                        else if (datasplit[0] == "y")
                        {
                            position.z = float.Parse(datasplit[1]);
                        }
                        else if (datasplit[0] == "ts")
                        {
                            timestamp = int.Parse(datasplit[1]);
                            timestampUpdated = true;
                        }

                        // Orientation
                        else if (datasplit[0] == "ox")
                        {
                            orientationUpdated = true;
                            orientation.y = -float.Parse(datasplit[1]);
                            orientation_quat.x = -orientation.y;
                        }
                        else if (datasplit[0] == "oy")
                        {
                            orientation.x = -float.Parse(datasplit[1]);
                            orientation_quat.y = -orientation.x;
                        }
                        else if (datasplit[0] == "oz")
                        {
                            orientation.z = float.Parse(datasplit[1]);
                            orientation_quat.z = orientation.z;
                        }
                        else if (datasplit[0] == "ow")
                        {
                            orientationQuaternion = true;
                            orientation_quat.w = -float.Parse(datasplit[1]);
                        }
                    }
                    foreach (VRTracker.Manager.VRT_Tag tag in VRTracker.Manager.VRT_Manager.Instance.tags)
                    {
                        if (tag.UID == uid)
                        {
                            if (orientationUpdated)
                            {
                                if (orientationQuaternion)
                                    tag.UpdateOrientationQuat(orientation_quat);
                                else
                                    tag.UpdateOrientation(orientation);
                            }
                            if (positionUpdated)
                            {
                                if (!timestampUpdated)
                                    tag.UpdatePosition(position);
                                else
                                    tag.UpdatePosition(position, timestamp);

                            }
                        }
                    }
                }
            }
            else if (e.Data.Contains("cmd=specialcmd"))
            {
                string[] datas = e.Data.Split('&');
                string uid = null;
                string command = null;
                foreach (string data in datas)
                {
                    string[] datasplit = data.Split('=');

                    // Tag UID sending the special command
                    if (datasplit[0] == "uid")
                    {
                        uid = datasplit[1];
                    }

                    // Special Command name
                    else if (datasplit[0] == "data")
                    {
                        command = datasplit[1];
                    }
                }
                if (uid != null && command != null)
                    ReceiveSpecialCommand(uid, command);

            }
			else if (e.Data.Contains("cmd=taginfos"))
			{
				string[] datas = e.Data.Split('&');

				string uid = null;
				string status = null;
				int battery = 0;
                string version = "";
				foreach (string data in datas)
				{
					string[] datasplit = data.Split('=');

					// Tag UID sending its informations
					if (datasplit[0] == "uid")
					{
						uid = datasplit[1];
					}
					// Tag status (“lost”, “tracking”, “unassigned”)
					else if (datasplit[0] == "status")
					{
						status = datasplit[1];
					}
					// Tag battery
					else if (datasplit[0] == "battery")
					{
						battery = int.Parse(datasplit[1]);
					}
                    // Tag version
                    else if (datasplit[0] == "version")
                    {
                        version = datasplit[1];
                    }
				}
				if (uid != null && status != null)
				{
                    ReceiveTagInformations(uid, status, battery, version);
				}

			}
            else if (e.Data.Contains("cmd=tag"))
            { // Tag V2 data
                string[] datas = e.Data.Split('&');
                string uid = null;
                foreach (string data in datas)
                {
                    string[] datasplit = data.Split('=');

                    // Tag UID sending the special command
                    if (datasplit[0] == "uid")
                    {
                        uid = datasplit[1];
                    }

                }
                if (uid != null)
                    ReceiveSpecialData(uid, e.Data);
            }
            else if (e.Data.Contains("cmd=error"))
            {
                // TODO Parse differnt kinds of errors
                Debug.LogWarning("VR Tracker : " + e.Data);
                if (e.Data.Contains("needmacadress"))
                {
                    myws.SendAsync("cmd=mac&uid=" + VRTracker.Manager.VRT_Manager.Instance.UserUID, OnSendComplete);
                    foreach (VRTracker.Manager.VRT_Tag tag in VRTracker.Manager.VRT_Manager.Instance.tags)
                    {
                        if (tag.UID != "Enter Your Tag UID")
                            AssignTag(tag.UID);
                    }
                }
            }
            else if (e.Data.Contains("function=needaddress"))
            {
                ReceiveAskServerIP();
            }
            //if the message gives us an IP address, try to connect as a client to it
            else if (e.Data.Contains("function=address"))
            {

                string[] datas = e.Data.Split('&');
                foreach (string data in datas)
                {
                    string[] infos = data.Split('=');
                    if (infos[0] == "ip")
                    {
						//Debug.Log("IP received " + infos[1]);
                        ReceiveServerIP(infos[1]);
                    }
                }
            }
            else if (e.Data.Contains("cmd=availabletag"))
            {
                string[] datas = e.Data.Split('&');

                // Verify if Tags connected to the system can be assoicated to the User from association File
                foreach (string data in datas)
                {
                    string[] datasplit = data.Split('=');
                    if (datasplit[0].Contains("tag"))
                    {
                        VRTracker.Manager.VRT_Manager.Instance.AddAvailableTag(datasplit[1]);
                    }
                }
            }
            else if (e.Data.Contains("cmd=reoriente"))
            {
                string uid = null;
                string[] datas = e.Data.Split('&');

                foreach (string data in datas)
                {
                    string[] datasplit = data.Split('=');
                    // Tag UID sending the special command
                    if (datasplit[0] == "uid")
                    {
                        uid = datasplit[1];
                    }
                }
                foreach (VRTracker.Manager.VRT_Tag tag in VRTracker.Manager.VRT_Manager.Instance.tags)
                {
                    if (tag.UID == uid)
                    {
                        Debug.Log("Resetting orientation after receiving message");
                        tag.ResetOrientation();
                    }
                }
            }
            else if (e.Data.Contains("cmd=offset"))
            {
                string[] datas = e.Data.Split('&');

                foreach (string data in datas)
                {
                    string[] datasplit = data.Split('=');
                    // Tag UID sending the special command
                    if (datasplit[0] == "oy")
                    {
                        float f;

                        // Update rotation offset only if not null
                        if (float.TryParse(datasplit[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out f))
                            VRTracker.Manager.VRT_Manager.Instance.roomNorthOffset = f;
                    }
                }
            }
            else if (e.Data.Contains("cmd=boundaries"))
            {
                Debug.LogWarning(e.Data);
                string[] datas = e.Data.Split('&');

                float xmin = 0.0f;
                float ymin = 0.0f;
                float xmax = 0.0f;
                float ymax = 0.0f;
				bool newBoundary = false;
                foreach (string data in datas)
                {
                    string[] datasplit = data.Split('=');
                    // Tag UID sending the special command
                    if (datasplit[0] == "xmin")
                    {
                        xmin = float.Parse(datasplit[1]);
						newBoundary = true;
                    }
                    else if (datasplit[0] == "xmax")
                    {
                        xmax = float.Parse(datasplit[1]);
                    }
                    else if (datasplit[0] == "ymin")
                    {
                        ymin = float.Parse(datasplit[1]);
                    }
                    else if (datasplit[0] == "ymax")
                    {
                        ymax = float.Parse(datasplit[1]);
                    }
                    else if (datasplit[0] == "data")
                    {
                        ReceiveBoundaries(datasplit[1]);
                    }
                }

                //Handle boundaries
				if(newBoundary)
					CreateNewBoundaries(xmin, xmax, ymin, ymax);
            }
            else if (e.Data.Contains("cmd=newtag") || e.Data.Contains("cmd=rectag"))
            {
                //TODO Improve implementation
                string[] datas = e.Data.Split('&');
                foreach (string data in datas)
                {
                    string[] datasplit = data.Split('=');
                    if (datasplit[0].Contains("uid"))
                    {
                        foreach (VRTracker.Manager.VRT_Tag tag in VRTracker.Manager.VRT_Manager.Instance.tags)
                        {
                            if (tag.UID == datasplit[1])
                            {
                                Debug.Log("Reassigning Tag " + tag.UID);
                                AssignTag(tag.UID);
                                break;
                            }
                        }
                    }
                }
            }
            //if the message gives us an IP address, try to connect as a client to it
            else if (e.Data.Contains("function=address"))
            {

                string[] datas = e.Data.Split('&');
                foreach (string data in datas)
                {
                    string[] infos = data.Split('=');
                    if (infos[0] == "ip")
                    {
                        ReceiveServerIP(infos[1]);
                    }
                }
            }
            else
            {
                //	Debug.LogWarning("VR Tracker : Unknown data received : " + e.Data);
            }
        }

        // Called when connection to Gateway is closed
        private void OnCloseHandler(object sender, CloseEventArgs e)
        {
            Debug.LogWarning("VR Tracker closing websocket " + e.ToString());
            if (isSocketClosing)
            {
                if (OnDisconnected != null)
                    OnDisconnected();
            }
            else
            {
                isSocketRunning = false;
                UnityMainThreadDispatcher.Instance().Enqueue(TryToReconnect());
            }

        }

        private void OnSendComplete(bool success)
        {

        }
			
		/// <summary>
		/// Opens the websocket connection with the Gateway
		/// </summary>
        private void OpenWebsocket()
        {
            myws = new WebSocket("ws://192.168.42.1:7777/user/");
            myws.OnOpen += OnOpenHandler;
            myws.OnMessage += OnMessageHandler;
            myws.OnClose += OnCloseHandler;
            myws.OnError += OnErrorHandler;
            myws.ConnectAsync();
        }
			
		/// <summary>
		/// Closes the websocket connection to the Gateway
		/// </summary>
        private void CloseWebsocket()
        {
            if (isSocketRunning)
            {
                isSocketClosing = true;
                if (myws != null)
                    myws.Close();
            }
            else
            {
                Debug.LogError("Websocket is already down ! ");
            }

        }
			
		/// <summary>
		/// Sends your Unique ID, it can be your MAC address for
		/// example but avoid the IP. It will be used by the Gateway
		/// to identify you over the network. It is necessary on multi-gateway
		/// configuration
		/// </summary>
		/// <param name="uid">Uid.</param>
        private void SendMyUID(string uid)
        {
            myws.SendAsync(uid, OnSendComplete);

        }
			
		/// <summary>
		/// Asks the gateway to assign a specific Tag to this device.
		/// Assigned Tags will then send their position to this device.
		/// </summary>
		/// <param name="TagID">Tag I.</param>
        public void AssignTag(string TagID)
        {
            myws.SendAsync("cmd=tagassign&uid=" + TagID, OnSendComplete);
        }
			
		/// <summary>
		/// Asks the gateway to assign a Tag to this device.
		/// Assigned Tags will then send their position to this device.
		/// </summary>
        public void AssignATag()
        {
            myws.SendAsync("cmd=AssignATag", OnSendComplete);
        }
			
		/// <summary>
		/// Asks the gateway to UNassign a specific Tag from this device.
		/// You will stop receiving updates from this Tag.
		/// </summary>
		/// <param name="TagID">Tag I.</param>
        public void UnassignTag(string TagID)
        {
            myws.SendAsync("cmd=tagunassign&uid=" + TagID, OnSendComplete);
        }
			
		/// <summary>
		/// Asks the gateway to Unassign all Tags from this device.
		/// You will stop receiving updates from all the assigned tags
		/// </summary>
        public void UnassignAllTags()
        {
            myws.SendAsync("cmd=tagunassignall", OnSendComplete);
        }
			
		/// <summary>
		/// Ask for informations on a specific Tag
		/// You will receive the id, battery level, version of the tag
		/// </summary>
		/// <param name="TagID">Tag I.</param>
        public void GetTagInformations(string TagID)
        {
            myws.SendAsync("cmd=taginfos&uid=" + TagID, OnSendComplete);
        }
			
		/// <summary>
		/// Enables or Disables orientation for a Tag
		/// </summary>
		/// <param name="TagID">Tag I.</param>
		/// <param name="enable">If set to <c>true</c> enable.</param>
        public void EnableTagOrientation(string TagID, bool enable)
        {
            string en = "";
            if (enable)
            {
                en = "true";
            }
            else
            {
                en = "false";
            }

            myws.SendAsync("cmd=orientation&orientation=" + en + "&uid=" + TagID, OnSendComplete);
        }
			
		/// <summary>
		/// Sets a specific color on the Tag
		/// </summary>
		/// <param name="TagID">Tag I.</param>
		/// <param name="red">Red (0-255)</param>
		/// <param name="green">Green (0-255)</param>
		/// <param name="blue">Blue (0-255)</param>
        public void SetTagColor(string TagID, int red, int green, int blue)
        {
            myws.SendAsync("cmd= color&r=" + red + "&g=" + green + "&b=" + blue + "&uid=" + TagID, OnSendComplete);
        }
			
		/// <summary>
		/// Sends the tag command across the network
		/// </summary>
		/// <param name="TagID">Tag I.</param>
		/// <param name="command">Command.</param>
        public void SendTagCommand(string TagID, string command)
        {
            myws.SendAsync("cmd=specialcmd&uid=" + TagID + "&data=" + command, OnSendComplete);
        }
			
		/// <summary>
		/// Sends special command to the gateway that will be broadcast to all others users
		/// </summary>
		/// <param name="command">Command.</param>
        public void SendSpecialData(string command)
        {
            Debug.Log("VR Tracker : " + command);
            myws.SendAsync("cmd=specialdata&data=" + command, OnSendComplete);
        }
			
		/// <summary>
		/// Sends the user battery level to the Gateway
		/// </summary>
		/// <param name="battery">Battery (0-100)</param>
        public void SendUserBattery(int battery)
        {
            myws.SendAsync("cmd=usrbattery&battery=" + battery, OnSendComplete);
        }

		/// <summary>
		/// For Multiplayer, we ask all other user if the know the Server IP
		/// </summary>
        public void AskServerIP()
        {
            myws.SendAsync("cmd=specialdata&function=needaddress", OnSendComplete);

        }

		/// <summary>
		/// Sends the server Ip, only the server will answer the server ip request
		/// </summary>
		/// <param name="ip">Ip.</param>
        public void SendServerIP(string ip)
        {
            myws.SendAsync("cmd=specialdata&function=address&ip=" + ip, OnSendComplete);
        }

		/// <summary>
		/// The server IP was sent to us by another user (typically the server is the only one responding)
		/// </summary>
		/// <param name="ip">Ip.</param>
        private void ReceiveServerIP(string ip)
        {
			if (!serverIpReceived && ip != localIp)
            {
                serverIp = ip;
                serverIpReceived = true;
            }
        }

		/// <summary>
		/// Another user is looking for the Server and asks if we know the IP
		/// </summary>
        private void ReceiveAskServerIP()
        {
			if (serverIp != "" && !serverIpReceived)
            {
                SendServerIP(serverIp);
            }
        }

		/// <summary>
		/// Sends the message to gateway.
		/// </summary>
		/// <param name="message">Message.</param>
        public void SendMessageToGateway(string message)
        {
            myws.SendAsync(message, OnSendComplete);
        }

		/// <summary>
		/// Looks for function in the message
		/// </summary>
		/// <returns>The for function.</returns>
		/// <param name="message">Message.</param>
        public static string LookForFunction(string message)
        {
            string[] datas = message.Split('&');
            foreach (string data in datas)
            {
                string[] infos = data.Split('=');
                if (infos[0] == "function")
                {
                    return infos[1];
                }
            }
            return null;
        }
			
		/// <summary>
		/// Receives a special command and notify the corresponding tag
		/// </summary>
		/// <param name="TagID">Tag I.</param>
		/// <param name="data">Data.</param>
        public void ReceiveSpecialCommand(string TagID, string data)
        {
            // TODO: You can do whatever you wants with the special command, have fun !
            bool tagFound = false;
            // Search for the Tag the special command is sent to
            foreach (VRTracker.Manager.VRT_Tag tag in VRTracker.Manager.VRT_Manager.Instance.tags)
            {
                if (tag.UID == TagID)
                {
                    tagFound = true;
                    tag.OnSpecialCommand(data);
                }
            }
            // If the Tag was not found, the command is sent to all Tags
            if (!tagFound)
            {
                foreach (VRTracker.Manager.VRT_Tag tag in VRTracker.Manager.VRT_Manager.Instance.tags)
                {
                    tag.OnSpecialCommandToAll(TagID, data);
                }
            }

        }
			
		/// <summary>
		/// Receives the special data for a specific tag and notify the corresponding tag with id "tagID"
		/// </summary>
		/// <param name="TagID">Tag I.</param>
		/// <param name="data">Data.</param>
        public void ReceiveSpecialData(string TagID, string data)
        {
            // TODO: You can do whatever you wants with the special command, have fun !

            // Search for the Tag the special command is sent to
            foreach (VRTracker.Manager.VRT_Tag mTag in VRTracker.Manager.VRT_Manager.Instance.tags)
            {
                if (mTag.UID == TagID)
                {
                    mTag.OnTagData(data);
                }
            }
        }
			
		/// <summary>
		/// Receives the tag informations
		/// </summary>
		/// <param name="TagID">Tag I.</param>
		/// <param name="status">Status.</param>
		/// <param name="battery">Battery.</param>
        public void ReceiveTagInformations(string TagID, string status, int battery, string version)
        {
            foreach (VRTracker.Manager.VRT_Tag tag in VRTracker.Manager.VRT_Manager.Instance.tags)
            {
                if (tag.UID == TagID)
                {
                    tag.UpdateTagInformations(status, battery, version);
                }
            }
        }
			
		/// <summary>
		/// Receives the boundaries and update the boundaries data
		/// </summary>
		/// <param name="boundariesData">Boundaries data.</param>
        public void ReceiveBoundaries(string boundariesData)
        {
			UnityMainThreadDispatcher.Instance().Enqueue(UpdateBoundaries(boundariesData));
        }

		/// <summary>
		/// Updates the boundaries from the data received from the network
		/// </summary>
		/// <returns>The boundaries.</returns>
		/// <param name="boundariesData">Boundaries data.</param>

        IEnumerator UpdateBoundaries(string boundariesData)
        {
			VRT_RoomBoundarySystem.SaveBoundaries (boundariesData);
            yield return null;
        }

		/// <summary>
		/// Creates the new boundaries from the data received from the gateway boundaries file or dashboard
		/// </summary>
		/// <param name="xmin">Xmin.</param>
		/// <param name="xmax">Xmax.</param>
		/// <param name="ymin">Ymin.</param>
		/// <param name="ymax">Ymax.</param>
		public void CreateNewBoundaries(float xmin, float xmax, float ymin, float ymax)
		{
			UnityMainThreadDispatcher.Instance().Enqueue(CreateBoundaries(xmin, xmax, ymin, ymax));
		}

		/// <summary>
		/// Creates the boundaries from the limit data of the play area
		/// </summary>
		/// <returns>The boundaries.</returns>
		/// <param name="xmin">Xmin.</param>
		/// <param name="xmax">Xmax.</param>
		/// <param name="ymin">Ymin.</param>
		/// <param name="ymax">Ymax.</param>
		IEnumerator CreateBoundaries(float xmin, float xmax, float ymin, float ymax)
		{
			VRT_RoomBoundarySystem.CreateBoundariesFromLimit (xmin, xmax, ymin, ymax);
			yield return null;
		}

		/// <summary>
		/// Raises the application quit event.
		/// Ensure the Websocket is correctly closed on application quit
		/// </summary>
        void OnApplicationQuit()
        {
            CloseWebsocket();
        }

       	/// <summary>
       	/// Gets the magnetic offset.
		/// Ask the gateway for the rotation offset between true magnetic North and room forward axis
		///  (Y in VR Tracker coordinates, Z in Unity coordinates)
       	/// </summary>
		public void GetMagneticOffset()
        {
            myws.SendAsync("cmd=getoffset", OnSendComplete);
        }

        public void OnDestroy()
        {
            CloseWebsocket();
        }


		/// <summary>
		/// Tries to reconnect.
		/// Reconnects the websocket if the gateway goes down
		/// </summary>
		/// <returns>The to reconnect.</returns>
        private IEnumerator TryToReconnect()
        {
            yield return new WaitForSeconds(5);
            myws.ConnectAsync();
        }

		/// <summary>
		/// Gets the local IP address of the device
		/// </summary>
		/// <returns>The local IP address.</returns>
        private string GetLocalIPAddress()
        {

            bool addressFound = false;
            string myaddress = "";
            // Get a list of all network interfaces (usually one per network card, dialup, and VPN connection)
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface network in networkInterfaces)
            {
                // Read the IP configuration for each network
                IPInterfaceProperties properties = network.GetIPProperties();

                // Each network interface may have multiple IP addresses
                foreach (IPAddressInformation address in properties.UnicastAddresses)
                {
                    // We're only interested in IPv4 addresses for now
                    if (address.Address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                        continue;

                    // Ignore loopback addresses (e.g., 127.0.0.1)
                    if (IPAddress.IsLoopback(address.Address))
                        continue;

                    if (address.Address.ToString().StartsWith("192.168.42."))
                    {
                        myaddress = address.Address.ToString();
                        addressFound = true;
                    }


                }
            }

            if (addressFound)
            {
                Debug.Log("My IP address is : " + myaddress);
                return myaddress;
            }
            else
            {
                Debug.LogError("We could find your IP Address, are you sure to be connected to VR Tracker Gateway ?");
                if (OnNoGateway != null)
                    OnNoGateway("We could find your IP Address, are you sure to be connected to VR Tracker Gateway ?");
                }
            return "";
        }

		/// <summary>
		/// Sets the server ip
		/// Means that the current device is the server
		/// </summary>
        public void SetServerIp()
        {
            serverIp = localIp;
        }
    }
}
