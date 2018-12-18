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
					
                    /*Debug.Log("Data length " + data.Length);
					string text = ByteArrayToString(data);
                    Debug.Log(text);*/
                    ParseData(data);
				}
				catch (Exception err)
				{
                    //Close connection in case of error
                    if (!askedToClose)
                        Debug.LogError("Error with UDP reception : " + err.ToString());
                    else
                    {
                        connected = false;
                        if (client != null)
                            client.Close();
                    }
				}
			}
		}


		/// <summary>
		/// Parses the data received 
		/// </summary>
		/// <param name="data">Data.</param>
		private void ParseData(byte[] data){
			OnTagData (data);
		}


        /// <summary>
        /// Raises the tag data event.
        /// Handle the binary data received from the tag
        /// </summary>
        /// <param name="data">Data.</param>
        public void OnTagData(byte[] data)
        {
            //Debug.Log("UDP size " + data.Length + "  Timestamp " + System.DateTime.Now.Millisecond);
            bool shouldCheckMac = true; // In case of position data, MAC should be checked at the end of each sensor data as multiple Tag are in the same message
            int i = 0;
            byte[] uid = { data[i], data[i + 1] };
            VRTracker.Manager.VRT_Tag currentTag = FindTag(uid);
            bool tagFound = false;
            while (i < data.Length)
            {
                if(shouldCheckMac){
                    uid[0] = data[i];
                    uid[1] = data[i+1];
                    currentTag = FindTag(uid);
                    tagFound = currentTag == null ? false : true;
                    i = i + 2; // Set i to sensor type
                }

                switch (data[i] >> 4 & 0x0F)
                {
                    // Position
                    case 0:
                        {
                            shouldCheckMac = true;
                            int sensorCount = data[i] & 0x0F;
                            byte length = data[i+1];
                            double timestamp = ((double)((data[i + 2] << 8) + data[i + 3]) / 10000.0d);

                            bool isNeg = (data[i + 4] & 0x80) == 0x80 ? true : false;
                            float x = isNeg ? (((data[i + 4] & 0x7F) << 8) + data[i + 5]) - 32768 : (((data[i + 4] & 0x7F) << 8) + data[i + 5]);
                            isNeg = (data[i + 6] & 0x80) == 0x80 ? true : false;
                            float y = isNeg ? (((data[i + 6] & 0x7F) << 8) + data[i + 7]) - 32768 : (((data[i + 6] & 0x7F) << 8) + data[i + 7]);
                            isNeg = (data[i + 8] & 0x80) == 0x80 ? true : false;
                            float z = isNeg ? (((data[i + 8] & 0x7F) << 8) + data[i + 9]) - 32768 : (((data[i + 8] & 0x7F) << 8) + data[i + 9]);
                            Vector3 position = new Vector3(x/1000.0f, z/ 1000.0f, y/ 1000.0f);
                            i += 10;
                            shouldCheckMac = true;

                            // Create Endpoint if inexsiting
                            if (!tagFound || !currentTag.trackedEndpoints.ContainsKey(sensorCount))
                            {
                                Debug.LogWarning("Tag does not exist for message. Tag found ? " + tagFound.ToString());
                            }
                            else
                            {
                               // Debug.Log("Parse positon "  + position.ToString("F4") + "  TS:" + timestamp.ToString());
                                currentTag.trackedEndpoints[sensorCount].UpdatePosition(position, timestamp);
                            }
                            
                            break;
                        }

                    // IMU
                    case 1:
                        {
                            int sensorCount = data[i] & 0x0F;

                            float ox = ((data[i + 4] << 8) + data[i + 5]) / 100;
                            float oy = ((data[i + 6] << 8) + data[i + 7]) / 100;
                            float oz = ((data[i + 8] << 8) + data[i + 9]) / 100;
                            bool isNeg = (data[i + 10] & 0x80) == 0x80 ? true : false;
                            int ax = isNeg ? (((data[i + 10] & 0x7F) << 8) + data[i + 11]) - 32768 : (((data[i + 10] & 0x7F) << 8) + data[i + 11]);
                            isNeg = (data[i + 12] & 0x80) == 0x80 ? true : false;
                            int ay = isNeg ? (((data[i + 12] & 0x7F) << 8) + data[i + 13]) - 32768 : (((data[i + 12] & 0x7F) << 8) + data[i + 13]);
                            isNeg = (data[i + 14] & 0x80) == 0x80 ? true : false;
                            int az = isNeg ? (((data[i + 14] & 0x7F) << 8) + data[i + 15]) - 32768 : (((data[i + 14] & 0x7F) << 8) + data[i + 15]);
                            Vector3 rec_orientation = new Vector3(ox, oy, oz);
                            Vector3 rec_acceleration = new Vector3((float)ax * (9.80665f / 1000f), (float)ay * (9.80665f / 1000f), (float)az * (9.80665f / 1000.0f));
                            i += 16;
                            shouldCheckMac = false;

                            if (!tagFound || !currentTag.trackedEndpoints.ContainsKey(sensorCount))
                            {
                                Debug.LogWarning("Tag does not exist for message. Tag found ? " + tagFound.ToString());
                            }
                            else
                                currentTag.trackedEndpoints[sensorCount].UpdateOrientationAndAcceleration(rec_orientation, rec_acceleration);
                            break;
                        }
                    // IMU Quaternion
                    case 2:
                        {
                            int length = data[i + 1];
                            int sensorCount = data[i] & 0x0F;
                            //  float accuracy = (data[i + 1] << 8) / 10;
                            float ow = ((float)((data[i + 2] << 8) + data[i + 3]) / 10000) - 1;
                            float ox = -(((float)((data[i + 4] << 8) + data[i + 5]) / 10000) - 1);
                            float oz = -(((float)((data[i + 6] << 8) + data[i + 7]) / 10000) - 1);
                            float oy = -(((float)((data[i + 8] << 8) + data[i + 9]) / 10000) - 1);

                            bool isNeg = (data[i + 10] & 0x80) == 0x80 ? true : false;
                            int ax = isNeg ? (((data[i + 10] & 0x7F) << 8) + data[i + 11]) - 32768 : (((data[i + 10] & 0x7F) << 8) + data[i + 11]);
                            isNeg = (data[i + 12] & 0x80) == 0x80 ? true : false;
                            int ay = isNeg ? (((data[i + 12] & 0x7F) << 8) + data[i + 13]) - 32768 : (((data[i + 12] & 0x7F) << 8) + data[i + 13]);
                            isNeg = (data[i + 14] & 0x80) == 0x80 ? true : false;
                            int az = isNeg ? (((data[i + 14] & 0x7F) << 8) + data[i + 15]) - 32768 : (((data[i + 14] & 0x7F) << 8) + data[i + 15]);
                            Quaternion rec_orientation = new Quaternion(ox, oy, oz, ow);
                            Vector3 rec_acceleration = new Vector3((float)ax * (9.80665f / 1000f), (float)ay * (9.80665f / 1000f), (float)az * (9.80665f / 1000.0f));

                            // If Timestamped
                            bool timestamped = length == 18 ? true : false;
                            double timestamp = 0;
                            if (timestamped)
                            {
                                timestamp = ((double)((data[i + 16] << 8) + data[i + 17]) / 10000.0d);
                            }

                            i += length;
                            shouldCheckMac = false;

                            if (!tagFound || !currentTag.trackedEndpoints.ContainsKey(sensorCount))
                            {
                                Debug.LogWarning("Tag does not exist for message. Tag found ? " + tagFound.ToString());
                            }
                            else {
                                if(timestamped)
                                    currentTag.trackedEndpoints[sensorCount].UpdateOrientationAndAcceleration(timestamp, rec_orientation, rec_acceleration);
                                else
                                    currentTag.trackedEndpoints[sensorCount].UpdateOrientationAndAcceleration(rec_orientation, rec_acceleration);
                            
                            }
                            break;
                        }

                    // Trackpad
                    case 3:
                        {
                            int sensorCount = data[i] & 0x0F;
                            byte x = data[i + 1];
                            byte y = data[i + 2];
                            //byte pressure = (byte)(data[i + 3] >> 4);
                            byte btn = (byte)(data[i + 3] & 0x0F);


                            float yFloat = (float)-(x - 127.5) / 255;
                            float xFloat = (float)-(y - 127.5) / 255;
                            i += 4;
                            shouldCheckMac = false;

                            if (!tagFound || !currentTag.trackedEndpoints.ContainsKey(sensorCount))
                            {
                                Debug.LogWarning("Tag does not exist for message. Tag found ? " + tagFound.ToString());
                            }
                            else
                                currentTag.UpdateTrackpadData(xFloat, yFloat, btn);
                            
                            break;
                        }

                    // Gun
                    case 6:
                        {
                            int sensorCount = data[i] & 0x0F;
                            // byte buttons : 0 [trigger, grab, joy_d, a, b, x, y] 7
                            byte buttons = data[i + 1];
                            bool new_trigger = (buttons & (1 << 0)) != 0;
                            bool new_grab = (buttons & (1 << 1)) != 0;
                            bool new_joystick = (buttons & (1 << 2)) != 0;
                            bool new_a = (buttons & (1 << 3)) != 0;
                            bool new_b = (buttons & (1 << 4)) != 0;
                            bool new_x = (buttons & (1 << 5)) != 0;
                            bool new_y = (buttons & (1 << 6)) != 0;

                            i += 4;
                            shouldCheckMac = false;

                            if (!tagFound || !currentTag.trackedEndpoints.ContainsKey(sensorCount))
                            {
                                Debug.LogWarning("Tag does not exist for message. Tag found ? " + tagFound.ToString());
                            }
                            else
                                currentTag.UpdateGunData(new_trigger, new_grab, new_joystick, new_a, new_b, new_x, new_y);

                            break;
                        }

                    default:
                        {
                            Debug.Log("Data could not be parsed : " + data[i].ToString() + "  index: " + i);
                            i = data.Length;
                            break;
                        }
                }
            }
        }


		private VRTracker.Manager.VRT_Tag FindTag(byte[] tagID){
			if (tagID.Length != 2) {
				Debug.LogWarning ("Tag ID != 2 bytes");
				return null;
			}

            // Extract MAC address
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
                //Debug.Log("Close UDP");
                client.Close();
                receiveThread.Abort();
            }
            if(client != null)
            {
                //Debug.Log("Close UDP");
                client.Close();
            }

        }

        private void OnApplicationPause(bool pause)
        {
            if(pause)
            {
                Debug.Log("Pausing app, udp close");
                askedToClose = true;
                if (receiveThread != null)
                {
                    Debug.Log("receive thread not null");
                    connected = false;
                    client.Close();
                    receiveThread.Abort();
                }
                if (client != null)
                {
                    Debug.Log("client not null");
                    client.Close();
                }
            }
            else
            {
                Debug.Log("UnPause UDP Init");
                Init();
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
