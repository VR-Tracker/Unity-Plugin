using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRTracker.Player
{
	/// <summary>
	/// VRT inside out follow tag.
	/// Used with headset with inside out tracking
	/// Fuse the data between both tracking system
	/// </summary>
    public class VRT_InsideOutFollowTag : MonoBehaviour
    {

        [Tooltip("The real player camera to which the inside out tracking is applied")]
        public Camera playerCamera; // The real player camera to which the inside out tracking is applied

        [Tooltip("he offset between the Tag position and the users eyes position")]
        public Vector3 eyeTagOffset; // The offset between the Tag position and the users eyes position

        private Vector3 tagPositionWithEyeOffset; // THe Tag position with the eye tag offset applied

        private Quaternion previousOrientationOffset; 
        private Quaternion destinationOrientationOffset;

        private float tRotation = 0.0f;
        private float tPosition = 0.0f;
        private float timeToReachTargetRotation = 15.0f;
        private float timeToReachTargetPosition = 15.0f;
        [Tooltip("The minimum rotation offset in degrees to blink instead of rotating.")]
        public float minRotationOffsetToBLink = 30.0f;

        [Tooltip("The minimum position offset in meters to blink instead of lerping.")]
        public float minPositionOffsetToBLink = 0.4f;

        private VRTracker.Manager.VRT_Tag tagToFollow;

        [HideInInspector]
        public bool simulatorTag = false;

        [SerializeField] Renderer fader;

        // Use this for initialization
        void Start()
        {
            tagPositionWithEyeOffset = Vector3.zero;

            if (VRTracker.Manager.VRT_Manager.Instance != null)
            {
                tagToFollow = VRTracker.Manager.VRT_Manager.Instance.GetTag(VRTracker.Manager.VRT_Tag.TagType.Head);
                if (tagToFollow && tagToFollow is VRT_TagSimulator)
                    simulatorTag = true;
            }

            if (playerCamera == null)
                playerCamera = GetComponentInChildren<Camera>();
            
            previousOrientationOffset = Quaternion.Euler(Vector3.zero);
            destinationOrientationOffset = Quaternion.Euler(Vector3.zero);

            StartCoroutine(FixOrientationOffset());
        }

        // Update is called once per frame
        void Update()
        {
            tRotation += Time.deltaTime / timeToReachTargetRotation;
            transform.localRotation = Quaternion.Lerp(previousOrientationOffset, destinationOrientationOffset, tRotation);

            tagPositionWithEyeOffset = tagToFollow.transform.position + playerCamera.transform.rotation * eyeTagOffset;

            //TODO: Renable   if (tagToFollow.delaySinceLastUpdateReceived() < 60)
           // {
                // Calculates the difference between the Tag position and Inside Out Position
                Vector3 correctionOffset = tagPositionWithEyeOffset - playerCamera.transform.localPosition;
                Vector3 diffOffset = tagPositionWithEyeOffset - playerCamera.transform.position;
                // TODO: Check is tracking
                // Blink to update the position if offset is too large
                if (diffOffset.magnitude > minPositionOffsetToBLink)
                {
                    transform.position = correctionOffset;
                    StartCoroutine(Blink()); // TODO: change the position at the middle of the blink, not before
                }
                // Lerp the offset position to destination
                else
                {
                    transform.position = Vector3.Lerp(transform.position, correctionOffset, 0.02f);
                }
         //   }
        }



        IEnumerator Blink()
        {
            Color faderColor = fader.material.color;

            faderColor.a = 1;
            fader.material.color = faderColor;

            yield return new WaitForSeconds(0.1f);
            while (faderColor.a > 0)
            {
                faderColor.a -= 0.2f;
                fader.material.color = faderColor;
                yield return new WaitForSeconds(0.05f);
            }
            faderColor.a = 0;
            fader.material.color = faderColor;
        }

        IEnumerator FixOrientationOffset()
        {
            while (true)
            {
                if (VRTracker.Manager.VRT_Manager.Instance != null)
                {
                    if (tagToFollow == null)
                        tagToFollow = VRTracker.Manager.VRT_Manager.Instance.GetTag(VRTracker.Manager.VRT_Tag.TagType.Head);
                    if (tagToFollow != null)
                    {
                        //TODO: Re enable
                        /*
                        Vector3 tagRotation = UnmultiplyQuaternion(Quaternion.Euler(tagToFollow.getOrientation()));
                        Vector3 cameraRotation = UnmultiplyQuaternion(playerCamera.transform.localRotation);
                        float yRotation = tagRotation.y - cameraRotation.y;


                        float offsetY = Mathf.Abs(destinationOrientationOffset.eulerAngles.y - yRotation) % 360;
                        offsetY = offsetY > 180.0f ? offsetY - 360 : offsetY;

                        previousOrientationOffset = destinationOrientationOffset;
                        destinationOrientationOffset = Quaternion.Euler(new Vector3(0, yRotation, 0));
                        if (Mathf.Abs(offsetY) > minRotationOffsetToBLink)
                        {
                            tRotation = timeToReachTargetRotation;
                            StartCoroutine(Blink());
                        }
                        else
                            tRotation = 0;
*/
                    }
                    yield return new WaitForSeconds(20);
                }
                else
                {
                    yield return new WaitForSeconds(0.5f);
                }
            }
        }

		/// <summary>
		/// Fixs the position offset.
		/// </summary>
		/// <returns>The position offset.</returns>
        IEnumerator FixPositionOffset()
        {
            while (true)
            {
                if (VRTracker.Manager.VRT_Manager.Instance != null)
                {
                    if (tagToFollow == null)
                        tagToFollow = VRTracker.Manager.VRT_Manager.Instance.GetTag(VRTracker.Manager.VRT_Tag.TagType.Head);
                    if (tagToFollow != null)
                    {
                        //TODO: Re enable
                        /*Vector3 tagRotation = UnmultiplyQuaternion(Quaternion.Euler(tagToFollow.getOrientation()));
                        Vector3 cameraRotation = UnmultiplyQuaternion(playerCamera.transform.localRotation);
                        float yRotation = tagRotation.y - cameraRotation.y;
           
						float offsetY = Mathf.Abs(destinationOrientationOffset.eulerAngles.y - yRotation) % 360;
                        offsetY = offsetY > 180.0f ? offsetY - 360 : offsetY;

                        previousOrientationOffset = destinationOrientationOffset;
                        destinationOrientationOffset = Quaternion.Euler(new Vector3(0, yRotation, 0));
                        if (Mathf.Abs(offsetY) > minRotationOffsetToBLink)
                        {
                            tPosition = timeToReachTargetPosition;
                            StartCoroutine(Blink());
                        }
                        else
                            tPosition = 0;
*/
                    }
                    yield return new WaitForSeconds(20);
                }
                else
                {
                    yield return new WaitForSeconds(0.5f);
                }
            }
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