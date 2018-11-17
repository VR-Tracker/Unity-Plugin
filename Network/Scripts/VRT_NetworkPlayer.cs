using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using VRTracker.Player;

namespace VRTracker.Network {
	/// <summary>
	/// VRT network player 
	/// Handles the list of players on the network
	/// And Set the local player
	/// </summary>
    public class VRT_NetworkPlayer : NetworkBehaviour
    {
        [SerializeField]
        private VRTracker.Network.VRT_NetworkManager networkManager;

        // Use this for initialization
        void Start()
        {
            if (networkManager == null)
                networkManager = FindObjectOfType<VRTracker.Network.VRT_NetworkManager>();
            if (networkManager == null)
                Debug.LogError("Network Manager not found");


#if (VRTACKER_INTERNAL)
            VRT_PlayerInstance localPlayer = this.gameObject.GetComponent<VRT_PlayerInstanceExtended> ();
#else
            VRT_PlayerInstance localPlayer = this.gameObject.GetComponent<VRT_PlayerInstance>();
#endif
            networkManager.AddPlayer(localPlayer);

            if (this.gameObject.GetComponent<NetworkIdentity> ().isLocalPlayer) {
                networkManager.SetLocalPlayer(localPlayer);
			}
				
        }

        void OnDestroy()
        {
            if (networkManager)
            {
#if (VRTACKER_INTERNAL)
                networkManager.RemovePlayer(this.gameObject.GetComponent<VRT_PlayerInstanceExtended>());
#else
                networkManager.RemovePlayer(this.gameObject.GetComponent<VRT_PlayerInstance>());
#endif
            }
        }
    }
}
