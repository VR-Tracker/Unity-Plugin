using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using VRTracker.Manager;
using VRTracker.Player;

namespace VRTracker.Network
{
	/// <summary>
	/// VRT Network Manager overrides the UNET Network Manager
	/// Handle the network component in the game
	/// Store the list of players
	/// </summary>
    public class VRT_NetworkManager : NetworkManager
    {
        [Tooltip("List of all the paleyr in the Game")]
        public List<VRT_PlayerInstance> players;
        public event Action<VRT_PlayerInstance> OnPlayerJoin;
        public event Action<VRT_PlayerInstance> OnPlayerLeave;
        public event Action<VRT_PlayerInstance> OnLocalPlayerJoin;  // Called the local player is set

        public static VRT_NetworkManager Instance;

        [Tooltip("Local player in the Game")]
        public VRT_PlayerInstance localPlayer;

		private bool isServer = false;
		private bool isClient = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        public void Start()
        {
            DontDestroyOnLoad(this.gameObject);

            if (VRT_Manager.Instance.spectator)
            {
                StartServer();
                isServer = true;
				VRT_Manager.Instance.vrtrackerWebsocket.SetServerIp();
                serverBindAddress = VRT_Manager.Instance.vrtrackerWebsocket.serverIp;
                serverBindToIP = true;
                networkAddress = serverBindAddress;
            }
            else
            {
                StartCoroutine(WaitForServerIP());
            }
        }


        /// <summary>
        /// Waits in loop to receive server IP from the Gateway before starting
        /// as a Client
        /// </summary>
        /// <returns></returns>
        IEnumerator WaitForServerIP() {
            //while testing
            while (!VRT_Manager.Instance.vrtrackerWebsocket.serverIp.StartsWith("192.168."))
            {
                yield return new WaitForSeconds(1);
            }

            //Joining the server
            serverBindAddress = VRT_Manager.Instance.vrtrackerWebsocket.serverIp;
            serverBindToIP = true;
            networkAddress = serverBindAddress;
            StartClient();
            isClient = true;
            yield return null;
        }

        internal void JoinGame(string ipAddress)
        {
            networkAddress = ipAddress;
            StartClient();
        }

        internal void StartLanHost()
        {
            //Debug.Log("NETWORK: Starting as Host");
            StartHost();
            //FindObjectOfType<VRTracker.Network.VRT_NetworkDiscovery>().StartBroadcast();
        }

        internal void StartLanServer()
        {
            //Debug.Log("NETWORK: Starting as Server");
            StartServer();
            //FindObjectOfType<VRTracker.Network.VRT_NetworkDiscovery>().StartBroadcast();
        }

        public override void OnServerDisconnect(NetworkConnection conn)
        {
            base.OnServerDisconnect(conn);

            foreach (var p in conn.playerControllers)
            {
                if (p != null && p.gameObject != null)
                {
                    RemovePlayer(p.gameObject.GetComponent<VRT_PlayerInstance>());
                }
            }
        }


		/// <summary>
		/// Adds the player to the list, and update its id
		/// </summary>
		/// <param name="player">Player.</param>
        public void AddPlayer(VRT_PlayerInstance player)
        {
            players.Add(player);

            foreach (VRT_PlayerInstance playerInstance in players)
            {
                player.playerId = (int)player.GetComponent<NetworkIdentity>().netId.Value;
                //player.playerName = "Player " + (player.playerId);
                player.playerTeamId = 0;
            }

            if (OnPlayerJoin != null)
                OnPlayerJoin(player);
        }

		/// <summary>
		/// Removes the player on deconnection
		/// </summary>
		/// <param name="player">Player.</param>
        public void RemovePlayer(VRT_PlayerInstance player)
        {
            players.Remove(player);
            if (OnPlayerLeave != null)
                OnPlayerLeave(player);

        }

		/// <summary>
		/// Sets the local player in the game
		/// </summary>
		/// <param name="player">Player.</param>
        public void SetLocalPlayer(VRT_PlayerInstance player)
        {
            localPlayer = player;
            if (OnLocalPlayerJoin != null)
            {
                OnLocalPlayerJoin(player);
            }
        }

        public VRT_PlayerInstance GetLocalPlayer()
        {
            return localPlayer;
        }
			
		public void OnDestroy()
		{
			if (isServer) {
				if (isClient)
					StopHost ();
				else
					StopServer ();
            }
            else {
				if (isClient)
					StopClient ();
                Debug.Log("NETWORK: Stopping Client ");
            }

        }

    }
}
