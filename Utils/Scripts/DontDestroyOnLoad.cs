using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This script prevents object from being destroyed when switching scene
/// </summary>
public class DontDestroyOnLoad : MonoBehaviour {

	private void Awake()
	{
        DontDestroyOnLoad(this);
	}
}
