using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// VRT tag simulator simulates a VRT_Tag script in the simulator
/// </summary>
public class VRT_TagSimulator : VRTracker.Manager.VRT_Tag {

	// Use this for initialization
	protected override void Start () {
		VRTracker.Manager.VRT_Manager.Instance.AddTag(this);

        VRTracker.Player.VRT_FollowTag[] tagsFollow = FindObjectsOfType<VRTracker.Player.VRT_FollowTag>();
        foreach (VRTracker.Player.VRT_FollowTag tagFollow in tagsFollow) {
			if (tagFollow.tagTypeToFollow == tagType) {
				tagFollow.simulatorTag = true;
				tagFollow.tagToFollow = this;
			}
		}
	}
	
	// Update is called once per frame
    protected override void Update () {

    }

}
