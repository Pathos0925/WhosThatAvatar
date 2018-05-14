using UnityEngine;
using System.Collections;
using UnityEditor;

[InitializeOnLoad]
public class AutoAddONSPAudioSourceComponents
{

    public static bool Enabled = true;

    static AutoAddONSPAudioSourceComponents()
    {
        EditorApplication.hierarchyWindowChanged += OnHierarchyWindowChanged;
		EditorApplication.projectWindowChanged += OnProjectWindowChanged;
		RegisterCallbacks();
    }

    static void OnHierarchyWindowChanged()
    {
        if (!Enabled)
        {
            EditorApplication.hierarchyWindowChanged -= OnHierarchyWindowChanged;
            return;
        }

        TryAddONSPToAllAudioSources(true, false);
    }

	static void OnProjectWindowChanged()
	{
		RegisterCallbacks();
	}

	static void RegisterCallbacks()
	{
		VRC_SdkControlPanel._EnableSpatialization = VRCSDKControlPanel_EnableSpatialization;
	}

	// callback from VrcSdkControlPanel in dll
	public static void VRCSDKControlPanel_EnableSpatialization()
	{
		Debug.Log("Enabling spatialization on AudioSources...");
		TryAddONSPToAllAudioSources(false, true);
	}

    public static void TryAddONSPToAllAudioSources(bool newAudioSourcesOnly, bool includeInactive)
    {
        AudioSource[] allAudioSources = includeInactive ? Resources.FindObjectsOfTypeAll<AudioSource>() : Object.FindObjectsOfType<AudioSource>();
        foreach (AudioSource src in allAudioSources)
        {
            if (src == null || src.gameObject == null || !src.enabled || src.gameObject.scene != UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene())
            {
                continue;
            }

            if (newAudioSourcesOnly)
            {
                if (!IsNewAudioSource(src))
                    continue;

                src.spatialBlend = 1.0f;  // set 3D mode

                UnityEngine.Audio.AudioMixerGroup mixer = AssetDatabase.LoadAssetAtPath<UnityEngine.Audio.AudioMixerGroup>("Assets/VRCSDK/Dependencies/OSPNative/scenes/mixers/SpatializerMixer.mixer");
                if (mixer != null)
                {
                    src.outputAudioMixerGroup = mixer;
                }
            }

            if (AddONSPAudioSourceComponent.ApplyDefaultSpatializationToAudioSource(src))
            {
                Debug.Log("Automatically added ONSPAudioSource component and enabled spatialized audio to " + GetGameObjectPath(src.gameObject) + "!");
            }
        }
    }

    static bool IsNewAudioSource(AudioSource src)
    {
        if (src.clip != null)
            return false;
        if (src.outputAudioMixerGroup != null)
            return false;

        if (src.mute || src.bypassEffects || src.bypassReverbZones || !src.playOnAwake || src.loop)
            return false;

        if (src.priority != 128 ||
            !Mathf.Approximately(src.volume, 1.0f) ||
            !Mathf.Approximately(src.pitch, 1.0f) ||
            !Mathf.Approximately(src.panStereo, 0.0f) ||
            !Mathf.Approximately(src.spatialBlend, 0.0f) ||
            !Mathf.Approximately(src.reverbZoneMix, 1.0f))
        {
            return false;
        }

        if (!Mathf.Approximately(src.dopplerLevel, 1.0f) ||
            !Mathf.Approximately(src.spread, 0.0f) ||
            src.rolloffMode != AudioRolloffMode.Logarithmic ||
            !Mathf.Approximately(src.minDistance, 1.0f) ||
            !Mathf.Approximately(src.maxDistance, 500.0f))
        {
            return false;
        }

        return true;
    }

    static string GetGameObjectPath(GameObject obj)
    {
        string path = "/" + obj.name;
        while (obj.transform.parent != null)
        {
            obj = obj.transform.parent.gameObject;
            path = "/" + obj.name + path;
        }
        return path;
    }

}
