using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRTracker.Boundary
{
    /// <summary>
    /// VR Tracker
    /// Draws a circle
    /// Used to show player boundary to others.
    /// </summary>

	[RequireComponent(typeof(LineRenderer))]
	public class VRT_PlayerBoundaryRenderer : MonoBehaviour 
	{
		[Range(0.1f, 10f)]
		public float radius = 1.0f;

		[Range(3, 256)]
		public int numSegments = 128;

		public LineRenderer lineRenderer;
		private float alpha = 0.0f; // Transparency

		void Start ( ) 
		{
            if(lineRenderer == null)
			{
    			lineRenderer = GetComponent<LineRenderer>();
            	lineRenderer.enabled = false;
            }
            lineRenderer.material = new Material(Shader.Find("Particles/Additive"));
            lineRenderer.positionCount = numSegments;
            lineRenderer.useWorldSpace = false;
            DoRenderer();
		}

        void Update()
        {
            DoRenderer();
        }

		/// <summary>
		/// Update the transparency of the boundaries
		/// </summary>
		public void DoRenderer ( )
		{
			if (alpha <= 0.0)
				return;
			lineRenderer.material = new Material(Shader.Find("Particles/Additive"));
			lineRenderer.positionCount = numSegments;
			lineRenderer.useWorldSpace = false;

			float deltaTheta = (float) (2.0 * Mathf.PI) / numSegments;
			float theta = 0f;

			for (int i = 0 ; i < numSegments; i++) 
			{
				float x = radius * Mathf.Cos(theta);
				float z = radius * Mathf.Sin(theta);
				Vector3 pos = new Vector3(x, 0, z);
				lineRenderer.SetPosition(i, pos);
				theta += deltaTheta;
			}
		}


		public void SetAlpha(float value)
		{
			if (alpha == value)
				return;
			else if (value == 0.0f) 
			{
				lineRenderer.enabled = false;
			} else if (alpha == 0.0f)
			{
				lineRenderer.enabled = true;
			}
			else 
			{
				Color c = lineRenderer.startColor;
				c.a = value;
                lineRenderer.startColor = c;
                lineRenderer.endColor = c;
			}
			alpha = value;
		}
	}
}
