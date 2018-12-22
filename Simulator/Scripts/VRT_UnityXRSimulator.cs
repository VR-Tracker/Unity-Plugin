using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using VRTracker.Manager;


namespace VRTracker.Manager
{
    public class VRT_UnityXRSimulator : VRT_Tag
    {

        public enum TagType
        {
            Head,               // Track the user head
            LeftController,     // Track the user left controller
            RightController    // Track the user right controller
        }

        // Start is called before the first frame update
        protected override void Start()
        {

        }

        // Update is called once per frame
        protected override void Update()
        {

        }



        public override void ResetOrientation()
        {
            if (UnityEngine.XR.XRSettings.isDeviceActive)
                UnityEngine.XR.InputTracking.Recenter();
        }

        public override void OnSpecialCommand(string data)
        {
            // Not implemented in simulator 
        }

        public override void OnSpecialCommandToAll(string tagID, string data)
        {
            // Not implemented in simulator
        }

        public override void UpdateTagInformations(string status_, int battery_, string version_)
        {
            // Not implemented in simulator
        }

        public override void OnTagData(string data)
        {
            // Not implemented in simulator
        }

        public override void UpdateTrackpadData(float x, float y, byte button)
        {
            // Not implemented in simulator
        }

        public override void UpdateGunData(bool trigger, bool grab, bool joystick, bool a, bool b, bool x, bool y)
        {
            // Not implemented in simulator
        }

        public override IEnumerator WaitForAssignation(float delayToPressButton)
        {
            // Not implemented in simulator
        }

        protected override void OnDestroy()
        {
            // Not implemented in simulator
        }

        public override void AssignTag(string tagID)
        {
            // Not implemented in simulator
        }

        public override void SetColor(Color color)
        {
           // Not implemented in simulator
        }
    }
}