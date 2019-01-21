using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using VRTracker.Interaction;

namespace VRTracker.Network
{

    using UnityEngine.Networking;

    /* VR Tracker
     * Ce script doit etre attaché sur le player prefab (a son root)
     * Les fonctions permettent de Spawn / Delete des object sur le Network et d'assigner
     * ou retirer les authority
     */

    [RequireComponent(typeof(NetworkIdentity))]
    public class VRT_NetworkAuthority : NetworkBehaviour
    {

        public NetworkIdentity playerNetworkIndentity;
        private NetworkManager networkManager;

        // Use this for initialization
        void Start()
        {
            if (playerNetworkIndentity == null)
                playerNetworkIndentity = GetComponent<NetworkIdentity>();

            if (networkManager == null)
                networkManager = NetworkManager.singleton;
        }

        /// <summary>
        /// This a function is executed client side.
        /// Remove the Authority for the player where this script is, upon an object
        /// identified by its NetworkInstanceId
        /// </summary>
        /// <param name="objectId">NetworkInstanceId of the object</param>
        public void RemAuth(NetworkInstanceId objectId)
        {
            if (playerNetworkIndentity.isClient)
                CmdRemAuth(objectId);
        }

        /// <summary>
        /// This a function is executed server side.
        /// Remove the Authority for the player where this script is, upon an object
        /// identified by its NetworkInstanceId
        /// </summary>
        /// <param name="objectId">NetworkInstanceId of the object</param>
        [Command]
        private void CmdRemAuth(NetworkInstanceId objectId)
        {
            GameObject iObject = NetworkServer.FindLocalObject(objectId); // Finds the object on the server scene (find by ID)
            NetworkIdentity objectNetworkIdentity = iObject.GetComponent<NetworkIdentity>(); // Look for the object Network Identity
            objectNetworkIdentity.RemoveClientAuthority(playerNetworkIndentity.connectionToClient); // Remove the authority for this player 

            if (iObject.GetComponent<VRT_InteractableObject>())
                iObject.GetComponent<VRT_InteractableObject>().NotifyServerReleased();

        }


        /// <summary>
        /// This a function is executed CLIENT side.
        /// Set the Authority to the player where this script is, upon an object
        /// identified by its NetworkInstanceId
        /// </summary>
        /// <param name="objectId">NetworkInstanceId of the object</param>
        public void SetAuth(NetworkInstanceId objectId)
        {
            if (playerNetworkIndentity.isClient)
            {
                CmdSetAuth(objectId);
            }
        }

        /// <summary>
        /// This a function is executed SERVER side.
        /// Set the Authority to the player where this script is, upon an object
        /// identified by its NetworkInstanceId
        /// </summary>
        /// <param name="objectId">NetworkInstanceId of the object</param>
        [Command]
        private void CmdSetAuth(NetworkInstanceId objectId)
        {
            GameObject iObject = NetworkServer.FindLocalObject(objectId); // Finds the object on the server scene (find by ID)
            NetworkIdentity objectNetworkIdentity = iObject.GetComponent<NetworkIdentity>(); // Look for the object Network Identity
            NetworkConnection otherOwner = objectNetworkIdentity.clientAuthorityOwner; // Check who is the current owner of the object authority

            if(iObject.GetComponent<VRT_InteractableObject>())
                iObject.GetComponent<VRT_InteractableObject>().NotifyServerGrabbed();

            // First case : the player asking for the authority already has it
            if (otherOwner == playerNetworkIndentity.connectionToClient)
                return;
            else
            {
                // If another player currently has authority over the object, we remove its authority (in case case we could not want do that and prevent the player from "stealing" the authority)
                if (otherOwner != null)
                    objectNetworkIdentity.RemoveClientAuthority(otherOwner);

                // Set authority to the player
                objectNetworkIdentity.AssignClientAuthority(playerNetworkIndentity.connectionToClient);
            }
        }


        /// <summary>
        /// This a function is executed CLIENT side.
        /// Destroy an object identified by its NetworkInstanceId on all players and server
        /// </summary>
        /// <param name="objectId">NetworkInstanceId of the object</param>
        public void DestroyObject(NetworkInstanceId objectId)
        {
            if (playerNetworkIndentity.isClient)
                CmdDestroy(objectId);
        }

        /// <summary>
        /// This a function is executed SERVER side.
        /// Destroy an object identified by its NetworkInstanceId on all players and server
        /// </summary>
        /// <param name="objectId">NetworkInstanceId of the object</param>
        [Command]
        private void CmdDestroy(NetworkInstanceId objectId)
        {
            GameObject iObject = NetworkServer.FindLocalObject(objectId); // Finds the object on the server scene (find by ID)
            NetworkServer.Destroy(iObject);
            Destroy(iObject);
        }


        /// <summary>
        /// This a function is executed CLIENT side.
        /// Spawn an object on all clients with authority to the player spawning it
        /// </summary>
        /// <param name="objectToSpawn">prefab of the object to spawn</param>
        /// <param name="position">object position</param>
        /// <param name="rotation">object orientation</param>
        public void SpawnWithAuthority(GameObject objectToSpawn, Vector3 position, Quaternion rotation)
        {
            CmdSpawnWithAuthority(objectToSpawn.name, position, rotation);
        }


        /// <summary>
        /// This a function is executed SERVER side.
        /// Spawn an object on all clients with authority to the player spawning it
        /// </summary>
        /// <param name="objectToSpawn">name of the prefab of the object to spawn</param>
        /// <param name="position">object position</param>
        /// <param name="rotation">object orientation</param>
        [Command]
        private void CmdSpawnWithAuthority(string objName, Vector3 position, Quaternion rotation)
        {
            foreach (GameObject spawnPrefab in networkManager.spawnPrefabs)
            {
                if (spawnPrefab.name == objName)
                {
                    GameObject newObject = (GameObject)Instantiate(spawnPrefab, position, rotation);
                    NetworkServer.SpawnWithClientAuthority(newObject, connectionToClient);
                    return;
                }
            }
        }


        /// <summary>
        /// This a function is executed CLIENT side.
        /// Spawn an object on all players
        /// </summary>
        /// <param name="objectToSpawn">prefab of the object to spawn</param>
        /// <param name="position">object position</param>
        /// <param name="rotation">object orientation</param>
        public void Spawn(GameObject objectToSpawn, Vector3 position, Quaternion rotation)
        {
            CmdSpawn(objectToSpawn.name, position, rotation);
        }


        /// <summary>
        /// This a function is executed SERVER side.
        /// Spawn an object on all players
        /// </summary>
        /// <param name="objectToSpawn">name of the prefab of the object to spawn</param>
        /// <param name="position">object position</param>
        /// <param name="rotation">object orientation</param>
        [Command]
        private void CmdSpawn(string objName, Vector3 position, Quaternion rotation)
        {
            foreach (GameObject spawnPrefab in networkManager.spawnPrefabs)
            {
                if (spawnPrefab.name == objName)
                {
                    GameObject newObject = (GameObject)Instantiate(spawnPrefab, position, rotation);
                    NetworkServer.Spawn(newObject);
                    return;
                }

            }
        }
    }
}