using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using VRTracker.Manager;

namespace VRTracker.Manager
{
    public class VRT_UnityXREndpointSimulator : VRT_TagEndpoint
    {
        private XRNode trackPoint;
        private bool buttonState = false;

        public override void Start()
        {
            parentTag = GetComponentInParent<VRT_Tag>();
            parentTag.IDisAssigned = true;

            if (parentTag.tagType == VRT_Tag.TagType.Head)
                trackPoint = XRNode.Head;
            else if (parentTag.tagType == VRT_Tag.TagType.LeftController)
                trackPoint = XRNode.LeftHand;
            else if (parentTag.tagType == VRT_Tag.TagType.RightController || parentTag.tagType == VRT_Tag.TagType.Gun)
                trackPoint = XRNode.RightHand;
            else
                Debug.LogError("No compatible tag type set");
        }

        // Update is called once per frame
        public override void Update()
        {
            transform.position = InputTracking.GetLocalPosition(trackPoint);
            transform.rotation = InputTracking.GetLocalRotation(trackPoint);

            if (positionUpdateHandler != null)
            {
                positionUpdateHandler(transform.position);
            }

            if (orientationUpdateHandler != null)
            {
                orientationUpdateHandler(transform.rotation);
            }

            if (trackPoint == XRNode.LeftHand)
            {
                if (buttonState != Input.GetButton("TriggerLeft"))
                {
                    buttonState = Input.GetButton("TriggerLeft");
                    if (buttonState && parentTag.OnTriggerDown != null)
                    {
                        UnityMainThreadDispatcher.Instance().Enqueue(parentTag.OnTriggerDown);
                    }
                    else if (!buttonState && parentTag.OnTriggerUp != null)
                    {
                        UnityMainThreadDispatcher.Instance().Enqueue(parentTag.OnTriggerUp);
                    }
                }
            }
            else if (trackPoint == XRNode.RightHand)
            {
                if (buttonState != Input.GetButton("TriggerRight"))
                {
                    buttonState = Input.GetButton("TriggerRight");
                    if (buttonState && parentTag.OnTriggerDown != null)
                    {
                        UnityMainThreadDispatcher.Instance().Enqueue(parentTag.OnTriggerDown);
                    }

                    else if (!buttonState && parentTag.OnTriggerUp != null)
                    {
                        UnityMainThreadDispatcher.Instance().Enqueue(parentTag.OnTriggerUp);
                    }
                }
            }
        }

        // Use this for initialization
        public override void SetTag(VRT_Tag tag)
        {
            parentTag = tag;
        }

        /// <summary>
        /// Gets the orientation of the tag
        /// </summary>
        /// <returns>The orientation.</returns>
        public override Quaternion getOrientation()
        {
            return this.transform.rotation;
        }

        /// <summary>
        /// Gets the position received from the system
        /// </summary>
        /// <returns>The position.</returns>
        public override Vector3 GetPosition()
        {
            return this.transform.position;
        }
    }
}