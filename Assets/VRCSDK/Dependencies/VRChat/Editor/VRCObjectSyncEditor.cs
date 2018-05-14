using UnityEngine;
using System.Collections;
using UnityEditor;
using System;

[CustomEditor(typeof(VRCSDK2.VRC_ObjectSync))]
public class VRCObjectSyncEditor : Editor
{
    VRCSDK2.VRC_ObjectSync sync;

    void OnEnable()
    {
        if (sync == null)
            sync = (VRCSDK2.VRC_ObjectSync)target;
    }

    public override void OnInspectorGUI()
    {
        sync.SynchronizePhysics = EditorGUILayout.Toggle("Synchronize Physics",sync.SynchronizePhysics);
        sync.AllowCollisionTransfer = EditorGUILayout.Toggle("Allow Collision Transfer", sync.AllowCollisionTransfer);

#if PLAYMAKER
        if ( sync.GetComponent<PlayMakerFSM>() != null )
        {
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Playmaker");
            sync.PlaymakerSyncState = EditorGUILayout.Toggle("Synchronize Playmaker State?", sync.PlaymakerSyncState);
            sync.PlaymakerSyncVariables = EditorGUILayout.Toggle("Synchronize Playmaker Variables?", sync.PlaymakerSyncVariables);
        }
#endif
    }
}
