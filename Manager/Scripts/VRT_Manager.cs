using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRTracker.Manager
{
	/// <summary>
	/// The VRT_Manager handles the Tags and all the communications. 
	/// It links everything together. This script is a Singleton
	/// </summary>
    public class VRT_Manager : MonoBehaviour
    {
        [Tooltip("Unique User Identifier")]
        public string UserUID = ""; //Each user id is unique

        [Tooltip("The offset between your X axis and the magnetic North (set in the Dashboard)")]
        public float roomNorthOffset; //Value set on the Master Control > Dashboard

        [Tooltip("Check to enable spectator mode")]
        public bool spectator = false; //Disabled by default on android devices

        [Tooltip("If you want to assign a Tag to the spectator mode tracked camera")]
        public bool spectatorTrackedCamera = false; 

        [Tooltip("List of the Tags required for the game")]
        public List<VRTracker.Manager.VRT_Tag> tags;

        public static VRTracker.Manager.VRT_Manager Instance = null;

        public VRTracker.Manager.VRT_WebsocketClient vrtrackerWebsocket; //tcp socket
        public VRTracker.Manager.VRT_UDPClient vrtrackerUDP; //udp socket

        public event Action OnAddTag;  // Called when a player is added with a tagtype
        public event Action<string> OnAvailableTag; // Called when a Tag is available on the Gateway

        private void Awake()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
				//By default deactivate the spectator mode in android devices when built
                spectator = false;
#endif

            //Check if instance already exists
            if (Instance == null)
            {
                Instance = this;
                tags = new List<VRTracker.Manager.VRT_Tag>();
            }

            //If instance already exists and it's not this:
            else if (Instance != this)
                //Then destroy this. This enforces our singleton pattern, meaning there can only ever be one instance of a GameManager.
                Destroy(gameObject);
        }

        // Use this for initialization
        void Start()
        {
			
            DontDestroyOnLoad(this.gameObject);

            if (vrtrackerWebsocket == null)
                vrtrackerWebsocket = GetComponent<VRTracker.Manager.VRT_WebsocketClient>();
			
            if (vrtrackerWebsocket == null)
                Debug.LogError("Linkage error : VRTracker.Manager.VRT_VRT_WebsocketClient is not linked in VRTracker.Manager.VRT_Manager");
            
            if (vrtrackerUDP == null)
                vrtrackerUDP = GetComponent<VRTracker.Manager.VRT_UDPClient>();
			
            if (vrtrackerUDP == null)
                Debug.LogError("Linkage error : VRTracker.Manager.VRT_VRT_UDPClient is not linked in VRTracker.Manager.VRT_Manager");
            
            // Initialize Unique User ID
            UserUID = SystemInfo.deviceUniqueIdentifier.ToLower();
        }

		/// <summary>
		/// Adds the tag to the tags list needed for a player
		/// </summary>
		/// <param name="tag">Tag.</param>
        public void AddTag(VRTracker.Manager.VRT_Tag tag)
        {
            tags.Add(tag);
			if (OnAddTag != null) {
				OnAddTag();
			}
        }

		/// <summary>
		/// Notifies the availability of tag with id "id"
		/// </summary>
		/// <param name="id">Identifier.</param>
        public void AddAvailableTag(String id){
            if (OnAvailableTag != null)
                OnAvailableTag(id);
        }

        /// <summary>
        /// Sends a command to the Gateway to retreive the list of all available Tags
        /// </summary>
        public void GetAvailableTags()
        {
            vrtrackerWebsocket.GetAvailableTags();
        }

		/// <summary>
		/// Gets the tag with the specific tag type : type
		/// WARNING You should only a tag type for a unique element script, if you want to use multiple one, choose other
		/// </summary>
		/// <returns>The tag.</returns>
		/// <param name="type">Type.</param>
        public VRTracker.Manager.VRT_Tag GetTag(VRTracker.Manager.VRT_Tag.TagType type)
        {
            foreach (VRTracker.Manager.VRT_Tag tag in tags)
                if (tag.tagType == type)
                    return tag;
         //   Debug.LogWarning("Could not find a VR Tracker Tag with type " + type.ToString() + " in current Scene");
            return null;
        }
			
		/// <summary>
		/// Gets the tag associated to the user head
		/// </summary>
		/// <returns>The headset tag</returns>
        public VRTracker.Manager.VRT_Tag GetHeadsetTag()
        {
            foreach (VRTracker.Manager.VRT_Tag tag in tags)
                if (tag.tagType == VRTracker.Manager.VRT_Tag.TagType.Head)
                    return tag;
			Debug.LogWarning("Could not find a VR Tracker Tag with type " + VRTracker.Manager.VRT_Tag.TagType.Head.ToString() + " in current Scene");
            return null;
        }

		/// <summary>
		/// Gets the tag associated to the left controller
		/// </summary>
		/// <returns>The left controller tag.</returns>
        public VRTracker.Manager.VRT_Tag GetLeftControllerTag()
        {
            foreach (VRTracker.Manager.VRT_Tag tag in tags)
                if (tag.tagType == VRTracker.Manager.VRT_Tag.TagType.LeftController)
                    return tag;
            Debug.LogWarning("Could not find a VR Tracker Tag with type " + VRTracker.Manager.VRT_Tag.TagType.LeftController.ToString() + " in current Scene");
            return null;
        }

		/// <summary>
		/// Gets the tag associated to the right controller
		/// </summary>
		/// <returns>The right controller tag.</returns>
        public VRTracker.Manager.VRT_Tag GetRightControllerTag()
        {
            foreach (VRTracker.Manager.VRT_Tag tag in tags)
                if (tag.tagType == VRTracker.Manager.VRT_Tag.TagType.RightController)
                    return tag;
            Debug.LogWarning("Could not find a VR Tracker Tag with type " + VRTracker.Manager.VRT_Tag.TagType.RightController.ToString() + " in current Scene");
            return null;
        }

		/// <summary>
		/// Removes the tag from the tag list
		/// Should occur on a tag deconnection
		/// </summary>
		/// <param name="tag">Tag.</param>
        public void RemoveTag(VRTracker.Manager.VRT_Tag tag)
        {
            tags.Remove(tag);
        }

		/// <summary>
		/// Gets the tag object associated with id "id"
		/// </summary>
		/// <returns>The tag object.</returns>
		/// <param name="id">Identifier.</param>
        public GameObject GetTagObject(string id)
        {
            foreach (VRTracker.Manager.VRT_Tag tag in tags)
            {
                if (tag.UID == id)
                {
                    return tag.gameObject;
                }
            }
            return null;
        }
    }
}
