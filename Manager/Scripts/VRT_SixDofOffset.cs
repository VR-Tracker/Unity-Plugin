using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRTracker
{

    /// <summary>
    /// This script is used to capture or set a Yaw offset to a Tag Endpoint
    /// It is used in cased of 6DOF IMU without 2nd LED for correction (body tracking)
    /// 
    /// </summary>
    public class VRT_SixDofOffset : MonoBehaviour
    {

        public VRTracker.Manager.VRT_TagEndpoint tagEndpoint;
        private float offset = 0;
                
        /// <summary>
        /// Get the current endpoint Yaw and 
        /// apply it to endpoint
        /// </summary>
        public void SetToZero(){
            //Debug.Log("Set To Zero");
            tagEndpoint.useCustomOrientation = true;
            Vector3 eulerOrientation = tagEndpoint.getOrientation().eulerAngles;
            eulerOrientation.y += tagEndpoint.customOrientationOffset;
            tagEndpoint.customOrientationOffset = eulerOrientation.y;
            offset = eulerOrientation.y;
        }
    }
}