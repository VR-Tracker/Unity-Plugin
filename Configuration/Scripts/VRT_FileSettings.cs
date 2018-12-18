using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using SimpleJSON;

/// <summary>
/// VTR file conifguration.
/// This script reads a configuration JSON file 
/// in this configuration file we can set if the Plugin and Game should be used 
/// as an host / server, set the IP etc
/// This script can be extended from for specific configuration for each Game
/// </summary>

namespace VRTracker.Configuration
{
    public class VRT_FileSettings : MonoBehaviour
    {
        public static VRTracker.Configuration.VRT_FileSettings Instance = null;
        public string path = "/VRTracker/configuration.json";
        private JSONNode jsonObject;

        [HideInInspector]
        public bool settingsFound = false;

        [HideInInspector]
        public bool server_set = false;
        [HideInInspector]
        public bool server = false;

        [HideInInspector]
        public bool host_set = false;
        [HideInInspector]
        public bool host = false;

        [HideInInspector]
        public bool client_set = false;
        [HideInInspector]
        public bool client = false;

        [HideInInspector]
        public bool ip_set = false;
        [HideInInspector]
        public string ip;

        [HideInInspector]
        public bool port_set = false;
        [HideInInspector]
        public int port;


        private void Awake()
        {
            //Check if instance already exists
            if (Instance == null)
            {
                Instance = this;
            }

            // Destroy Instance if singleton already created
            else if (Instance != this)
                Destroy(gameObject);

            if (FileToJSON(path))
            {
                settingsFound = true;
                ParseSettings(jsonObject);
            }
            else
                settingsFound = false;
        }

        // Use this for initialization
        void Start()
        {
            
        }

        private void ParseSettings(JSONNode json)
        {
            if (json["server"] != null){
                server_set = true;
                server = json["server"].AsBool;
            }

            if (json["host"] != null)
            {
                host_set = true;
                host = json["host"].AsBool;
            }

            if (json["client"] != null)
            {
                client_set = true;
                client = json["client"].AsBool;
            }

            if (json["port"] != null)
            {
                port_set = true;
                port = json["port"].AsInt;
            }

            if (json["ip"] != null)
            {
                ip_set = true;
                ip = json["ip"];
            }
        }


        /// <summary>
        /// Read a file into a JSON Object
        /// </summary>
        /// <returns>The to json.</returns>
        /// <param name="filePath">File path.</param>
        private bool FileToJSON(string filePath)
        {
            string finalPath;
#if UNITY_EDITOR || UNITY_ANDROID
            finalPath = Directory.GetCurrentDirectory() + filePath;
#elif UNITY_STANDALONE
        finalPath = Directory.GetCurrentDirectory() + "../../.." + filePath;
#endif

            if (!File.Exists(finalPath))
            {
                Debug.LogWarning("Could not find " + finalPath + " file");
                return false;
            }
            else
            {
                string json_string = File.ReadAllText(finalPath);
                jsonObject = JSON.Parse(json_string);
                return true;
            }
        }


    }
}