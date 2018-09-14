using UnityEngine;
using System.Collections;

/**
 * VR Tracker
 * Resize the texture using world space value
 * to avoid stretching textures.
 * Especially usefull for the boundary system
 **/

namespace VRTracker.Boundary
{
	/// <summary>
	/// TextureResize help display the size of boundaries to fit in the editor
	/// </summary>
	[ExecuteInEditMode]
	public class TextureResize : MonoBehaviour 
	{
		public float scaleFactor = 5.0f;
		Material mat;
		// Use this for initialization
		void Start () 
		{
			GetComponent<Renderer>().material.mainTextureScale = new Vector2 (transform.localScale.x / scaleFactor , transform.localScale.z / scaleFactor);
		}

		// Update is called once per frame
		void Update () 
		{

			if (transform.hasChanged && Application.isEditor && !Application.isPlaying) 
			{
				GetComponent<Renderer>().material.mainTextureScale = new Vector2 (transform.localScale.x / scaleFactor , transform.localScale.z / scaleFactor);
				transform.hasChanged = false;
			} 

		}

		// To force in game resize (when receiving boundaries size for example)
		public void Resize()
		{
			GetComponent<Renderer>().material.mainTextureScale = new Vector2 (transform.localScale.x / scaleFactor , transform.localScale.z / scaleFactor);
		}
	}
}