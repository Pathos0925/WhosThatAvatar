using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System;

[CustomEditor(typeof(VRCSDK2.VRC_Station))]
public class VRCPlayerStationEditor : Editor 
{
	VRCSDK2.VRC_Station myTarget;

	void OnEnable()
	{
		if(myTarget == null)
			myTarget = (VRCSDK2.VRC_Station)target;
	}

	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();
	}
}