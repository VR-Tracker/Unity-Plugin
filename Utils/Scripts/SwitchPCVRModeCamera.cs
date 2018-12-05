using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// This script is designed only for the player camera holder
/// If used without the VR headset it will enable the tracker orientation for the Camera
/// If in VR it will use the VR headset orientation and correct it with the Tracker orientation
/// </summary>
public class SwitchPCVRModeCamera : MonoBehaviour {

    private VRTracker.Player.VRT_FollowTag followTag;

	void Start () {
        if(followTag == null){
            followTag = gameObject.GetComponent<VRTracker.Player.VRT_FollowTag>();
            if (followTag == null)
            {
                Debug.LogWarning("VRT_FollowTag not linked in SwitchPCVRModeCamera");
                return;
            } 
        }

        if (XRDevice.isPresent)
        {
            followTag.followOrientationX = false;
            followTag.followOrientationY = false;
            followTag.followOrientationZ = false;
        }
        else {
            followTag.followOrientationX = true;
            followTag.followOrientationY = true;
            followTag.followOrientationZ = true;
        }
	}
}
