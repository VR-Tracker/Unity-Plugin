using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// VR tracker player camera
/// Attach the script to the player prefab 
/// The camera will follow the player
/// </summary>
public class VRTrackerPlayerCamera : MonoBehaviour {

    [Tooltip("The player object to follow")]
    public GameObject player;

    public Vector3 offsetVector;
    public Vector3 offsetRotation;
    public Vector3 offsetCameraRotation;

    public new Camera camera;

    // Use this for initialization
    void Start () {
		if (VRTracker.Manager.VRT_Manager.Instance != null && !SpectatorManager.Instance.spectator)
		{	
            return;
        }
        transform.rotation = Quaternion.Euler(offsetRotation.x, offsetRotation.y, offsetRotation.z);
    }

    // Update is called once per frame
    void LateUpdate()
    {
        transform.position = new Vector3(player.transform.position.x + offsetVector.x, 2f + offsetVector.y, player.transform.position.z + offsetVector.z);
    }
}
