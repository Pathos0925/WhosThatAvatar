using UnityEngine;
using System.Collections;

public class AddONSPAudioSourceComponent
{
    static float SpatializeGainFactor = 10.0f;
    static float InvSqrAttentuationMaxDistanceScale = 4.0f;

    public static void ApplyDefaultSpatializationToAudioSources()
    {
        AudioSource[] audioSources = Object.FindObjectsOfType<AudioSource>();
        foreach (AudioSource audioSrc in audioSources)
        {
            ApplyDefaultSpatializationToAudioSource(audioSrc);
        }
    }

    public static bool ApplyDefaultSpatializationToAudioSource(AudioSource audioSrc)
    {
        if (audioSrc == null)
            return false;

        // don't spatialize non-full 3D sounds
        if (!Mathf.Approximately(audioSrc.spatialBlend, 1.0f))
            return false;

        ONSPAudioSource onspSrc = audioSrc.GetComponent<ONSPAudioSource>();
        if (onspSrc != null)
        {
            // already has a spatialized audio source
            return false;
        }

        // add ONSPAudioSource
        onspSrc = audioSrc.gameObject.AddComponent<ONSPAudioSource>();

        // enable spatialization
        audioSrc.spatialize = true;
        onspSrc.EnableSpatialization = true;

        // constant gain
        onspSrc.Gain = SpatializeGainFactor;

        // set falloff
        onspSrc.UseInvSqr = audioSrc.rolloffMode != AudioRolloffMode.Linear;     // apply invSqr falloff to both logarithmic and custom falloffs

        onspSrc.Near = (audioSrc.rolloffMode != AudioRolloffMode.Custom) ? audioSrc.minDistance : CalculateCustomRolloffNearDistance(audioSrc);
        if (onspSrc.UseInvSqr)
            onspSrc.Far = audioSrc.maxDistance * InvSqrAttentuationMaxDistanceScale;
        else
            onspSrc.Far = audioSrc.maxDistance;

        return true;
    }

    static float CalculateCustomRolloffNearDistance(AudioSource audioSrc)
    {
        if (audioSrc.rolloffMode != AudioRolloffMode.Custom)
            return audioSrc.minDistance;

        AnimationCurve curve = audioSrc.GetCustomCurve(AudioSourceCurveType.CustomRolloff);
        if (curve == null)
            return 1.0f;

        Keyframe[] keys = curve.keys;
        if (keys.Length == 0)
            return 1.0f;

        Keyframe key = keys[0];
        for (int i = 1; i < keys.Length; i++)
        {
            if (keys[i].value < key.value - 0.001f)
                break;

            key = keys[i];
        }

        return key.time * audioSrc.maxDistance;
    }
}
