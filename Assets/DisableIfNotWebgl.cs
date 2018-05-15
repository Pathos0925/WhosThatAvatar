using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableIfNotWebgl : MonoBehaviour {

	// Use this for initialization
	void Start ()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
                
#else
        this.gameObject.SetActive(false);
#endif
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
