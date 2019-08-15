using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTracker.Player;
using UnityEngine.Networking;
using UnityEngine.XR;
using System;

public class VRT_ManualHeadsetOrientation : VRT_HeadsetRotation
{
    private void Start()
    {
        if (networkIdentity == null)
            networkIdentity = GetComponentInParent<NetworkIdentity>();
        newRotation = Vector3.zero;
        if (tag == null)
            tag = VRTracker.Manager.VRT_Manager.Instance.GetHeadsetTag();
        if (networkIdentity != null && !networkIdentity.isLocalPlayer)
        {
            gameObject.SetActive(false);
            this.enabled = false;
            return;
        }

        VRStandardAssets.Utils.VRCameraFade fader = gameObject.GetComponentInChildren<VRStandardAssets.Utils.VRCameraFade>();
        if (fader != null)
        {
            Blink += fader.FadeBlink;

            if (tag.trackedEndpoints[0].filter != null && tag.trackedEndpoints[0].blinkOnJump)
            {
                tag.trackedEndpoints[0].filter.Blink += fader.FadeBlink;
            }
        }
    }

    private void FixedUpdate()
    {
        //Debug.Log("child script fixed update");
    }

    //public override void ResetOrientation()
    //{
    //    InputTracking.Recenter();

    //   if (Blink != null)
    //        Blink();
    //}
}
