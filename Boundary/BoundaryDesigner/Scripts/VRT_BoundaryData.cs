using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using VRTracker.Manager;
using SimpleJSON;
using System.IO;

namespace VRTracker.Boundary
{
	/// <summary>
	/// Store the information for the VR Tracker boundary system.
	/// </summary>
    public class VRT_BoundaryData : MonoBehaviour
    {
        [SerializeField] GameObject corner; ///Corner in the play area that is a boundary
        LineRenderer line; ///Use to trace the boundary wall
        Transform target = null;
        List<Transform> cornersPositionsList = new List<Transform>(); ///List of all the corners of the boundary
        bool assignedPoint = false;
        [SerializeField] private string JsonFilePath = "Corners_Data.json";
        private VRTracker.Player.VRT_FollowTag followTag;

        void Start()
        {
            if (followTag == null)
                followTag = GetComponent<VRTracker.Player.VRT_FollowTag>();

            if (followTag != null)
                followTag.OnTagJoin += TagJoined;
            
            if (followTag != null && followTag.tagToFollow != null)
                followTag.tagToFollow.OnRedButtonDown += OnTagButtonPressed;

            line = GetComponentInChildren<LineRenderer>();
        }

        /// <summary>
        /// Callback called when the Tag in VRT_FollowTag joins
        /// the system.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">E.</param>
        private void TagJoined(object sender, EventArgs e)
        {
            if (followTag != null && followTag.tagToFollow != null)
                followTag.tagToFollow.OnRedButtonDown += OnTagButtonPressed;
        }

        /// <summary>
        /// Callback called when the red button on the Tag is pressed
        /// in order to create a new corner in the boundaries
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">E.</param>
        private void OnTagButtonPressed()
		{
            SetCorner();
        }

		/// <summary>
		/// Create a corner with the tag position
		/// </summary>
        void SetCorner()
        {
            if (!assignedPoint)
            {
                InstantiateCorner(transform.position);
            }
            else
            {
                CompleteDrawing();
                SaveCornerToJSON();
            }
        }

        void Update()
        {
			//Can press the return button instead of the red one
            if (Input.GetKeyDown(KeyCode.Return))
                SetCorner();
			
			//Backspace will clear the boundaries
            if (Input.GetKeyDown(KeyCode.Backspace))
                ClearBoundaries();

            if (target != null)
            {
                line.SetPosition(0, transform.position);
                line.SetPosition(1, target.position);
            }
            else
            {
                line.SetPosition(0, transform.position);
                line.SetPosition(1, transform.position);
            }
        }

		/// <summary>
		/// Clear the boundaries
		/// </summary>
        void ClearBoundaries()
        {
            for (int i = cornersPositionsList.Count - 1; i >= 0; i--)
            {
                GameObject corner = cornersPositionsList[i].gameObject;
                cornersPositionsList.RemoveAt(i);
                Destroy(corner);
            }
            cornersPositionsList.Clear();
            string filePath = Path.Combine(Application.persistentDataPath, JsonFilePath);
            string content = "";
            File.WriteAllText(filePath, content);
        }

		/// <summary>
		/// Draw a wall between two point, from the previous to the new one
		/// </summary>
        void DrawLineToPrevious()
        {
            int currentIndex = cornersPositionsList.Count - 1;
            cornersPositionsList[currentIndex].GetComponentInChildren<LineRenderer>().SetPosition(0, cornersPositionsList[currentIndex].position);
            cornersPositionsList[currentIndex].GetComponentInChildren<LineRenderer>().SetPosition(1, cornersPositionsList[currentIndex - 1].position);
        }

		/// <summary>
		/// End the boundary with the last wall
		/// </summary>
        void DrawLineToFirst()
        {
            int currentIndex = cornersPositionsList.Count - 1;
            cornersPositionsList[0].GetComponentInChildren<LineRenderer>().SetPosition(0, cornersPositionsList[0].position);
            cornersPositionsList[0].GetComponentInChildren<LineRenderer>().SetPosition(1, cornersPositionsList[currentIndex].position);
        }

		/// <summary>
		/// Create a corner with 3D position
		/// </summary>
        GameObject InstantiateCorner(Vector3 pos)
        {
            GameObject createdCorner = Instantiate(corner, pos, corner.transform.rotation);
            cornersPositionsList.Add(createdCorner.transform);
            target = createdCorner.transform;

            if (cornersPositionsList.Count >= 2)
            {
                DrawLineToPrevious();
            }

            return createdCorner;
        }


		/// <summary>
		/// Save the boundary data in a JSON file
		/// </summary>
        void SaveCornerToJSON()
        {
            JSONNode cornerList = new JSONArray();
            JSONNode cornerValue = new JSONObject();

            for (int i = 0; i < cornersPositionsList.Count; i++)
            {
                cornerValue["x"] = cornersPositionsList[i].position.x;
                cornerValue["z"] = cornersPositionsList[i].position.z;
                cornerList.Add(cornerValue);
                cornerValue = new JSONObject();
            }

            string filePath = Path.Combine(Application.persistentDataPath, JsonFilePath);
            string content = cornerList.ToString();
            File.WriteAllText(filePath, content);
        }

		/// <summary>
		/// Load the boundary data from file and draw the boundaries .
		/// </summary>
        public JSONNode LoadCornersData()
        {
            JSONNode cornerList = null;
            string filePath = Path.Combine(Application.persistentDataPath, JsonFilePath);
            Debug.Log("Opening " + filePath);

            if (File.Exists(filePath))
            {
                // Read the json from the file into a string
                string jsonDataString = File.ReadAllText(filePath);
                cornerList = JSON.Parse(jsonDataString);

                if (cornerList != null)
                {
                    for (int i = 0; i < cornerList.Count; i++)
                    {
                        Vector3 vectorPos = new Vector3(cornerList[i]["x"], 2.5f, cornerList[i]["z"]);
                        InstantiateCorner(vectorPos);
                    }
                    CompleteDrawing();
                }
            }
            else
            {
                Debug.LogWarning("Cannot load json file!");
            }
            return cornerList;
        }

        void CompleteDrawing()
        {
            Debug.Log("completed");
            DrawLineToFirst();
            target = null;
            DisplayPositionList();
        }

        void DisplayPositionList()
        {
            for (int i = 0; i < cornersPositionsList.Count; i++)
            {
                Debug.Log(cornersPositionsList[i].position);
            }
        }

        private void OnTriggerEnter(Collider collision)
        {
            assignedPoint = true;
        }

        private void OnTriggerExit(Collider other)
        {
            assignedPoint = false;
        }

		public void OnDestroy()
		{
            if (followTag != null)
                followTag.OnTagJoin -= TagJoined;

            if (followTag != null && followTag.tagToFollow != null)
                followTag.tagToFollow.OnRedButtonDown -= OnTagButtonPressed;
		}
	}
}

