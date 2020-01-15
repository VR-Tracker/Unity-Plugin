using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRTracker.Spectator
{
    /// <summary>
    /// Script to be put on a camera to create a travelling effect between "from" and "to" positions
    /// </summary>
    public class VRT_TravellingCamera : MonoBehaviour
    {

        public float duration = 5f;
        [Tooltip("Origin Camera position")]
        public Transform from;
        [Tooltip("Destination Camera position")]
        public Transform to;
        [Tooltip("Check if the Camera should travel in both directions (from --> to AND to --> from)")]
        public bool returnToPosition = true;
        private float t;

        private Vector3 position1;
        private Vector3 position2;
        [SerializeField] Transform lookat;

        // Use this for initialization
        void Start()
        {
            position1 = from.position;
            position2 = to.position;
        }

        // Update is called once per frame
        void Update()
        {            
            t += Time.deltaTime / duration;
            transform.position = Vector3.Lerp(position1, position2, t);
            if (t > 1.0f)
            {
                t = 0;
                if (returnToPosition)
                {
                    Vector3 temp = position1;
                    position1 = position2;
                    position2 = temp;
                }
            }
            if(lookat != null)
                transform.LookAt(lookat.position, Vector3.up);
            else
                transform.LookAt(Vector3.zero, Vector3.up);
        }
    }
}