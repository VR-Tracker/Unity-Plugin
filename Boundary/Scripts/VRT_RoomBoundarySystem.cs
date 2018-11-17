using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using SimpleJSON;
using System.IO;
using System;
using VRTracker.Manager;


namespace VRTracker.Boundary
{
    /// <summary>
    /// VR Tracker
    /// The boundary system generates walls linking each "corners".
    /// When playing it will continuously check for the distance between the Tag and walls
    /// and make them appear to the User if he is too close.
    /// </summary>

    public class VRT_RoomBoundarySystem : MonoBehaviour
    {
        [Tooltip("The distance from the Tags to the walls at which the walls start to fade in")]
        public float distanceStartFade = 1.5f; //Distance before fading the boundaries
        public List<Vector3> corners; //List of boundaries corners
        public GameObject boundaryWall; //Boundaries game object
        private List<GameObject> walls; //List of each wall
        [SerializeField] private static string JsonFilePath = "Corners_Data.json";
        private float generalAlpha = 0.0f;
        public Action cornersLoaded;

        //Alert sound when approaching/crossing boundary
        [SerializeField] AudioSource alertAudio;
        [SerializeField] AudioMixer audioMixer;
        [SerializeField] float alertFrequence;
        [SerializeField] float alertDistance;
        [SerializeField] float alertVolume;
        Coroutine alertRoutine;
        bool alertSoundPlaying = false;

		public static event Action OnNewBoundaries;  // Called when new boundaries are set
		public static bool exist = false; //Variable to know if the object exist, knowing if the data has been updated
											//WARNING: This will be removed in a next update


        // Generate the walls using data of each corner
        void Start()
        {
            walls = new List<GameObject>();
            LoadCornersData();
			//Don't draw the boundaries for spectator
            if (!VRTracker.Manager.VRT_Manager.Instance.spectator)
            {
                DrawBoundaries();
            }
			OnNewBoundaries += UpdateBoundaries;
			exist = true;
        }

        public void UpdateBoundaries()
        {
            LoadCornersData();
            DrawBoundaries();
        }

		/// <summary>
		/// Load the boundary and return the corners data 
		/// </summary>
		/// <returns>The corners data.</returns>
        JSONNode LoadCornersData()
        {
            JSONNode cornerList = null;
            string filePath = Path.Combine(Application.persistentDataPath, JsonFilePath);
			Debug.Log(filePath);
            if (File.Exists(filePath))
            {
                // Read the json from the file into a string
                string jsonDataString = File.ReadAllText(filePath);
                cornerList = JSON.Parse(jsonDataString);

                if (cornerList != null)
                {
                    corners.Clear();
                    for (int i = 0; i < cornerList.Count; i++)
                    {
                        Vector3 vectorPos = new Vector3(cornerList[i]["x"], 0, cornerList[i]["z"]);
                        corners.Add(new Vector3(vectorPos.x, 0, vectorPos.z));
                    }
                }
                if (VRT_Manager.Instance.spectator)
                {
                    cornersLoaded();
                }

            }
            else
            {
                Debug.LogWarning("Cannot load json file!");
            }
            return cornerList;
        }

		/// <summary>
		/// Draws the boundaries.
		/// </summary>
        void DrawBoundaries()
        {
            walls.Clear();
            for (int i = 0; i < corners.Count; i++)
            {
                float distanceToNextPoint = Mathf.Sqrt((corners[i].x - corners[(i + 1) % corners.Count].x) * (corners[i].x - corners[(i + 1) % corners.Count].x) 
											+ (corners[i].z - corners[(i + 1) % corners.Count].z) * (corners[i].z - corners[(i + 1) % corners.Count].z));
                float angle = Vector3.Angle(Vector3.left, corners[(i + 1) % corners.Count] - corners[i]);

                GameObject bound = (GameObject)Instantiate(boundaryWall);
                bound.transform.parent = this.transform;

                Vector3 scale = bound.transform.localScale;
                scale.x = distanceToNextPoint / 10;
                bound.transform.localScale = scale;

				//Set the boundarie gameobject position
                Vector3 position = bound.transform.position;
                position.x = corners[i].x + (corners[(i + 1) % corners.Count].x - corners[i].x) / 2;
                position.z = corners[i].z + (corners[(i + 1) % corners.Count].z - corners[i].z) / 2;
                bound.transform.position = position;

				//Set the boundarie gameobject rotation
                Vector3 rotation = bound.transform.localRotation.eulerAngles;
                rotation.y = angle;
                bound.transform.localRotation = Quaternion.Euler(rotation);

                TextureResize textureResize = bound.GetComponent<TextureResize>();
                if (textureResize)
                    textureResize.Resize();

                walls.Add(bound);
            }
			/*
			 * Enable sound when player go outside the boundaries
                if(walls.Count > 1)
                {
                    BoxCollider collider = corners[1].GetComponent<VRT_TriggerAlert>().collider;
                    collider.center = new Vector3(0, -35, 0);
                }
                */
            ChangeGlobalWallsAlpha(1.0f);
        }
			
		/// <summary>
		/// Changes the global walls alpha.
		/// </summary>
		/// <param name="value">Value.</param>
        void ChangeGlobalWallsAlpha(float value)
        {
            generalAlpha = value;
            foreach (GameObject wall in walls)
            {
                TextureAlpha textureAlpha = wall.GetComponent<TextureAlpha>();
                if (textureAlpha)
                    textureAlpha.SetAlpha(value);
            }
        }
			
		/// <summary>
		/// Gets the shortest distance to walls and return the shortest distance;
		/// </summary>
		/// <returns>The shortest distance to walls.</returns>
        public float GetShortestDistanceToWalls()
        {
            float distance = 1000.0f;
            if (VRTracker.Manager.VRT_Manager.Instance == null)
                return distance;

            foreach (VRTracker.Manager.VRT_Tag tag in VRTracker.Manager.VRT_Manager.Instance.tags)
            {
                foreach (GameObject wall in walls)
                {
                    // Get the distance from point to plane by using projection
                    Vector3 V1 = tag.transform.position - wall.transform.position;
                    Vector3 V2 = Vector3.Project(V1, wall.transform.up);
                    if (distance > V2.magnitude)
                        distance = V2.magnitude;
                }
            }
            return distance;
        }

        // Update is called once per frame
        void Update()
        {
            float distance = GetShortestDistanceToWalls();
            if (distance < distanceStartFade)
            {
                float closeToWallValue = (float)(distanceStartFade - distanceStartFade * (distance / distanceStartFade));

                ChangeGlobalWallsAlpha(closeToWallValue);

            }
            else if (generalAlpha != 0.0f)
            {
                ChangeGlobalWallsAlpha(0.0f);
            }
        }

		/// <summary>
		/// Launch the alert sound
		/// For when the player is getting to close to the boundaries
		/// </summary>
        public void PlayAlert()
        {
            if (!alertSoundPlaying)
            {
                audioMixer.SetFloat("beepVolume", alertVolume);
                alertRoutine = StartCoroutine(PlayAlertSound());
            }
        }

		/// <summary>
		/// Stop the alert sound 
		/// For when player is getting far enough of the boundaries
		/// </summary>
        public void StopAlert()
        {
            if (alertSoundPlaying)
            {
                alertSoundPlaying = false;
                StopCoroutine(alertRoutine);
            }
        }

        IEnumerator PlayAlertSound()
        {
            alertSoundPlaying = true;

            while (alertSoundPlaying)
            {
                float volume = 0;
                audioMixer.GetFloat("beepVolume", out volume);

                alertAudio.Play();
                yield return new WaitForSeconds(alertFrequence);
            }
        }

		/// <summary>
		/// Saves the boundaries into a Json file
		/// </summary>
		/// <param name="content">boundaries file data in string</param>
        public static void SaveBoundaries(string content)
        {
            string filePath = Path.Combine(Application.persistentDataPath, JsonFilePath);
            File.WriteAllText(filePath, content);
			if (OnNewBoundaries != null)
				OnNewBoundaries ();
        }

		/// <summary>
		/// Creates the boundaries from limit send from the dashboard
		/// Store the data in the corners_data file
		/// </summary>
		/// <param name="xmin">Xmin value for boundary</param>
		/// <param name="xmax">Xmax value for boundary</param>
		/// <param name="ymin">Ymin value for boundary</param>
		/// <param name="ymax">Ymax value for boundary</param>
		public static void CreateBoundariesFromLimit(float xmin, float xmax, float ymin, float ymax)
		{
			//Create the date to save in the coners_data file
			JSONNode cornerList = new JSONArray();
			JSONNode cornerValue = new JSONObject();

			cornerValue["x"] = xmin;
			cornerValue["z"] = ymin;
			cornerList.Add(cornerValue);
			cornerValue = new JSONObject();

			cornerValue["z"] = ymax;
			cornerList.Add(cornerValue);
			cornerValue = new JSONObject();

			cornerValue["x"] = xmax;
			cornerList.Add(cornerValue);
			cornerValue = new JSONObject();

			cornerValue["z"] = ymin;
			cornerList.Add(cornerValue);

			string filePath = Path.Combine(Application.persistentDataPath, JsonFilePath);
			string content = cornerList.ToString();
			if (!File.Exists (filePath)) 
			{
				File.WriteAllText (filePath, content);
				if (OnNewBoundaries != null)
					OnNewBoundaries ();
			} 
			else 
			{
				if (VRT_RoomBoundarySystem.exist) 
				{
					if (OnNewBoundaries != null)
						OnNewBoundaries ();
				}
				//In the other case it is just the file that has been sent by the Gateway when started
			}
		}
    }
}
