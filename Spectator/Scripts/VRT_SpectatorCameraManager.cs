using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTracker.Manager;

namespace VRTracker.Spectator
{
    /// <summary>
    /// VR Tracker
    /// The Camera Manager is used for the spectator mode, and handle all the different cameras in its child component
    /// You need to add a camera in the camera prefab to be able to use it in the spectator mode
    /// </summary>

    public class VRT_SpectatorCameraManager : MonoBehaviour
    {
        public List<Camera> cameras;
        private int index = 0;


        [Tooltip("Check to automatically transition from a camera to another every switchDelay")]
        [SerializeField]
        private bool autoSwitch = false;

        [Tooltip("Duration before switching from a camera to another")]
        [SerializeField] private float switchDelay = 6f;


        // Use this for initialization
        void Start()
        {
            //Retrieve all cameras on the gameobject
            Camera[] cams = GetComponentsInChildren<Camera>();
            foreach (Camera cam in cams)
            {
                cameras.Add(cam);
            }

            foreach (Camera cam in cameras)
            {
                DisableCam(cam);
            }
            if (VRT_Manager.Instance != null && VRT_Manager.Instance.spectator)
            {
                EnableCam(cameras[index]);
            }

            if (autoSwitch)
                StartCoroutine(AutoSwitchCameras());
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetButtonDown("Jump"))
            {
                autoSwitch = !autoSwitch;
                if (autoSwitch)
                {
                    StartCoroutine(AutoSwitchCameras());
                }

            }
            if (Input.GetKeyDown(KeyCode.N))
            {
                DisableCam(cameras[index]);
                index = (index + 1) % cameras.Count;
                EnableCam(cameras[index]);
            }
        }

		/// <summary>
		/// Removes the camera from the camera list
		/// </summary>
        public void RemoveCamera()
        {
            if (autoSwitch)
            {
                StopCoroutine(AutoSwitchCameras());
            }
            cameras.Clear();
            Camera[] cams = GetComponentsInChildren<Camera>();
            foreach (Camera cam in cams)
            {
                cameras.Add(cam);
            }
            if (autoSwitch)
            {
                StartCoroutine(AutoSwitchCameras());
            }
        }

		/// <summary>
		/// Launch the automatic switch camera mode
		/// </summary>
		/// <returns>The switch cameras.</returns>
        IEnumerator AutoSwitchCameras()
        {
            while (autoSwitch)
            {
                DisableCam(cameras[index]);
                index = (index + 1) % cameras.Count;
                EnableCam(cameras[index]);
                yield return new WaitForSeconds(switchDelay);
            }
        }

		/// <summary>
		/// Adds the player cam to the spectator camera list
		/// </summary>
		/// <param name="newCam">New cam.</param>
        public void AddPlayerCam(Camera newCam)
        {
            DisableCam(newCam);
            cameras.Add(newCam);
        }

		/// <summary>
		/// Enables the camera to display in the spectator view
		/// </summary>
		/// <param name="cam">Cam.</param>
        private void EnableCam(Camera cam)
        {
            cam.enabled = true;
            cam.GetComponent<AudioListener>().enabled = true;
        }

		/// <summary>
		/// Disables the camera view
		/// </summary>
		/// <param name="cam">Cam.</param>
        private void DisableCam(Camera cam)
        {
            cam.enabled = false;
            cam.GetComponent<AudioListener>().enabled = false;
        }
    }
}