using UnityEngine;
using System.Collections;
using System;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net.NetworkInformation;

namespace VRTracker.Manager
{
	/// <summary>
	/// VR Tracker
	/// Script handling the udp connection data
	/// </summary>
    public class VRT_UDPClient : MonoBehaviour {

        private VRTracker.Manager.VRT_Manager vrtrackerManager;
		Thread receiveThread;
		UdpClient client;
		private int port = 7778; // port used
		string hostname;		
        bool connected = false;

        private bool askedToClose = false; // Bool set to true when we want to close the socket and avoid poping an Error message 

		public void Start()
		{
			Application.runInBackground = true;
			Init();
            if (vrtrackerManager == null)
                vrtrackerManager = VRTracker.Manager.VRT_Manager.Instance;
		}

		// init
		private void Init()
		{
            askedToClose = false;
			receiveThread = new Thread( new ThreadStart(ReceiveData));
			receiveThread.IsBackground = true;
			receiveThread.Start();
		}

		private void ReceiveData()
		{
            client = new UdpClient(port);
            connected = true;
			while (connected)
			{
				try
				{
					IPEndPoint anyIP = new IPEndPoint(IPAddress.Broadcast, port);
					byte[] data = client.Receive(ref anyIP);
					ParseData(data);
                    /*Debug.Log("Data length " + data.Length);
					string text = ByteArrayToString(data);
                    Debug.Log(text);*/
				}
				catch (Exception err)
				{
                    //Close connection in case of error
                    if(!askedToClose)
                    Debug.LogError("Error with UDP reception : " + err.ToString());
                    connected = false;
                    if (client != null)
                        client.Close();
				}
			}
		}


		/// <summary>
		/// Parses the data received 
		/// </summary>
		/// <param name="data">Data.</param>
		private void ParseData(byte[] data){
			byte[] uid = { data [0], data [1] };
			VRTracker.Manager.VRT_Tag tag = FindTag (uid);
			if (tag == null){
				return;
			}
			tag.OnTagData (data);
		}

		private VRTracker.Manager.VRT_Tag FindTag(byte[] tagID){
			if (tagID.Length != 2) {
				Debug.LogWarning ("Tag ID != 2 bytes");
				return null;
			}

			StringBuilder hex = new StringBuilder(tagID.Length * 2);
			hex.AppendFormat("{0:x2}", tagID[0]);
			hex.Append (":");
			hex.AppendFormat("{0:x2}", tagID[1]);
			string id = hex.ToString();
			id.ToLower ();
            foreach (VRTracker.Manager.VRT_Tag tag in vrtrackerManager.tags) {
				if (tag.UID.EndsWith (id)){
					return tag;
					}
			}
			return null;
		}

		
		void OnDisable()
		{
            askedToClose = true;
            if (receiveThread != null)
            {
                connected = false;
                client.Close();
                receiveThread.Abort();
            }
            if(client != null)
            {
                client.Close();
            }

        }        
		/// <summary>
		/// Transforms the Byte array to string.
		/// </summary>
		/// <returns>The array to string.</returns>
		/// <param name="ba">Ba.</param>
        public static string ByteArrayToString(byte[] ba)
		{
			StringBuilder hex = new StringBuilder(ba.Length * 2);
			foreach (byte b in ba)
				hex.AppendFormat("{0:x2}", b);
			return hex.ToString();
		}
	}
}
