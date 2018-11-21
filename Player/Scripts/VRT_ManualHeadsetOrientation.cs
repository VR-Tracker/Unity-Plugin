using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTracker.Player;
using UnityEngine.Networking;

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
    }

    private void FixedUpdate()
    {
        //Debug.Log("child script fixed update");
    }

    public override void ResetOrientation()
    {
        Vector3 camRot = camera.transform.eulerAngles;
        Vector3 offsetY = new Vector3(0, camRot.y, 0);
        transform.rotation = Quaternion.Euler(transform.eulerAngles - offsetY);
        StartCoroutine(Blink());
    }
}
