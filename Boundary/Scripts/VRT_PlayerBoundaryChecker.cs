using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;
using VRTracker.Player;

namespace VRTracker.Boundary
{
    /// <summary>
    /// VR Tracker
    /// Calculates the distance from the gameobject where this script is attached
    /// to other players.
    /// </summary>

	public class VRT_PlayerBoundaryChecker : NetworkBehaviour 
	{

		private List<VRT_PlayerBoundaryRenderer> playersBoundary; // References to other players boundaries

		[SerializeField]
		private NetworkIdentity playerNetworkIdentity; // Reference to local player network identity

		[SerializeField]
		private VRTracker.Network.VRT_NetworkManager networkManager;

		[SerializeField]
		private VRT_PlayerBoundaryRenderer playerBoundary;

		[Tooltip("Distance to start fading in the boundary")]
		[SerializeField]
		private float distanceStartFade = 2.0f; // Distance for fading to start for the player's boundaries

		[Tooltip("Distance at which the boundary is fully visible")]
		[SerializeField]
		private float distanceEndFade = 1.0f;

		// Use this for initialization
		void Start () 
		{

            if (networkManager == null && FindObjectsOfType<VRTracker.Network.VRT_NetworkManager>().Length > 0)
                networkManager = FindObjectsOfType<VRTracker.Network.VRT_NetworkManager>()[0];

			playersBoundary = new List<VRT_PlayerBoundaryRenderer> ();

			if (playerNetworkIdentity == null) 
			{
				playerNetworkIdentity = GetComponentInParent<NetworkIdentity> ();
			}

			if (!playerNetworkIdentity.isLocalPlayer)
				return;
			
			networkManager.OnPlayerJoin += OnPlayerJoin;
			networkManager.OnPlayerLeave += OnPlayerLeave;

			if (playerBoundary == null)
				playerBoundary = gameObject.GetComponent<VRT_PlayerBoundaryRenderer> ();
			
			if (playerBoundary == null)
				Debug.LogError ("The player does not have a PlayerBoundaryRenderer");

			FindPlayersBoundary ();
		}
		
		// Update is called once per frame
		void Update () 
		{
			if(playerNetworkIdentity.isLocalPlayer)
				UpdateBoundaries ();
		}

        /// <summary>
        /// Update the transparency of each other player boundaries depending on the distance to them
        /// </summary>
		void UpdateBoundaries()
		{
            foreach (VRT_PlayerBoundaryRenderer renderer in playersBoundary)
            {
                if (renderer != null)
                {
                    float distance = GetDistanceToPlayer(renderer.gameObject);
					if (distance < distanceEndFade)
					{
						renderer.SetAlpha (1.0f);
					} 
					else if (distance < distanceStartFade) 
					{
						renderer.SetAlpha ((float)((distanceStartFade - distanceEndFade) - (distanceStartFade - distanceEndFade) * ((distance - distanceEndFade) / (distanceStartFade - distanceEndFade))));
					} 
					else 
					{
						renderer.SetAlpha(0.0f);
					}
                }
            }
		}

        /// <summary>
        /// Finds all players boundaries using the PlayerBoundary Tag.
        /// </summary>
		void FindPlayersBoundary()
		{
			playersBoundary = new List<VRT_PlayerBoundaryRenderer> ();
			GameObject[] playerBoundaryObjects = GameObject.FindGameObjectsWithTag ("PlayerBoundary");
            
            // Find boundaries using Tag
			foreach (GameObject playerBoundaryObject in playerBoundaryObjects) 
			{
                VRT_PlayerBoundaryRenderer renderer = playerBoundaryObject.GetComponent<VRT_PlayerBoundaryRenderer> ();
                if(playerBoundaryObject != null && renderer != null && renderer != playerBoundary)
                    playersBoundary.Add(playerBoundaryObject.GetComponent<VRT_PlayerBoundaryRenderer>());
			}
		}

        /// <summary>
        /// Get the 2D distance from this object to the object in argument
        /// </summary>
        /// <returns>The distance to player.</returns>
        /// <param name="player">Player.</param>
		float GetDistanceToPlayer(GameObject player)
		{
			float distanceToPlayer = Mathf.Sqrt ((player.transform.position.x-transform.position.x)*(player.transform.position.x-transform.position.x) + (player.transform.position.z-transform.position.z)*(player.transform.position.z-transform.position.z));
			return distanceToPlayer;
		}

        /// <summary>
        /// Callback to handle new player joining the game
        /// </summary>
        /// <param name="player">Player.</param>
		void OnPlayerJoin(VRT_PlayerInstance player)
		{
			VRT_PlayerBoundaryRenderer playerBoundaryRenderer = player.GetComponent<VRT_PlayerBoundaryRenderer> ();
			if(playerBoundaryRenderer != null)
				playersBoundary.Add(playerBoundaryRenderer);
		}

        /// <summary>
        /// Callback to handle new player leaving the game
        /// </summary>
        /// <param name="player">Player.</param>
		void OnPlayerLeave(VRT_PlayerInstance player)
		{
			VRT_PlayerBoundaryRenderer playerBoundaryRenderer = player.GetComponent<VRT_PlayerBoundaryRenderer> ();
			if(playerBoundaryRenderer != null)
				playersBoundary.Remove(playerBoundaryRenderer);
		}
	}
}
