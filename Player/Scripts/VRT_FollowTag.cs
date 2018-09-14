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

        [Tooltip("The type of Tag chosen in VRTracker.Manager.VRT_Tag script to follow")]
        public VRTracker.Manager.VRT_Tag.TagType tagTypeToFollow;
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

                if (tagToFollow != null)
                    OnTagJoinEvent(EventArgs.Empty);
            }
            else
                Debug.LogError("No VR Tracker script found in current Scene. Import VRTracker prefab");
        }

        // Update is called once per frame
        void LateUpdate()
        {
            if (NetIdent != null && !NetIdent.isLocalPlayer)
                return;

            if (tagToFollow == null && VRTracker.Manager.VRT_Manager.Instance != null)
            {
                tagToFollow = VRTracker.Manager.VRT_Manager.Instance.GetTag(tagTypeToFollow);
                if (tagToFollow != null)
                    OnTagJoinEvent(EventArgs.Empty);
            }
            else if (simulatorTag)
            {
                Vector3 newRotation = tagToFollow.transform.rotation.eulerAngles;
                transform.position = tagToFollow.transform.position;
                transform.rotation = Quaternion.Euler(newRotation.x, newRotation.y, newRotation.z);
            }
            else if (tagToFollow != null)
            {
                if (followPositionX || followPositionY || followPositionZ)
                {
                    if(useLocalPosition)
                        transform.localPosition = new Vector3(followPositionX ? tagToFollow.transform.position.x : originalPosition.x, followPositionY ? tagToFollow.transform.position.y : originalPosition.y, followPositionZ ? tagToFollow.transform.position.z : originalPosition.z);
                    else
                        transform.position = new Vector3(followPositionX ? tagToFollow.transform.position.x : originalPosition.x, followPositionY ? tagToFollow.transform.position.y : originalPosition.y, followPositionZ ? tagToFollow.transform.position.z : originalPosition.z);
                }

                if (followOrientationX || followOrientationY || followOrientationZ)
                {
                    Vector3 newRotation = tagToFollow.getOrientation();
                    if (useLocalRotation)
                        transform.localRotation = Quaternion.Euler(followOrientationX ? newRotation.x : originalRotation.x, followOrientationY ? newRotation.y : originalRotation.y, followOrientationZ ? newRotation.z : originalRotation.z);
                    else
                        transform.rotation = Quaternion.Euler(followOrientationX ? newRotation.x : originalRotation.x, followOrientationY ? newRotation.y : originalRotation.y, followOrientationZ ? newRotation.z : originalRotation.z);
                }
            }
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
