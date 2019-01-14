using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

/**
 * VR Tracker - Network
 **/

namespace VRTracker.Network {
	/// <summary>
	/// VRT Network Discovery.
	/// Handles the server discovery for Lan
	/// </summary>
	[RequireComponent(typeof(VRTracker.Network.VRT_NetworkManager))]
	public class VRT_NetworkDiscovery : NetworkDiscovery
	{
		private float timeout = 5f;
        private bool broadcasting = false;
		private Dictionary<string, float> lanAddresses = new Dictionary<string, float>();

		private void Start()
		{
		}

		public void StartBroadcast()
		{
            if(broadcasting)
    			StopBroadcast();
            
			Initialize();

			string matchName = SceneManager.GetActiveScene().name;
			broadcastData = broadcastData + ":" + matchName + ":" + GetComponent<VRTracker.Network.VRT_NetworkManager>().networkPort.ToString() + ":" + GetComponent<VRTracker.Network.VRT_NetworkManager>().playerPrefab.name;
            broadcasting = true;
            StartAsServer();
            Debug.Log("Broadcasting");
		}

        public void StartClient()
        {
            Initialize();
            StartAsClient();
            StartCoroutine(CleanupExpiredEntries());
        }

        private IEnumerator CleanupExpiredEntries()
		{
			while (true)
			{
				var keys = lanAddresses.Keys.ToList();
				foreach (var key in keys)
				{
					if (lanAddresses[key] <= Time.time)
					{
						lanAddresses.Remove(key);
					}
				}
				yield return new WaitForSeconds(timeout);
			}
		}

		public override void OnReceivedBroadcast(string fromAddress, string data)
		{
			base.OnReceivedBroadcast(fromAddress, data);

			if (FindObjectOfType<VRTracker.Network.VRT_NetworkAutoStart> ())
				FindObjectOfType<VRTracker.Network.VRT_NetworkAutoStart> ().BrodcastReception (fromAddress);

			if (lanAddresses.ContainsKey(fromAddress) == false)
			{
				lanAddresses.Add(fromAddress, Time.time + timeout);
			}
			else
			{
				lanAddresses[fromAddress] = Time.time + timeout;
			}
		}

		private void OnApplicationQuit()
		{
            if(enabled && broadcasting) 
                base.StopBroadcast();
		}
	}
}