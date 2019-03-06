using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTracker.Network;
using VRTracker.Player;

public class VRT_DisableObjectNetwork : MonoBehaviour
{
    /// <summary>
    /// Disables the GameObject linked (or the gameboject where the script is), if the condition is met
    /// </summary>

    [Tooltip("Object to disable if this PC is the spectator. If empty it uses the GameObject where this script is")]
    [SerializeField] private GameObject objectToDisable;
    //set only one of the options below to true;
    [SerializeField] bool disableIfNotLocalClient;
    [SerializeField] VRT_PlayerInstanceExtended pInstance; //this game object player instance


    [SerializeField] bool disableOnSpectator;

    public void Init() // called by player network when not owner of this object
    {
        if (disableIfNotLocalClient)
        {
            if (objectToDisable == null)
                gameObject.SetActive(false); //if nothing is assigned in inspector, disable this object
            else
                objectToDisable.SetActive(false);
        }
    }

    void Start()
    {

        // NO LONGER ACCESSIBLE WITH FORGE NETWORKMANAGER

        //if (disableOnSpectator)
        //{
        //    if (VRTracker.Manager.VRT_Manager.Instance.spectator)
        //    {
        //        if (objectToDisable == null)
        //            gameObject.SetActive(false);
        //        else
        //            objectToDisable.SetActive(false);
        //    }
        //}
    }
}
