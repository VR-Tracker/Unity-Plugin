using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* This script will look for other gameobject with the same name on awake
 * and auto destroy if another is found.
 * The goal is to avoid duplicate gameobjects when moving to the pairing scene and back
 * to the original scene
 */
namespace VRTracker.Utils
{
    public class SingletonGameObjectName : MonoBehaviour
    {
        // Start is called before the first frame update
        void Awake()
        {
            int count = 0;
            foreach (var gameObj in FindObjectsOfType(typeof(GameObject)) as GameObject[])
            {
                if (gameObj.name == this.gameObject.name)
                    count++;
            }
            if(count > 1)
                Destroy(gameObject);
        }
        
    }
}