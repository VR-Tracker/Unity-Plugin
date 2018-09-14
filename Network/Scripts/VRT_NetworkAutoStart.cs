using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using UnityEngine.Networking.Types;

/**
 * VR Tracker - Network
 **/

namespace VRTracker.Network {
	/// <summary>
	/// VR Tracker network auto start
	/// Tries to find a Server for 2 seconds using NetworkDiscovery
	/// and starts a Host or Server if the server wasn't found 
	/// </summary>
	public class VRT_NetworkAutoStart : MonoBehaviour {

		public bool ServerOnly = false;

		public VRTracker.Network.VRT_NetworkDiscovery networkDiscovery;
		public VRTracker.Network.VRT_NetworkManager networkManager;
		private bool hostFound = false;

		// Use this for initialization
		void Start () {
			if(networkDiscovery == null)
				networkDiscovery = FindObjectOfType<VRTracker.Network.VRT_NetworkDiscovery>();
			if (networkManager == null)
				networkManager = FindObjectOfType<VRTracker.Network.VRT_NetworkManager>();
			if (networkDiscovery) {
				StartCoroutine (WaitForLanBoradcast ());
			}
		}

		IEnumerator WaitForLanBoradcast()
		{
			yield return new WaitForSeconds(2);
			if (!hostFound) {
				if (ServerOnly)
					networkManager.StartLanServer ();
				else
					networkManager.StartLanHost ();
				yield break;
			}
		}

		public void BrodcastReception(string ipAddress){
			if (hostFound || ipAddress == "localhost")
				return;
			hostFound = true;
			networkManager.JoinGame (ipAddress);
		}
	}
}