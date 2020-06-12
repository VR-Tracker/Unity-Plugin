using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using VRTracker.Manager;

namespace VRTracker.Simulator {

	/// <summary>
	/// VRT Simulator movement control class enables you to have all the functionalities of VR Tracker System without it
	/// It can simulate the game when you need a quick debug or in the development phase when you do not have the system nearby
	/// </summary>
	public class VRT_SimulatorMovementControl : MonoBehaviour {

		public bool enable = false;

		public float speed = 2f;
		public float sensitivityXY = 15F;
		private float minimumY = -85F;
		private float maximumY = 85F;

		private List<GameObject> trackers;
		private int index = 0;
		private bool toggleReleased = true;
		private bool triggerPressed = false;
		private string selectedObject = "Main";

		public GameObject vrtrackerManager;

        // Use this for initialization
        void Start () {
			trackers = new List<GameObject> ();
            if (SpectatorManager.Instance.spectator)
            {
                gameObject.SetActive(false);
            }
			if (enable) {
                
				if (vrtrackerManager == null) {
					VRTracker.Manager.VRT_Tag[] tags = FindObjectsOfType (typeof(VRTracker.Manager.VRT_Tag)) as VRTracker.Manager.VRT_Tag[];
					if (tags.Length > 0) {
						vrtrackerManager = tags [0].transform.parent.gameObject;
						vrtrackerManager.SetActive (true);
						trackers.Add (vrtrackerManager);   
					}
					VRTracker.Manager.VRT_Manager.Instance.tags.Clear ();
					foreach (VRTracker.Manager.VRT_Tag tag in tags) {
						tag.gameObject.AddComponent<VRT_TagSimulator> ();
						VRT_TagSimulator sim = tag.gameObject.GetComponent<VRT_TagSimulator> ();
						sim.tagType = tag.tagType;
						tag.enabled = false;
						trackers.Add (tag.gameObject);
					}
				} else {
					vrtrackerManager.SetActive (true);
					trackers.Add (vrtrackerManager);
					for (int i = 0; i < vrtrackerManager.transform.childCount; i++) {

						if (vrtrackerManager.transform.GetChild (i).gameObject.GetComponent<VRTracker.Manager.VRT_Tag> ()) {
							VRTracker.Manager.VRT_Tag tag = vrtrackerManager.transform.GetChild (i).gameObject.GetComponent<VRTracker.Manager.VRT_Tag> ();
							vrtrackerManager.transform.GetChild (i).gameObject.AddComponent<VRT_TagSimulator> ();
							VRT_TagSimulator sim = vrtrackerManager.transform.GetChild (i).gameObject.GetComponent<VRT_TagSimulator> ();
							sim.tagType = tag.tagType;
							tag.enabled = false;
							trackers.Add (vrtrackerManager.transform.GetChild (i).gameObject);
						}
					}
				}
			} else {
				gameObject.SetActive(false);
			}
		}
		
		// Update is called once per frame
		void Update () {

			// Change object to move
			if (Input.GetAxis ("Toggle") == 1.0f && toggleReleased == true) {
				toggleReleased = false;
				index++;
			} else if (Input.GetAxis ("Toggle") == -1.0f && toggleReleased == true) {
				toggleReleased = false;
				if(index>0)
					index--;
			} else if (Input.GetAxis ("Toggle") == 0.0f  && toggleReleased == false) {
				toggleReleased = true;
			}

			// Get object transform
            Transform objectToMove = null;
            if(trackers.Count > 0){
                objectToMove = trackers[index % trackers.Count].transform;
                selectedObject = trackers [index % trackers.Count].name;   
            }

			if (objectToMove == null)
				return;


			// Update position and rotation
			Vector3 newRotation = Vector3.zero;
			Vector3 newPosition = Vector3.zero;

			float rotationX = objectToMove.localEulerAngles.y + Input.GetAxis("Mouse X") * sensitivityXY;
			float rotationY = objectToMove.localEulerAngles.x - Input.GetAxis("Mouse Y") * sensitivityXY;
			rotationY = rotationY > 180.0f ? rotationY - 360F : rotationY;
			rotationY = Mathf.Clamp (rotationY, minimumY, maximumY);
			newRotation = new Vector3 (rotationY, rotationX, 0);

            newPosition += Input.GetAxis ("Horizontal")*vrtrackerManager.transform.right * speed * Time.deltaTime;
            newPosition += Input.GetAxis ("Vertical")*vrtrackerManager.transform.forward * speed * Time.deltaTime;

			objectToMove.position += newPosition;
			objectToMove.localEulerAngles = newRotation;

			// Check for trigger
			if (Input.GetAxis ("Fire1") == 1.0f && triggerPressed != true) {
				triggerPressed = true;
				OnTriggerDown();
			} else if (Input.GetAxis ("Fire1") == 0.0f && triggerPressed == true) {
				triggerPressed = false;
				OnTriggerUp();
			}
		}

		void OnGUI()
		{
			GUI.Label(new Rect(30, 10, 100, 20), selectedObject);
		}

		/// <summary>
		/// Raises the trigger down event and notify the tag
		/// </summary>
		public void OnTriggerDown()
		{
			VRTracker.Manager.VRT_Tag tag = trackers [index % trackers.Count].GetComponent<VRTracker.Manager.VRT_Tag> ();
            if (tag != null)
                tag.OnTriggerDown();
		}

		/// <summary>
		/// Raises the trigger up event and notify the tag
		/// </summary>
		public void OnTriggerUp()
		{
			VRTracker.Manager.VRT_Tag tag = trackers [index % trackers.Count].GetComponent<VRTracker.Manager.VRT_Tag> ();
			if (tag != null)
                tag.OnTriggerUp();
		}
	}


}
