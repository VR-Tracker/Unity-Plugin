using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRTracker.Player
{

	/// <summary>
	/// VR Tracker : This scripts corrects the offset between the Tag position and the users eye
	/// This position offset depends on the users head rotation
	/// The offset value is the transform local position at start.
	/// </summary>
    public class VRT_EyeTagOffset : MonoBehaviour
    {
        [SerializeField] Camera userEyesCamera;	//User camera
        [SerializeField] Vector3 eyeTagOffset;	//Offset from the tag position to the user eye

        // Use this for initialization
        void Start()
        {
            if (userEyesCamera == null)
                userEyesCamera = GetComponentInChildren<Camera>();
        }

        // Update is called once per frame
        void LateUpdate()
        {
            transform.localPosition = userEyesCamera.transform.rotation * eyeTagOffset;

        }
    }
}