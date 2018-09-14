using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using System.IO;   
using UnityEngine.SceneManagement;
using VRTracker.Manager;
namespace VRTracker.Pairing
{
	/// <summary>
	/// VRT pairing manager.
	/// Handles the pairing steps
	/// </summary>
	public class VRT_PairingManager : MonoBehaviour {

        [Tooltip("Check to try to automatically assign the Tags")]
        [SerializeField]
        private bool autoPairing = true; 		//Skip the pairing when the tags are available
        [SerializeField]
		private string JsonFilePath = "Player_Data.json";
        //[SerializeField]
        //private float delayToPressButton = 10f; // Delay during which the User can press the red button on the Tag to assign it to one of its object in the game

        [SerializeField]
        private VRT_PairingUI pairingUI;

		private List<string> availableTagMac;	//List of available tag in the system

		private float currentTime;
        private bool automaticPairingSuccessfull = false;

		private void Awake()
	    {
			availableTagMac  = new List<string> ();
		}

		// Use this for initialization
		void Start () {
            if (pairingUI == null)
                pairingUI = GetComponent<VRT_PairingUI>();

            VRTracker.Manager.VRT_Manager.Instance.OnAvailableTag += AddAvailableTag;

			if (!VRTracker.Manager.VRT_Manager.Instance.spectator) {
				StartCoroutine(Pair());
			}
            else
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
		}

        public IEnumerator Pair()
        {
            // Try automatic pairing
            if (autoPairing)
            {
                StartCoroutine(pairingUI.ShowWaitForAutomaticPairing());
                yield return StartCoroutine(AutomaticPairing());
                StartCoroutine(pairingUI.HideWaitForAutomaticPairing());
            }

            if (automaticPairingSuccessfull)
            {
                // Load next scene
                StartCoroutine(pairingUI.ShowLoadingNextScene());
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            }

            // Try manual pairing
            else if(!automaticPairingSuccessfull){
                yield return StartCoroutine(ManualPairing());
                // Save pairing for next time
                SavePairingData();
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            }
        }

        /// <summary>
        /// Try to automatically pair the Tag using saved data in JSON file and available (connected) Tags
        /// </summary>
        public IEnumerator AutomaticPairing(){
            yield return new WaitForSeconds(1);
            if(!areTagsInJSON()){
                automaticPairingSuccessfull = false;
                yield break;
            }
            // 1. Try pairing from File
            //      Get Tag list in JSON file
            JSONNode filePairingData = LoadPairingData();
            //      Link Tag type to IDS in Json
            Dictionary<VRTracker.Manager.VRT_Tag, string> tagToAssign = new Dictionary<VRTracker.Manager.VRT_Tag, string>();
            foreach (VRTracker.Manager.VRT_Tag mTag in VRTracker.Manager.VRT_Manager.Instance.tags)
            {
                if (!mTag.IDisAssigned)
                {
                    for (short i = 0; i < filePairingData.Count; i++)
                    {
                        if (filePairingData.KeyAtIndex(i) == mTag.tagType.ToString())
                        {
                            tagToAssign.Add(mTag, filePairingData[filePairingData.KeyAtIndex(i)]);
                        }
                    }
                }
            }
            // Check if the Tags we want to assign are available
            bool tagsAvailableInGateway = false;
            for (int i = 0; i < 3 && !tagsAvailableInGateway; i++)
            {
                if (areTagsAvailable(tagToAssign))
                    tagsAvailableInGateway = true;
                else
                {
                    VRTracker.Manager.VRT_Manager.Instance.GetAvailableTags();
                    yield return new WaitForSeconds(1);
                }
            }

            // Set assignation in Tags
            if(tagsAvailableInGateway)
            {
                foreach (KeyValuePair<VRTracker.Manager.VRT_Tag, string> tagUID in tagToAssign)
                {
                    tagUID.Key.AssignTag(tagUID.Value);
                }
                automaticPairingSuccessfull = true;
            }
            else {
                Debug.LogWarning("Tag Association Error : Could not find all Tag on the Gateway, going to manual pair");
                automaticPairingSuccessfull = false;
            }
        }

        /// <summary>
        /// Try to pair manually the Tag using red button on the Tag
        /// </summary>
        public IEnumerator ManualPairing(){
            yield return StartCoroutine(pairingUI.ShowStartPairingButton());

            //Assignement step
            foreach (VRTracker.Manager.VRT_Tag mTag in VRTracker.Manager.VRT_Manager.Instance.tags)
            {
                bool associationFailed = true;
                while (associationFailed)
                {

                    // Edit shown title to the Prefab name
                    yield return StartCoroutine(pairingUI.ShowPairTag(mTag));

                    // Check if timed out and throw an error
                    if (!mTag.IDisAssigned)
                    {
                        associationFailed = true;
                        Debug.Log("Show Fail");
                        yield return StartCoroutine(pairingUI.ShowFailToPairTag(mTag));
                        Debug.Log("End SHow Fail");
                    }
                    else
                    {
                        associationFailed = false;
                        mTag.AssignTag(mTag.UID);
                    }
                }

            }
            yield break;
        }

        /// <summary>
        /// Check if the Tags to assign are all in the JSON file;
        /// </summary>
        /// <returns><c>true</c>, if tags in are in the json, <c>false</c> otherwise.</returns>
        private bool areTagsInJSON(){
            // 1. Try pairing from File
            //      Get Tag list in JSON file
            JSONNode filePairingData = LoadPairingData();
            if (filePairingData == null)
                return false;
            //      Link Tag type to IDS in Json
            Dictionary<VRTracker.Manager.VRT_Tag, string> tagToAssign = new Dictionary<VRTracker.Manager.VRT_Tag, string>();
            bool allTagAreInJSONList = true;
            foreach (VRTracker.Manager.VRT_Tag mTag in VRTracker.Manager.VRT_Manager.Instance.tags)
            {
                if (!mTag.IDisAssigned)
                {
                    bool tagFoundinJson = false;
                    for (short i = 0; i < filePairingData.Count; i++)
                    {
                        if (filePairingData.KeyAtIndex(i) == mTag.tagType.ToString())
                        {
                            //Debug.Log(filePairingData.KeyAtIndex(i) + " - " +filePairingData[filePairingData.KeyAtIndex(i)]);
                            tagToAssign.Add(mTag, filePairingData[filePairingData.KeyAtIndex(i)]);
                            tagFoundinJson = true;
                        }
                    }
                    if (!tagFoundinJson)
                        allTagAreInJSONList = false;
                }
            }
            return allTagAreInJSONList;
        }

        /// <summary>
        /// Checks if the Tags in parameter are all available in the Gateway (currently connected to the system) 
        /// </summary>
        private bool areTagsAvailable(Dictionary<VRTracker.Manager.VRT_Tag, string> tagToAssign){
            // Check if the Tags to assign are available in the Gateway
            bool allLinkFound = true;
            foreach (KeyValuePair<VRTracker.Manager.VRT_Tag, string> tagUID in tagToAssign)
            {
                bool tagLinkFound = false;
                foreach (string mac in availableTagMac)
                {
                    if (mac == tagUID.Value)
                        tagLinkFound = true;
                }
                if (!tagLinkFound)
                    allLinkFound = false;
            }
            return allLinkFound;
        }


        /// <summary>
        /// Load the pairing (Tag type - ID) from a JSON file
        /// </summary>
        public JSONNode LoadPairingData(){
            JSONNode playerAssociation = null;
			string filePath = Path.Combine(Application.persistentDataPath, JsonFilePath);
			//Debug.Log ("Opening " + filePath);

			if(File.Exists(filePath))
            {
				// Read the json from the file into a string
				string jsonDataString = File.ReadAllText(filePath);
                playerAssociation = JSON.Parse(jsonDataString);
			}
			else
			{
				Debug.LogWarning("Cannot load json file!");
			}
            return playerAssociation;
		}

        /// <summary>
        /// Saves the pairing (Tag type - ID) for each Tag to a JSON file
        /// </summary>
        public void SavePairingData(){
            JSONNode playerAssociation = null;
            bool canSave = false;
            foreach(VRTracker.Manager.VRT_Tag mTag in VRTracker.Manager.VRT_Manager.Instance.tags)
			{
                if(mTag.UID != "" && mTag.UID != "Enter Your Tag UID")
				{
					//Store every tag association
					if (playerAssociation == null)
					{
						playerAssociation = new JSONObject();
					}
                    playerAssociation[mTag.tagType.ToString()] = mTag.UID;
					canSave = true;
				}
			}
            string filePath = Path.Combine(Application.persistentDataPath, JsonFilePath);
            //Debug.Log("Save path " + filePath);
            if(playerAssociation != null)
            {
                if(canSave)
                {
                    string content = playerAssociation.ToString();
                    System.IO.File.WriteAllText(filePath, content);
                }
            }
            else
            {
                Debug.Log("No Association to save");
            }
		}

        /// <summary>
        /// Update the association data from the file
        /// </summary>
        public void AddAvailableTag(string uid)
        {
            availableTagMac.Add(uid);
        }
	}
}