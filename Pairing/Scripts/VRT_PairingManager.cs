using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using System.IO;   
using UnityEngine.SceneManagement;
using VRTracker.Manager;
using VRTracker.Utils;

namespace VRTracker.Pairing
{
	/// <summary>
	/// VRT pairing manager.
	/// Handles the pairing steps
	/// </summary>
	public class VRT_PairingManager : MonoBehaviour {


        [SerializeField]
        public SceneField pairingScene;
        public SceneField gameScene;

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
        private bool pairingSuccessfull = false;

        public static VRTracker.Pairing.VRT_PairingManager Instance = null;

        private void Awake()
        {

            //Check if instance already exists
            if (Instance == null)
            {
                Instance = this;
                availableTagMac = new List<string>();
            }
            
            //If instance already exists and it's not this:
            else if (Instance != this){
                //Then destroy this. This enforces our singleton pattern, meaning there can only ever be one instance of a GameManager.
                Destroy(gameObject);  

            }
        }

		// Use this for initialization
		void Start () {

            if (VRTracker.Manager.VRT_Manager.Instance.spectator)
            {
                gameObject.SetActive(false);
                if (gameScene.SceneName != "" && SceneManager.GetActiveScene().name != gameScene.SceneName.Split('/')[gameScene.SceneName.Split('/').Length-1])
                    SceneManager.LoadScene(gameScene);
                return;
            }
            else
                DontDestroyOnLoad(this);

            SceneManager.sceneLoaded += OnSceneLoaded;
            VRTracker.Manager.VRT_Manager.Instance.OnAvailableTag += AddAvailableTag;

            if (pairingUI == null)
                pairingUI = GetComponent<VRT_PairingUI>();
            if (pairingUI == null)
                pairingUI = GetComponent<VRT_PairingUIStandardAssets>();
            

            // If we couldn't find the Pairing UI on Start, it means we did not started from the Pairing scene, but the Main Scene
            // So we need to save the information
            if (pairingUI == null){
                if(gameScene == null)
                    gameScene = new SceneField(SceneManager.GetActiveScene());

                StartCoroutine(PairFromMainScene());
            }

            // Started from the pairing scene
            else {
                pairingScene = new SceneField(SceneManager.GetActiveScene());
                Debug.Log("Type Of Pairing Scene " + pairingScene.GetType().ToString());
                if (VRTracker.Manager.VRT_Manager.Instance.spectator){
                    if(gameScene.SceneName != "")
                        SceneManager.LoadScene(gameScene);
                    else // Load next scene in Build setting if no Game Scene was set manually in the Pairing Manager
                        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
                }
                // Not spectator
                else {
                    StartCoroutine(Pair());
                }
            }
		}

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log("OnSceneLoaded: " + scene.name);

            if (pairingUI == null && GameObject.FindObjectsOfType<VRT_PairingUI>().Length > 0)
                pairingUI = GameObject.FindObjectsOfType<VRT_PairingUI>()[0]; 

            if (pairingUI != null && !pairingSuccessfull){
                StartCoroutine(Pair());
            }
        }


        public IEnumerator PairFromMainScene(){
            // Do not try pairing if spectator
            if (VRTracker.Manager.VRT_Manager.Instance.spectator)
                yield return null;

            // Try automatic pairing
            if (autoPairing)
            {
                yield return StartCoroutine(AutomaticPairing());
                if (automaticPairingSuccessfull)
                {
                    Debug.Log("Auto pairing successful");
                    yield return null;
                }
                else
                {
                    Debug.Log("Auto pairing Failed");
                    if (pairingScene.SceneName != "")
                        SceneManager.LoadScene(pairingScene);
                    else
                    {
                        Debug.LogError("Pairing Scene is not set in VRT Pairing Manager, loading Scene 0");
                        SceneManager.LoadScene(0);
                    }
                }
            }
            else
            {
                if (pairingScene.SceneName != "")
                    SceneManager.LoadScene(pairingScene);
                else
                {
                    Debug.LogError("Pairing Scene is not set in VRT Pairing Manager, loading Scene 0");
                    SceneManager.LoadScene(0);
                }
            }
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
                pairingSuccessfull = true;
                if (gameScene.SceneName != "")
                    SceneManager.LoadScene(gameScene);
                else // Load next scene in Build setting if no Game Scene was set manually in the Pairing Manager
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            }

            // Try manual pairing
            else if(!automaticPairingSuccessfull){
                yield return StartCoroutine(ManualPairing());
                pairingSuccessfull = true;
                // Save pairing for next time
                SavePairingData();
                if (gameScene.SceneName != "")
                    SceneManager.LoadScene(gameScene);
                else // Load next scene in Build setting if no Game Scene was set manually in the Pairing Manager
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