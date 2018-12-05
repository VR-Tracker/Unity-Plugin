using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.Networking;

/// <summary>
/// VRT headset rotation.
/// This script is to be set on a Gameobject between the Camera and the Object to which the Headset Tag position is applied
/// </summary>
namespace VRTracker.Player
{
    public class VRT_HeadsetRotation : MonoBehaviour
    {

        public new Camera camera;
        public new VRTracker.Manager.VRT_Tag tag;

        protected NetworkIdentity networkIdentity;

        private Quaternion previousOffset;
        private Quaternion destinationOffset;

        protected Vector3 newRotation;

        private float t;
        private float timeToReachTarget = 15.0f;            //Time used to correct the orientation
        private int waitTimeBeforeVerification = 30; 	//Time in second before checking if the orientation need to be corrected
        [Tooltip("The minimum offset in degrees to blink instead of rotating.")]
        public float minOffsetToBlink = 15.0f;			//Minimun difference for the orientation to display a blink and do an hard correction
        public float errorOffset = 30.0f; // Offset to detect error (on start or when headset is put on) 
        private int errorCounter = 0;

        protected Action Blink;

        /*[Tooltip("The VRTK Headset Fade script to use when fading the headset. If this is left blank then the script will need to be applied to the same GameObject.")]
        public VRTK.VRTK_HeadsetFade headsetFade;
        */
        void Start()
        {
            // Don't use when XR device is not connected
            if (!XRDevice.isPresent)
            {
                enabled = false;
                return;
            }

            if (networkIdentity == null)
                networkIdentity = GetComponentInParent<NetworkIdentity>();
            newRotation = Vector3.zero;
            if (tag == null && VRTracker.Manager.VRT_Manager.Instance != null)
                tag = VRTracker.Manager.VRT_Manager.Instance.GetHeadsetTag();
            if (networkIdentity != null && !networkIdentity.isLocalPlayer)
            {
                gameObject.SetActive(false);
                this.enabled = false;
                return;
            }

            VRStandardAssets.Utils.VRCameraFade fader = gameObject.GetComponentInChildren<VRStandardAssets.Utils.VRCameraFade>();
            if (fader != null)
                Blink += fader.FadeBlink;
            else
                Debug.LogWarning("COuld not find Blink panel");

            previousOffset = Quaternion.identity;
            destinationOffset = Quaternion.identity;
           // ResetOrientation();
            StartCoroutine(FixOffset());
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            t += Time.deltaTime / timeToReachTarget;
            transform.localRotation = Quaternion.Lerp(previousOffset, destinationOffset, t);


            if (tag != null && tag.trackedEndpoints.ContainsKey((0)))
            {
                Vector3 tagRotation = UnmultiplyQuaternion(tag.trackedEndpoints[0].getOrientation());
                Vector3 cameraRotation = UnmultiplyQuaternion(camera.transform.localRotation);
                float offsetAngle = Mathf.Abs(GetShortestAngle(newRotation.y, tagRotation.y - cameraRotation.y));
                if (offsetAngle > errorOffset)
                {
                    errorCounter++;
                    if (errorCounter > 8)
                    {
                        if (Blink != null)
                            Blink();
                        errorCounter = 0;
                        t = timeToReachTarget;
                        newRotation.y = tagRotation.y - cameraRotation.y;
                        previousOffset = destinationOffset;
                        destinationOffset = Quaternion.Euler(newRotation);

                    }
                }
                else
                {
                    errorCounter = 0;
                }

            }
        }

        /// <summary>
        /// Fixes the offset and make the correction if necessary
        /// </summary>
        /// <returns>The offset.</returns>
        IEnumerator FixOffset()
        {
            while (true)
            {
                if (VRTracker.Manager.VRT_Manager.Instance != null)
                {
                    if (tag == null)
                        tag = VRTracker.Manager.VRT_Manager.Instance.GetHeadsetTag();
                    if (tag != null)
                    {
                        UpdateOrientationData();
                        if (ShoudlBlink())
                        {
                            t = timeToReachTarget;
                            if (Blink != null)
                                Blink();
                        }
                        else
                            t = 0;
                    }
                    yield return new WaitForSeconds(waitTimeBeforeVerification);
                }
                else
                {
                    yield return new WaitForSeconds(0.5f);
                }
            }
        }

        /// <summary>
        /// Updates the orientation data
        /// </summary>
        private bool UpdateOrientationData()
        {
            if (tag != null && tag.trackedEndpoints.ContainsKey((0)))
            {
                Vector3 tagRotation = UnmultiplyQuaternion(tag.trackedEndpoints[0].getOrientation());
                Vector3 cameraRotation = UnmultiplyQuaternion(camera.transform.localRotation);
                newRotation.y = tagRotation.y - cameraRotation.y;
                previousOffset = destinationOffset;
                destinationOffset = Quaternion.Euler(newRotation);
                //    Debug.Log("Update Data | Tag: " + tagRotation.y.ToString() + " | Cam: " + cameraRotation.y.ToString() + " | New rot: " + newRotation.ToString() + " | Previous offset: " + previousOffset.ToString());
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true if the orientation need to be corrected
        /// </summary>
        /// <returns><c>true</c>, if reorientation was needed, <c>false</c> otherwise.</returns>
        private bool ShoudlBlink()
        {
            float angle = GetShortestAngle(previousOffset.eulerAngles.y, newRotation.y);
            //    Debug.Log("Blink ? " + angle.ToString());
            return Mathf.Abs(angle) > minOffsetToBlink;
        }

        private float GetShortestAngle(float from, float to)
        {
            float angle = (to - from) % 360.0f;
            angle = (2 * angle) % 360.0f - angle;
            return angle;
        }

        /// <summary>
        /// Resets the orientation and fade a blink to the user
        /// </summary>
        public virtual void ResetOrientation()
        {
            VRTracker.Manager.VRT_Tag headTag = VRTracker.Manager.VRT_Manager.Instance.GetTag(VRTracker.Manager.VRT_Tag.TagType.Head);
            VRTracker.Manager.VRT_Tag gunTag = VRTracker.Manager.VRT_Manager.Instance.GetTag(VRTracker.Manager.VRT_Tag.TagType.Gun);

            VRTracker.VRT_SixDofOffset tagOffsetHead = null;
            VRTracker.VRT_SixDofOffset tagOffsetGun = null;

            if (headTag != null)
                tagOffsetHead = headTag.GetComponent<VRTracker.VRT_SixDofOffset>();

            if (gunTag != null)
                tagOffsetGun = gunTag.GetComponent<VRTracker.VRT_SixDofOffset>();

            if(tagOffsetHead!= null)
                tagOffsetHead.SetToZero(); 

            if(tagOffsetGun != null)
                tagOffsetGun.SetToZero();
                      
            if (Blink != null)
                Blink();
        }

        /// <summary>
        /// Unmultiplies the quaternion to get the rotation
        /// </summary>
        /// <returns>The quaternion.</returns>
        /// <param name="quaternion">Quaternion.</param>
        private Vector3 UnmultiplyQuaternion(Quaternion quaternion)
        {
            Vector3 ret;

            var xx = quaternion.x * quaternion.x;
            var xy = quaternion.x * quaternion.y;
            var xz = quaternion.x * quaternion.z;
            var xw = quaternion.x * quaternion.w;

            var yy = quaternion.y * quaternion.y;
            var yz = quaternion.y * quaternion.z;
            var yw = quaternion.y * quaternion.w;

            var zz = quaternion.z * quaternion.z;
            var zw = quaternion.z * quaternion.w;

            var check = zw + xy;
            if (Mathf.Abs(check - 0.5f) <= 0.00001f)
                check = 0.5f;
            else if (Mathf.Abs(check + 0.5f) <= 0.00001f)
                check = -0.5f;

            ret.y = Mathf.Atan2(2 * (yw - xz), 1 - 2 * (yy + zz));
            ret.z = Mathf.Asin(2 * check);
            ret.x = Mathf.Atan2(2 * (xw - yz), 1 - 2 * (zz + xx));

            if (check == 0.5f)
            {
                ret.x = 0;
                ret.y = 2 * Mathf.Atan2(quaternion.y, quaternion.w);
            }
            else if (check == -0.5f)
            {
                ret.x = 0;
                ret.y = -2 * Mathf.Atan2(quaternion.y, quaternion.w);
            }

            ret = ret * 180 / Mathf.PI;
            return ret;
        }
    }
}
