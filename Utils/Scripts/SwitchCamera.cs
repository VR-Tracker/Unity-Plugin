using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Disables all camera expect Pairing Camera on start
/// and re enable all camera on Destroy (switch to other scene)
/// </summary>
public class SwitchCamera : MonoBehaviour {
    
    private Camera[] cameras;

	// Use this for initialization
	void Start () {
        cameras = FindObjectsOfType<Camera>();
        Camera pairingCamera = gameObject.GetComponent<Camera>();
        if (cameras.Length > 1){
            foreach(Camera cam in cameras){
                if (cam != pairingCamera)
                    cam.enabled = false; 
            }
        }
	}
	
	// When switching back to Main Scene
	void OnDestroy () {
        foreach (Camera cam in cameras)
        {
            cam.enabled = true;
        }
	}
}
