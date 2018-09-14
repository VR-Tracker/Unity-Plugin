using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using VRStandardAssets.Utils;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections.Generic;

namespace VRTracker.Pairing
{
    // The intro scene takes users through the basics
    // of interacting through VR in the other scenes.
    // This manager controls the steps of the intro
    // scene.
    public abstract class VRT_PairingUI : MonoBehaviour
    {
        /// <summary>
        /// Shows to the user we are awaiting for the automatic pairing.
        /// </summary>
        public abstract IEnumerator ShowWaitForAutomaticPairing();

        /// <summary>
        /// Hide the automatic pairing message.
        /// </summary>
        public abstract IEnumerator HideWaitForAutomaticPairing();

        /// <summary>
        /// Show a button to trigger to start the pairing sequence. Returns only when the button is pressed
        /// </summary>
        public abstract IEnumerator ShowStartPairingButton();

        /// <summary>
        /// Shows information when the pairing timed out, with a button to restart it.
        /// </summary>
        /// <param name="tagName">Tag name.</param>
        public abstract IEnumerator ShowFailToPairTag(VRTracker.Manager.VRT_Tag tag);

        /// <summary>
        /// Shows the information to tell the user to pair the Tag
        /// </summary>
        /// <param name="tagName">Tag name.</param>
        public abstract IEnumerator ShowPairTag(VRTracker.Manager.VRT_Tag tag);

        /// <summary>
        /// Shows the user we are loading the next scene.
        /// </summary>
        /// <returns>The loading next scene.</returns>
        public abstract IEnumerator ShowLoadingNextScene();
    }
}
