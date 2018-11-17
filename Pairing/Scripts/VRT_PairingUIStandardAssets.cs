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
    public class VRT_PairingUIStandardAssets : VRT_PairingUI
    {
        [SerializeField] private Reticle m_Reticle;                         // The scene only uses SelectionSliders so the reticle should be shown.
        [SerializeField] private SelectionRadial m_Radial;                  // Likewise, since only SelectionSliders are used, the radial should be hidden.

        [SerializeField] private UIFader m_WaitForAutomaticPairingFader;

        [SerializeField] private UIFader m_PairingStartFader;            	// Afterwards users are asked to confirm how to use sliders in this UI.
        [SerializeField] private SelectionSlider m_PairingStartSlider;    	// They demonstrate this using this slider.

        [SerializeField] private UIFader m_PairingTagFader;                // The final instructions are controlled using this fader.

        [SerializeField] private UIFader m_PairingFailedFader;
        [SerializeField] private SelectionSlider m_PairingFailedSlider;

        [SerializeField] private UIFader m_LoadingFader;

        private void Start()
        {
            if(m_Reticle != null)
                m_Reticle.Show();
        }


        /// <summary>
        /// Shows the user we are awaiting for then automatic assignation.
        /// </summary>
        public override IEnumerator ShowWaitForAutomaticPairing()
        {
            yield return StartCoroutine(m_WaitForAutomaticPairingFader.InteruptAndFadeIn());
        }


        /// <summary>
        /// Hide the user we are awaiting for then automatic assignation.
        /// </summary>
        public override IEnumerator HideWaitForAutomaticPairing()
        {
            yield return StartCoroutine(m_WaitForAutomaticPairingFader.InteruptAndFadeOut());
        }

        /// <summary>
        /// Show a button to trigger to start the pairing sequence
        /// </summary>
        public override IEnumerator ShowStartPairingButton()
        {
            // Hide automatic pairing text
            yield return StartCoroutine(m_WaitForAutomaticPairingFader.InteruptAndFadeOut());
            // Show button to start pairing
            yield return StartCoroutine(ShowMenu(m_PairingStartFader,m_PairingStartSlider));
        }

        /// <summary>
        /// Shows information when the pairing timed out, with a button to restart it.
        /// </summary>
        /// <param name="tag">Tag.</param>
        public override IEnumerator ShowFailToPairTag(VRTracker.Manager.VRT_Tag mTag)
        {
            yield return StartCoroutine(ShowMenu(m_PairingFailedFader, m_PairingFailedSlider));
        }

        /// <summary>
        /// Shows the information to tell the user to pair the Tag
        /// </summary>
        /// <param name="tag">Tag.</param>
        public override IEnumerator ShowPairTag(VRTracker.Manager.VRT_Tag mTag)
        {
            m_PairingTagFader.transform.Find("PairingTagInstructions/Title").GetComponentInChildren<Text>().text = "Assign " + mTag.tagType.ToString();
            yield return StartCoroutine(ShowMenu(m_PairingTagFader, mTag, 10.0f));
        }

		/// <summary>
		/// Shows the user we are loading the next scene.
		/// </summary>
		/// <returns>The loading next scene.</returns>
        public override IEnumerator ShowLoadingNextScene(){
            yield return StartCoroutine(m_LoadingFader.InteruptAndFadeIn());
        }

		/// <summary>
		/// Shows the bar to fill to enable the pairing
		/// </summary>
		/// <returns>The menu.</returns>
		/// <param name="fader">Fader.</param>
		/// <param name="slider">Slider.</param>
        private IEnumerator ShowMenu(UIFader fader, SelectionSlider slider)
        {
            yield return StartCoroutine(fader.InteruptAndFadeIn());
            yield return StartCoroutine(slider.WaitForBarToFill());
            yield return StartCoroutine(fader.InteruptAndFadeOut());
        }

		/// <summary>
		/// Show the message to tell which tag is going to be paired
		/// </summary>
		/// <returns>The menu.</returns>
		/// <param name="fader">Fader.</param>
		/// <param name="tag">Tag.</param>
		/// <param name="delayToPressButton">Delay to press button.</param>
        private IEnumerator ShowMenu(UIFader fader, VRTracker.Manager.VRT_Tag tag, float delayToPressButton)
        {
            yield return StartCoroutine(fader.InteruptAndFadeIn());
            yield return StartCoroutine(tag.WaitForAssignation(delayToPressButton));
            if (tag.IDisAssigned)
                transform.GetComponent<AudioSource>().Play();
            yield return StartCoroutine(fader.InteruptAndFadeOut());
        }
    }
}
