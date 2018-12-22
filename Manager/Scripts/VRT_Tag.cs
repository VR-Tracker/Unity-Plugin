using UnityEngine;
using System.Collections;
using System;
using UnityEngine.VR;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Globalization;
using CircularBuffer;

namespace VRTracker.Manager
{
	/// <summary>
	/// VR Tracker
	/// This script handle all the interaction with a tag
	/// You need to add this component to a child object of VR Tracker depending on the number of tracked object you want
	/// </summary>
    public class VRT_Tag : MonoBehaviour
    {
        [Tooltip("Set the type of Tag to access it later. Each type must only be used once.")]
        public TagType tagType; // The tag type is used to get the tag association across multiple scene and player avatar

        public TagVersion tagVersion = TagVersion.V3;

        public enum TagType
        {
            Head, 				// Track the user head
			Gun, 				// Track the user weapon
			LeftController, 	// Track the user left controller
			RightController, 	// Track the user right controller
			LeftFoot, 			// Track the user left foot
			RightFoot, 			// Track the user right controller
			CameraSpectator, 	// Track the spectator camera, when a tag is put on a real camera to follow its position
			Other, 				// Use to track any object with a tag
            Body
        }



        public enum TagVersion {
            V2,
            V3,
            Gun
        }

        public Dictionary<int, VRT_TagEndpoint> trackedEndpoints = new Dictionary<int, VRT_TagEndpoint>();


        // Button value saved here for VRTK
        [System.NonSerialized] public bool triggerPressed = false;
        [System.NonSerialized] public bool triggerUp = false;
        [System.NonSerialized] public bool triggerDown = false;
        [System.NonSerialized] public bool buttonPressed = false;
        [System.NonSerialized] public bool buttonUp = false;
        [System.NonSerialized] public bool buttonDown = false;
        [System.NonSerialized] public bool trackpadTouch = false;
        [System.NonSerialized] public bool trackpadUp = false;
        [System.NonSerialized] public bool trackpadDown = false;
        [System.NonSerialized] public Vector2 trackpadXY = Vector2.zero;


        // Gun
        [System.NonSerialized] public bool trigger = false;
        [System.NonSerialized] public bool grab = false;
        [System.NonSerialized] public bool joystick = false;
        [System.NonSerialized] public bool a = false;
        [System.NonSerialized] public bool b = false;
        [System.NonSerialized] public bool x = false;
        [System.NonSerialized] public bool y = false;

        public Action OnGrab;
        public Action OnRelease;
        public Action OnAPressed;
        public Action OnAReleased;
        public Action OnBPressed;
        public Action OnBReleased;
        public Action OnXPressed;
        public Action OnXReleased;
        public Action OnYPressed;
        public Action OnYReleased;
        public Action OnJoystickPressed;
        public Action OnJoystickReleased;

        // Trackpad
        protected int trackpadMaxLeft = 0; 		// Max left (x) value sent by the trackpad
        protected int trackpadMaxRight = 1000;  // Max right (x) value sent by the trackpad
        protected int trackpadMaxUp = 1000; 	// Max up (x) value sent by the trackpad
        protected int trackpadMaxDown = 0; 		// Max down (x) value sent by the trackpad

        // Tag buttons
        public Action OnTriggerDown;  //Occurs when trigger button is down
        public Action OnTriggerUp;        //Occurs when trigger button is up
        public Action OnRedButtonDown;    //Occurs when red button is down
        public Action OnRedButtonUp;  //Occurs when red button is up

        // Actions for tracking lost or found
        public Action OnTrackingLost;
        public Action OnTrackingFound;

        protected float currentTime; //Timestamp use for assignation

		public string status; //Tag status (unassigned, tracked, lost)
        public int battery = 0; //Battery remaining for the tag, in percentage (0-100)
        private string version; // Tag version (exple 203 304 602...)

        [System.NonSerialized] public bool waitingForID = false; // if the tag is Waiting for its ID
        [System.NonSerialized] public bool IDisAssigned = false; // if the script is assigned to a tag

        public string UID = "Enter Your Tag UID";	//Tag UID, corresponding to the unique id of a tag

        protected NetworkIdentity netId;	//Network identity from UNET, used to get local player

        // Use this for initialization
        protected virtual void Start()
        {

            if (VRTracker.Manager.VRT_Manager.Instance.spectator)
            {
                gameObject.SetActive(false);
                return;
            }

            //Check if local player in UNET
            netId = transform.GetComponentInParent<NetworkIdentity>();
            if (netId != null && !netId.isLocalPlayer)
                return;

            VRTracker.Manager.VRT_Manager.Instance.AddTag(this);

            OnTrackingLost += SetLostColor;
            OnTrackingFound += SetFoundColor;

            VRT_TagEndpoint[] endpoints = GetComponentsInChildren<VRT_TagEndpoint>();
            foreach(VRT_TagEndpoint endpoint in endpoints){
                if (!trackedEndpoints.ContainsKey((int)endpoint.endpointID))
                {
                    trackedEndpoints.Add((int)endpoint.endpointID, endpoint);
                    endpoint.SetTag(this);
                }
            }
        }

        protected virtual void Update()
        {
            //UNET Check
            if (netId != null && !netId.isLocalPlayer)
                return;

            // For pairing purposes
			if (waitingForID)
            {
                currentTime -= Time.deltaTime;
                if (currentTime <= 0)
                {
                    //Assignation time off
                    currentTime = 0;
                    waitingForID = false;
                    IDisAssigned = false;
                }
            }
        }

        /// <summary>
        /// Reset Headset orientation and Tag orientation offset
        /// </summary>
        public virtual void ResetOrientation()
        {
            if (UnityEngine.XR.XRSettings.isDeviceActive)
                UnityEngine.XR.InputTracking.Recenter();
        }

		/// <summary>
		/// Raises the special command event.
		/// </summary>
		/// <param name="data">Data.</param>
        public virtual void OnSpecialCommand(string data)
        {
            if (data.Contains("triggeron" + data))
            {
                Debug.Log("trigger down");
                if (OnTriggerDown != null)
                {
                    Debug.Log("trigger down not null");
                    OnTriggerDown();
                }
                triggerPressed = true;
                triggerDown = true;
                triggerUp = false;
            }
            else if (data.Contains("triggeroff"))
            {
                if (OnTriggerUp != null)
                    OnTriggerUp();
                triggerPressed = false;
                triggerUp = true;
            }
            else if (data.Contains("buttonon"))
            {
                if (OnRedButtonDown != null)
                    OnRedButtonDown();
                buttonPressed = true;
                buttonDown = true;
                buttonUp = false;
            }
            else if (data.Contains("buttonoff"))
            {
                if (OnRedButtonUp != null)
                    OnRedButtonUp();
                buttonPressed = false;
                buttonUp = true;
            }
        }

		/// <summary>
		/// Raises the special command sent to all tags
		/// </summary>
		/// <param name="tagID">Tag I.</param>
		/// <param name="data">Data.</param>
        public virtual void OnSpecialCommandToAll(string tagID, string data)
        {
            if (waitingForID && data.Contains("buttonon"))
            {
                UID = tagID;
                IDisAssigned = true;
                waitingForID = false;
            }
        }

        /// <summary>
        /// Called when receiving updates about the Tag informations
        /// such as battery update, or status change
        /// </summary>
        /// <param name="status">Status.</param>
        /// <param name="battery">Battery.</param>
        /// <param name="version">Version.</param>
        public virtual void UpdateTagInformations(string status_, int battery_, string version_){
            if (status != status_)
            {
                // At start if not tracking the status is "unassigned"
                if(status_ == "unassigned" && OnTrackingLost != null)
                    OnTrackingLost();
                else if (status_ == "lost" && OnTrackingLost != null)
                    OnTrackingLost();
                else if (status_ == "tracking" && OnTrackingLost != null)
                    OnTrackingFound();

                status = status_;
            }
                

            battery = battery_;

            if(version != version_){
                if (version_.StartsWith("2", StringComparison.InvariantCulture))
                    tagVersion = TagVersion.V2;
                else if (version_.StartsWith("3", StringComparison.InvariantCulture))
                    tagVersion = TagVersion.V3;
                else if (version_.StartsWith("6", StringComparison.InvariantCulture))
                    tagVersion = TagVersion.Gun;
                else
                    Debug.LogWarning("Couldn't determine the Tag version of " + version_ + "  for tag UID " + UID);

                version = version_;
            }
        }

		/// <summary>
		/// Raises the tag data event.
		/// Handle the data received from the tag
		/// </summary>
		/// <param name="data">Data.</param>
        public virtual void OnTagData(string data)
        {
            //Debug.LogWarning(data);
            string[] sensors = data.Split(new string[] { "&s=" }, System.StringSplitOptions.RemoveEmptyEntries);
            for (int i = 1; i < sensors.Length; i++)
            {
                string[] parameters = sensors[i].Split('&');
                char[] sensorInfo = parameters[0].ToCharArray();
                if (sensorInfo.Length != 2)
                    return;
                Dictionary<string, string> values = new Dictionary<string, string>();
                for (int j = 1; j < parameters.Length; j++)
                {
                    string[] dict = parameters[j].Split('=');
                    values.Add(dict[0], dict[1]);
                }

                // IMU
                if (sensorInfo[0] == '1')
                {
                    Vector3 rec_orientation;
                    Vector3 rec_acceleration;

                    double f;
                    double.TryParse(values["ox"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out f);
                    rec_orientation.x = (float)f;
                    double.TryParse(values["oy"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out f);
                    rec_orientation.y = (float)f;
                    double.TryParse(values["oz"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out f);
                    rec_orientation.z = (float)f;

                    double.TryParse(values["ax"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out f);
                    rec_acceleration.x = (float)(f * (9.80665 / 1000.0f));
                    double.TryParse(values["ay"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out f);
                    rec_acceleration.y = (float)(f * (9.80665 / 1000));
                    double.TryParse(values["az"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out f);
                    rec_acceleration.z = (float)(f * (9.80665 / 1000));

                    VRT_TagEndpoint endpoint = gameObject.AddComponent<VRT_TagEndpoint>();
                    endpoint.SetTag(this);
                    if (!trackedEndpoints.ContainsKey(0))
                        trackedEndpoints.Add(0, endpoint);
                    trackedEndpoints[0].UpdateOrientationAndAcceleration(rec_orientation, rec_acceleration);
                }

                // Trackpad
                else if (sensorInfo[0] == '3')
                {
                    string press = values["st"];
                    if (press == "2")
                    {
                        trackpadTouch = false;
                        trackpadUp = true;
                    }
                    else if (press == "1" || press == "3")
                    {
                        trackpadTouch = true;
                        trackpadDown = true;
                    }
                    float a, b;
                    float.TryParse(values["x"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out a);
                    trackpadXY.y = -(a - (Mathf.Abs(trackpadMaxLeft - trackpadMaxRight) / 2)) / Mathf.Abs(trackpadMaxLeft - trackpadMaxRight);
                    float.TryParse(values["y"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out b);
                    trackpadXY.x = -(b - (Mathf.Abs(trackpadMaxUp - trackpadMaxDown) / 2)) / Mathf.Abs(trackpadMaxUp - trackpadMaxDown);
                    if (a == 0.0f && b == 0.0f)
                        trackpadXY = Vector2.zero;
                }
            }
        }

		
        public virtual void UpdateTrackpadData(float x, float y, byte button){
            if (button == 2)
            {
                trackpadTouch = false;
                trackpadUp = true;
            }
            else if (button == 1 || button == 3)
            {
                trackpadTouch = true;
                trackpadDown = true;
            }

            trackpadXY.y = x;
            trackpadXY.x = y;
        }

        public virtual void UpdateGunData(bool trigger, bool grab, bool joystick, bool a, bool b, bool x, bool y){
            if (this.trigger != trigger)
            {
                this.trigger = trigger;
                if (trigger && OnTriggerDown != null)
                    UnityMainThreadDispatcher.Instance().Enqueue(OnTriggerDown);
                else if (OnTriggerUp != null)
                    UnityMainThreadDispatcher.Instance().Enqueue(OnTriggerUp);
            }

            if (this.grab != grab)
            {
                this.grab = grab;
                if (grab && OnGrab != null)
                    UnityMainThreadDispatcher.Instance().Enqueue(OnGrab);
                else if (OnRelease != null)
                    UnityMainThreadDispatcher.Instance().Enqueue(OnRelease);
            }

            if (this.joystick != joystick)
            {
                this.joystick = joystick;
                if (joystick && OnJoystickPressed != null)
                    UnityMainThreadDispatcher.Instance().Enqueue(OnJoystickPressed);
                else if (OnJoystickReleased != null)
                    UnityMainThreadDispatcher.Instance().Enqueue(OnJoystickReleased);
            }

            if (this.a != a)
            {
                this.a = a;
                if (a && OnAPressed != null)
                    UnityMainThreadDispatcher.Instance().Enqueue(OnAPressed);
                else if (OnAReleased != null)
                    UnityMainThreadDispatcher.Instance().Enqueue(OnAReleased);
            }

            if (this.b != b)
            {
                this.b = b;
                if (b && OnBPressed != null)
                    UnityMainThreadDispatcher.Instance().Enqueue(OnBPressed);
                else if (OnBReleased != null)
                    UnityMainThreadDispatcher.Instance().Enqueue(OnBReleased);
            }

            if (this.x != x)
            {
                this.x = x;
                if (x && OnXPressed != null)
                    UnityMainThreadDispatcher.Instance().Enqueue(OnXPressed);
                else if (OnXReleased != null)
                    UnityMainThreadDispatcher.Instance().Enqueue(OnXReleased);
            }

            if (this.y != y)
            {
                this.y = y;
                if (y && OnYPressed != null)
                    UnityMainThreadDispatcher.Instance().Enqueue(OnYPressed);
                else if (OnYReleased != null)
                    UnityMainThreadDispatcher.Instance().Enqueue(OnYReleased);
            }

        }


		/// <summary>
		/// Waits for assignation in pairing phase
		/// </summary>
		/// <returns>The for assignation.</returns>
		/// <param name="delayToPressButton">Delay to press button.</param>
        public virtual IEnumerator WaitForAssignation(float delayToPressButton)
        {
            //Prepare for assignation
            currentTime = delayToPressButton;
            waitingForID = true;
            while (!IDisAssigned && waitingForID)
            {
                yield return new WaitForSeconds(1);
            }
        }
       
		/// <summary>
		/// Raises the destroy event and remove the tag from the list in the manager
		/// </summary>
        protected virtual void OnDestroy()
        {
            if (VRTracker.Manager.VRT_Manager.Instance)
                VRTracker.Manager.VRT_Manager.Instance.RemoveTag(this);
        }

		/// <summary>
		/// Assigns the tag and ask for assignment in the gateway
		/// Used for automatic pairing
		/// </summary>
		/// <param name="tagID">Tag unique id</param>
        public virtual void AssignTag(string tagID)
        {
            UID = tagID;
            IDisAssigned = true;
            waitingForID = false;
            VRTracker.Manager.VRT_Manager.Instance.vrtrackerWebsocket.AssignTag(tagID);
        }

        /// <summary>
        /// Sets the tracker color.
        /// </summary>
        /// <param name="color">Color.</param>
        public virtual void SetColor(Color color){
            VRTracker.Manager.VRT_Manager.Instance.vrtrackerWebsocket.SetTagColor(UID, (int)(255 * color.r), (int)(255 * color.g), (int)(255 * color.b));
        }

        private void SetFoundColor(){
            SetColor(Color.green);
        }

        private void SetLostColor()
        {
            SetColor(Color.yellow);
        }
    }
}
