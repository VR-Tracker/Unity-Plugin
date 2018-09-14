using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRTracker.Spectator
{
    /// <summary>
    /// Script to be put on a camera to create a rotating effect around the "lookAt" point
    /// </summary>
    public class VRT_RotatingCamera : MonoBehaviour
    {

        public float speed = 1f;
        [Tooltip("Point the Camera should look at")]
        [SerializeField] Transform lookat;
        [SerializeField] bool rotating;

        // Update is called once per frame
        void Update()
        {
            if (lookat != null)
            {
                transform.LookAt(lookat.position, lookat.up);
                if(rotating)
                {
                    transform.RotateAround(lookat.position, new Vector3(0.0f, 1.0f, 0.0f), Time.deltaTime * speed);
                }                
            }
            else if(rotating)
            {
                transform.RotateAround(Vector3.zero, new Vector3(0.0f, 1.0f, 0.0f), Time.deltaTime * speed);
            }
        }
    }
}