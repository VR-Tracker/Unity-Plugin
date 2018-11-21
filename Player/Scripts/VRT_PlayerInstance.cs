using UnityEngine;
using UnityEngine.Networking;
using System;
using UnityEngine.UI;
using VRTracker.Network;
using VRTracker.Manager;
using System.Collections.Generic;
#if VRTRACKER_INTERNAL
using VRTracker.Scoreboard;
#endif
namespace VRTracker.Player
{
    /// <summary>
	/// VRT Player Instance
    /// Must be placed on the player prefab
	/// Is one of the central component needed to be placed on a player prefab
	/// Some action, as the ready state, the player id, the player team are already implemented if you want to use them
    /// </summary>
    public class VRT_PlayerInstance : NetworkBehaviour
    {
        [Tooltip("Assign playerPosition in inspector")]
        public Transform playerPosition;
        public Camera mainCam;
        [SyncVar(hook = "OnReadyStateChange")]
        public bool readyState;
        public event EventHandler<EventArgs> readyStateEvent;

        [SyncVar(hook = "OnChangeName")] public string playerName;
        public event EventHandler<StringDataEventArgs> nameChangeEvent;
        public Text playerNameText; //assign text field to display name above player
        Vector3 velocity;
        Vector3 previousVelocity;
        public int playerId;        //Player unique id
        public int playerTeamId;    //Player's team unique id

        #if VRTRACKER_INTERNAL
        [SerializeField] public VRT_PlayerModelSkin playerSkin;
        [SerializeField] public VRT_PlayerStats playerStats;
        #endif
        [SerializeField] public NetworkIdentity networkIdentity;


        public void Start()
        {
            if (!isLocalPlayer)
                playerNameText.text = playerName;
        }

        void OnChangeName(string newName)
        {
            //Debug.Log("change name hook");
            playerName = newName;
            NameChangeEvent(new StringDataEventArgs(newName));
            playerNameText.text = newName;
        }

        /// <summary>
        /// Raises the name change event.
        /// </summary>
        /// <param name="e">E.</param>
        void NameChangeEvent(StringDataEventArgs e)
        {
            EventHandler<StringDataEventArgs> handler = nameChangeEvent;
            if (handler != null)
                handler(this, e);
        }

        public void SetName(string name)
        {
            playerName = name;
            if(isServer)
                NameChangeEvent(new StringDataEventArgs(name));
        }
        /// <summary>
        /// Raises the ready state change event.
        /// </summary>
        /// <param name="ready">If set to <c>true</c> ready.</param>
        void OnReadyStateChange(bool ready)
        {
            if (!isServer) //In case server and client
            {
                readyState = ready;
                ReadyStateChangeEvent(EventArgs.Empty);
            }

        }

        /// <summary>
        /// Raises the ready state change event.
        /// </summary>
        /// <param name="e">E.</param>
        void ReadyStateChangeEvent(EventArgs e)
        {
            EventHandler<EventArgs> handler = readyStateEvent;
            if (handler != null)
                handler(this, e);
        }

        /// <summary>
        /// Sets the player to ready state
        /// And raises the ready state change event
        /// </summary>
        public void SetReady()
        {
            readyState = true;
            ReadyStateChangeEvent(EventArgs.Empty);
        }

        /// <summary>
        /// Sets the player to not ready state
        /// And raises the ready state change event
        /// </summary>
        public void SetNotReady()
        {
            readyState = false;
            ReadyStateChangeEvent(EventArgs.Empty);
        }

        /// <summary>
        /// Inform the user to reorient its view
        /// This function is called from a server command
        /// </summary>
        /// <param name="con">Con.</param>
        [TargetRpc]
        public void TargetReoriente(NetworkConnection con)
        {
            VRT_HeadsetRotation headsetRotation = GetComponentInChildren<VRT_HeadsetRotation>();
            Debug.Log("targetReorient " + (headsetRotation == null));
            if (headsetRotation != null)
            {
                
                headsetRotation.ResetOrientation();
            }
        }
    }
}
