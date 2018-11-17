using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRTracker.Spectator {

    /// <summary>
    /// Disables the GameObject linked (or the gameboject where the script is), if this is 
    /// the spectator instance, set on VRTracker Manager.
    /// </summary>
    public class VRT_DisableObjectForSpectator : MonoBehaviour
    {

        [Tooltip("Object to disable if this PC is the spectator. If empty it uses the GameObject where this script is")]
        [SerializeField]
        private GameObject objectToDisable;

        void Start()
        {
            if (VRTracker.Manager.VRT_Manager.Instance.spectator)
            {
                if (objectToDisable == null)
                    gameObject.SetActive(false);
                else
                    objectToDisable.SetActive(false);
            }
        }
    }
}
