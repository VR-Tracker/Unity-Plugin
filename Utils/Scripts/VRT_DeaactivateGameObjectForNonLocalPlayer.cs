using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace VRTracker.Utils
{
    /// <summary>
    /// Disactivate the linked gameobject (or this GameObject) if this script is on a Non Local Player
    /// Note: this does nothing is there is no NetworkIdentity in the parent
    /// </summary>
    public class VRT_DeactivateGameObjectForNonLocalPlayer : MonoBehaviour
    {
        public GameObject objectToDisable;

        // Use this for initialization
        void Start()
        {
            if (GetComponentInParent<NetworkIdentity>() && !GetComponentInParent<NetworkIdentity>().isLocalPlayer)
            {
                if (objectToDisable != null)
                    objectToDisable.SetActive(false);
                else
                    this.gameObject.SetActive(false);
            }
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}