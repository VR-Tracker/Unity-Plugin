using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * VR Tracker
 * Change the alpha of the standard shader of the material 
 * on this object
 * Used for boundary system
 **/

namespace VRTracker.Boundary
{
	/// <summary>
	/// Handle the transparency of the boundary 
	/// </summary>
	public class TextureAlpha : MonoBehaviour 
	{
        [SerializeField] Renderer rend;
		[SerializeField]
		float alpha = 0.0f;

		public void SetAlpha(float value)
		{
            //Debug.Log("texturealpha script changed to " + value);

            alpha = value;
			Color color = rend.materials[0].color;
			color.a = alpha;
            rend.materials[0].color = color;
		}
	}
}
