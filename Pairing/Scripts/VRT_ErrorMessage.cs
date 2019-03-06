using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This script is designed to toogle an error message from events.
/// It has to register to event in the Start method
/// The script is to be linked to a text object that will receive and display the informations
/// </summary>
public class VRT_ErrorMessage : MonoBehaviour {
    
    public Text text;

	// Use this for initialization
	void Start () {
       // VRTracker.Manager.VRT_Manager.Instance.vrtrackerWebsocket.OnNoGateway += DisplayError;
	}

    void DisplayError(string message){
        text.enabled = true;
        text.text = message;
        Invoke("Hide", 5);
    }

    void Hide(){
        text.enabled = false;
    }
}
