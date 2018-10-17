using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace VRTracker.Player
{
	/// <summary>
	/// VRT follow tag
	/// Attach the script to any object that you to follow a Tag position and/or orientation
	/// and choose the Tag type to follow to update the transform data with the correct values
	/// </summary>
    public class VRT_FollowTag : MonoBehaviour
    {

        public enum EndpointId
        {
            Main,One,Two,Three,Four,Five,Six,Seven
        }

        [Tooltip("The type of Tag chosen in VRTracker.Manager.VRT_Tag script to follow")]
        public VRTracker.Manager.VRT_Tag.TagType tagTypeToFollow;

        [Tooltip("The tracking endpoint on the Tracker, in case of body tracking where multiple IMU/IR are handled by the same Tracker")]
        public EndpointId endpoint;

        public bool useLocalPosition = false;
        public bool useLocalRotation = false;
        public bool followOrientationX = true;
        public bool followOrientationY = true;
        public bool followOrientationZ = true;
        public bool followPositionX = true;
        public bool followPositionY = true;
        public bool followPositionZ = true;

        private Vector3 originalPosition;
        private Vector3 originalRotation;

        [HideInInspector]
        public VRTracker.Manager.VRT_Tag tagToFollow;
        [HideInInspector]
        public bool tagFound = false;
        [HideInInspector]
        public bool simulatorTag = false;

        public event EventHandler OnTagJoin;

        private NetworkIdentity NetIdent;

        // Use this for initialization
        void Start()
        {
            if (GetComponentsInParent<NetworkIdentity>().Length > 0)
            {
                NetIdent = GetComponentsInParent<NetworkIdentity>()[0];
                if (NetIdent != null && !NetIdent.isLocalPlayer)
                {
                    enabled = false;
                    return;
                }
            }

            originalPosition = transform.position;
            originalRotation = transform.rotation.eulerAngles;

            if (VRTracker.Manager.VRT_Manager.Instance != null)
            {
                tagToFollow = VRTracker.Manager.VRT_Manager.Instance.GetTag(tagTypeToFollow);
                if (tagToFollow && tagToFollow is VRT_TagSimulator)
                    simulatorTag = true;

                if (tagToFollow != null && tagToFollow.trackedEndpoints.ContainsKey((int)endpoint))
                {
                    tagFound = true;
                    tagToFollow.trackedEndpoints[(int)endpoint].positionUpdateHandler += UpdatePosition;
                    tagToFollow.trackedEndpoints[(int)endpoint].orientationUpdateHandler += UpdateOrientation;
                    OnTagJoinEvent(EventArgs.Empty);
                }
            }
            else
                Debug.LogError("No VR Tracker script found in current Scene. Import VRTracker prefab");
        }

        // Update is called once per frame
        void Update()
        {
            if (NetIdent != null && !NetIdent.isLocalPlayer)
                return;

            if (!tagFound && VRTracker.Manager.VRT_Manager.Instance != null)
            {
                tagToFollow = VRTracker.Manager.VRT_Manager.Instance.GetTag(tagTypeToFollow);
                if (tagToFollow != null && tagToFollow.trackedEndpoints.ContainsKey((int)endpoint))
                {
                    tagFound = true;
                    tagToFollow.trackedEndpoints[(int)endpoint].positionUpdateHandler += UpdatePosition;
                    tagToFollow.trackedEndpoints[(int)endpoint].orientationUpdateHandler += UpdateOrientation;
                    OnTagJoinEvent(EventArgs.Empty);
                }
            }
            else if (simulatorTag)
            {
                Debug.LogWarning("Simulator Tag is not implement in this version");
            }

        }

        public void UpdatePosition(Vector3 position)
		{
            if(useLocalPosition)
                transform.localPosition = new Vector3(followPositionX ? position.x : originalPosition.x, followPositionY ? position.y : originalPosition.y, followPositionZ ? position.z : originalPosition.z);
            else
                transform.position = new Vector3(followPositionX ? position.x : originalPosition.x, followPositionY ? position.y : originalPosition.y, followPositionZ ? position.z : originalPosition.z);
       
		}

        public void UpdateOrientation(Quaternion orientation)
        {
            Vector3 eulerRotation = orientation.eulerAngles;
            if (useLocalRotation)
                transform.localRotation = Quaternion.Euler(followOrientationX ? eulerRotation.x : originalRotation.x, followOrientationY ? eulerRotation.y : originalRotation.y, followOrientationZ ? eulerRotation.z : originalRotation.z);
            else
                transform.rotation = orientation;
        }

		/// <summary>
		/// Raises the tag join event event
		/// </summary>
		/// <param name="e">E.</param>
		protected virtual void OnTagJoinEvent(EventArgs e)
        {
            EventHandler handler = OnTagJoin;
            if (handler != null)
                handler(this, e);
        }
    }
}
