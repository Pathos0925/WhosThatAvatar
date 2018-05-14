#define ENV_SET_INCLUDED_SHADERS

using UnityEngine;
using UnityEditor;
using System.Collections;
using System;
using System.Reflection;
using System.Linq;

/// <summary>
/// Setup up SDK env on editor launch
/// </summary>
[InitializeOnLoad]
public class EnvConfig
{
    static BuildTarget[] relevantBuildTargets = new BuildTarget[] {
        BuildTarget.Android, BuildTarget.iOS,
        BuildTarget.StandaloneLinux, BuildTarget.StandaloneLinux64, BuildTarget.StandaloneLinuxUniversal,
        BuildTarget.StandaloneWindows, BuildTarget.StandaloneWindows64,
        BuildTarget.StandaloneOSXIntel, BuildTarget.StandaloneOSXIntel64, BuildTarget.StandaloneOSXUniversal
    };

#if !VRC_CLIENT
    static BuildTarget[] allowedBuildtargets = new BuildTarget[]
    {
        BuildTarget.StandaloneWindows64,
        BuildTarget.Android
    };
#endif

    static System.Collections.Generic.Dictionary<BuildTarget, UnityEngine.Rendering.GraphicsDeviceType[]> allowedGraphicsAPIs = new System.Collections.Generic.Dictionary<BuildTarget, UnityEngine.Rendering.GraphicsDeviceType[]>()
    {
        { BuildTarget.Android, null },
        { BuildTarget.iOS, null },
        { BuildTarget.StandaloneLinux, null },
        { BuildTarget.StandaloneLinux64, null },
        { BuildTarget.StandaloneLinuxUniversal, null },
        { BuildTarget.StandaloneWindows, new UnityEngine.Rendering.GraphicsDeviceType[] { UnityEngine.Rendering.GraphicsDeviceType.Direct3D11 } },
        { BuildTarget.StandaloneWindows64, new UnityEngine.Rendering.GraphicsDeviceType[] { UnityEngine.Rendering.GraphicsDeviceType.Direct3D11 } },
        { BuildTarget.StandaloneOSXIntel, null },
        { BuildTarget.StandaloneOSXIntel64, null },
        { BuildTarget.StandaloneOSXUniversal, null }
    };

    static string[] ensureTheseShadersAreAvailable = new string[]
    {
#if VRC_CLIENT
        "Hidden/CubeBlend",
        "Hidden/CubeBlur",
        "Hidden/CubeCopy",
        "Hidden/VideoDecode",
        "Legacy Shaders/Bumped Diffuse",
        "Legacy Shaders/Bumped Specular",
        "Legacy Shaders/Decal",
        "Legacy Shaders/Diffuse Detail",
        "Legacy Shaders/Diffuse Fast",
        "Legacy Shaders/Diffuse",
        "Legacy Shaders/Diffuse",
        "Legacy Shaders/Lightmapped/Diffuse",
        "Legacy Shaders/Lightmapped/Specular",
        "Legacy Shaders/Lightmapped/VertexLit",
        "Legacy Shaders/Parallax Diffuse",
        "Legacy Shaders/Parallax Specular",
        "Legacy Shaders/Reflective/Bumped Diffuse",
        "Legacy Shaders/Reflective/Bumped Specular",
        "Legacy Shaders/Reflective/Bumped Unlit",
        "Legacy Shaders/Reflective/Bumped VertexLit",
        "Legacy Shaders/Reflective/Diffuse",
        "Legacy Shaders/Reflective/Parallax Diffuse",
        "Legacy Shaders/Reflective/Parallax Specular",
        "Legacy Shaders/Reflective/Specular",
        "Legacy Shaders/Reflective/VertexLit",
        "Legacy Shaders/Self-Illumin/Bumped Diffuse",
        "Legacy Shaders/Self-Illumin/Bumped Specular",
        "Legacy Shaders/Self-Illumin/Diffuse",
        "Legacy Shaders/Self-Illumin/Parallax Diffuse",
        "Legacy Shaders/Self-Illumin/Parallax Specular",
        "Legacy Shaders/Self-Illumin/Specular",
        "Legacy Shaders/Self-Illumin/VertexLit",
        "Legacy Shaders/Specular",
        "Legacy Shaders/Transparent/Bumped Diffuse",
        "Legacy Shaders/Transparent/Bumped Specular",
        "Legacy Shaders/Transparent/Cutout/Bumped Diffuse",
        "Legacy Shaders/Transparent/Cutout/Bumped Specular",
        "Legacy Shaders/Transparent/Cutout/Diffuse",
        "Legacy Shaders/Transparent/Cutout/Soft Edge Unlit",
        "Legacy Shaders/Transparent/Cutout/Specular",
        "Legacy Shaders/Transparent/Cutout/VertexLit",
        "Legacy Shaders/Transparent/Diffuse",
        "Legacy Shaders/Transparent/Parallax Diffuse",
        "Legacy Shaders/Transparent/Parallax Specular",
        "Legacy Shaders/Transparent/Specular",
        "Legacy Shaders/Transparent/VertexLit",
        "Legacy Shaders/VertexLit",
        "Mobile/Particles/Additive",
        "Mobile/Particles/Alpha Blended",
        "Mobile/Particles/Multiply",
        "Mobile/Particles/VertexLit Blended",
        "Mobile/Skybox",
        "Nature/Terrain/Diffuse",
        "Nature/Terrain/Specular",
        "Nature/Terrain/Standard",
        "Particles/Additive (Soft)",
        "Particles/Additive",
        "Particles/Alpha Blended Premultiply",
        "Particles/Alpha Blended",
        "Particles/Anim Alpha Blended",
        "Particles/Multiply (Double)",
        "Particles/Multiply",
        "Particles/VertexLit Blended",
        "Particles/~Additive-Multiply",
        "Skybox/Cubemap",
        "Skybox/Procedural",
        "Skybox/6 Sided",
        "Sprites/Default",
        "Sprites/Diffuse",
        "UI/Default",
        "UI/Unlit/WebPanelTransparent",
#endif
    };

    private static bool _requestConfigureSettings = true;
    
    static EnvConfig()
    {
        EditorApplication.update += EditorUpdate;
    }

    static void EditorUpdate()
    {
        if (_requestConfigureSettings)
        {
            if (ConfigureSettings())
            {
                _requestConfigureSettings = false; 
            }
        }
    }

    public static void RequestConfigureSettings()
    {
        _requestConfigureSettings = true;
    }

    [UnityEditor.Callbacks.DidReloadScripts(int.MaxValue)]
    static void DidReloadScripts()
    {
        RequestConfigureSettings();
    }

    public static bool ConfigureSettings()
    {
        CustomDLLMaker.CreateDirectories();
        if (CheckForFirstInit())
            VRC.AssetExporter.CleanupTmpFiles();

        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isUpdating)
            return false;

        ConfigurePlayerSettings();

        if (!VRC.Core.RemoteConfig.IsInitialized())
        {
            VRC.Core.API.SetOnlineMode(true, "vrchat");
            VRC.Core.RemoteConfig.Init();
        }

        return true;
    }

    [MenuItem("VRChat SDK/Force Configure Player Settings")]
    public static void ConfigurePlayerSettings()
	{
		Debug.Log("Setting required PlayerSettings...");

        SetBuildTarget();

		// Needed for Microsoft.CSharp namespace in DLLMaker
		// Doesn't seem to work though
		if(PlayerSettings.GetApiCompatibilityLevel(EditorUserBuildSettings.selectedBuildTargetGroup) != ApiCompatibilityLevel.NET_2_0)
			PlayerSettings.SetApiCompatibilityLevel(EditorUserBuildSettings.selectedBuildTargetGroup, ApiCompatibilityLevel.NET_2_0);

		if(!PlayerSettings.runInBackground)
			PlayerSettings.runInBackground = true;

        SetDefaultGraphicsAPIs();
        SetGraphicsSettings();
        SetPlayerSettings();
        
#if VRC_CLIENT
        RefreshClientVRSDKs();
#else
		// SDK

		// default to steam runtime in sdk (shouldn't matter)
		SetVRSDKs(new string[] { "None", "OpenVR", "Oculus" });

        VRC.Core.AnalyticsSDK.Initialize(VRC.Core.SDKClientUtilities.GetSDKVersionDate());
#endif
    }

    static void EnableBatching(bool enable)
	{
		PlayerSettings[] playerSettings = Resources.FindObjectsOfTypeAll<PlayerSettings>();
		if (playerSettings == null)
			return;

		SerializedObject playerSettingsSerializedObject = new SerializedObject(playerSettings);
		SerializedProperty batchingSettings = playerSettingsSerializedObject.FindProperty("m_BuildTargetBatching");
		if (batchingSettings == null)
			return;

		for (int i = 0;i < batchingSettings.arraySize;i++)
		{
			SerializedProperty batchingArrayValue = batchingSettings.GetArrayElementAtIndex(i);
			if (batchingArrayValue == null)
				continue;
			
			IEnumerator batchingEnumerator = batchingArrayValue.GetEnumerator();
			if (batchingEnumerator == null)
				continue;

			while(batchingEnumerator.MoveNext())
			{
				SerializedProperty property = (SerializedProperty)batchingEnumerator.Current;

				if (property != null && property.name == "m_BuildTarget")
				{
					// only change setting on "Standalone" entry
					if (property.stringValue != "Standalone")
						break;
				}


				if (property != null && property.name == "m_StaticBatching")
				{
					property.boolValue = enable;
				}

				if (property != null && property.name == "m_DynamicBatching")
				{
					property.boolValue = enable;
				}
			}
		}

		playerSettingsSerializedObject.ApplyModifiedProperties();
	}

	public static void SetVRSDKs(string[] sdkNames)
	{
		Debug.Log("Setting virtual reality SDKs in PlayerSettings: ");
		if (sdkNames != null)
		{
			foreach (string s in sdkNames)
				Debug.Log("- " + s);
		}

		PlayerSettings[] playerSettings = Resources.FindObjectsOfTypeAll<PlayerSettings>();
		if (playerSettings == null)
			return;

		SerializedObject playerSettingsSerializedObject = new SerializedObject(playerSettings);
		SerializedProperty settingsGroup = playerSettingsSerializedObject.FindProperty("m_BuildTargetVRSettings");
		if (settingsGroup == null)
			return;

		for (int i = 0;i < settingsGroup.arraySize;i++)
		{
			SerializedProperty settingVal = settingsGroup.GetArrayElementAtIndex(i);
			if (settingVal == null)
				continue;

			IEnumerator enumerator = settingVal.GetEnumerator();
			if (enumerator == null)
				continue;

			while(enumerator.MoveNext())
			{
				SerializedProperty property = (SerializedProperty)enumerator.Current;

				if (property != null && property.name == "m_BuildTarget")
				{
					// only change setting on "Standalone" entry
					if (property.stringValue != "Standalone")
						break;
				}

				if (property != null && property.name == "m_Devices")
				{
					property.ClearArray();
					property.arraySize = (sdkNames != null) ? sdkNames.Length : 0;
					for (int j = 0; j < property.arraySize; j++)
					{
						property.GetArrayElementAtIndex(j).stringValue = sdkNames[j];
					}
				}
			}
		}

		playerSettingsSerializedObject.ApplyModifiedProperties();
	}

	static void RefreshClientVRSDKs()
	{
#if VRC_CLIENT

#if VRC_VR_STEAM
		SetVRSDKs(new string[] { "None", "OpenVR", "Oculus" });
#else
		SetVRSDKs(new string[] { "None", "Oculus", "OpenVR" });
#endif

#endif // VRC_CLIENT
    }

    public static bool CheckForFirstInit()
	{
		bool firstLaunch = UnityEditor.SessionState.GetBool("EnvConfigFirstLaunch", true);  
		if (firstLaunch)
    		UnityEditor.SessionState.SetBool("EnvConfigFirstLaunch", false);

		return firstLaunch;
	}

    static void SetDefaultGraphicsAPIs()
    {
        Debug.Log("Setting Graphics APIs");
        foreach (BuildTarget target in relevantBuildTargets)
        {
            var apis = allowedGraphicsAPIs[target];
            if (apis == null)
                SetGraphicsAPIs(target, true);
            else
                SetGraphicsAPIs(target, false, apis);
        }
    }

    static void SetGraphicsAPIs(BuildTarget platform, bool auto, UnityEngine.Rendering.GraphicsDeviceType[] allowedTypes = null)
    {
        try
        {
            if (auto != PlayerSettings.GetUseDefaultGraphicsAPIs(platform))
                PlayerSettings.SetUseDefaultGraphicsAPIs(platform, auto);
        }
        catch { }

        try
        {
            UnityEngine.Rendering.GraphicsDeviceType[] graphicsAPIs = PlayerSettings.GetGraphicsAPIs(platform);
            if (((allowedTypes == null || allowedTypes.Length == 0) && (graphicsAPIs != null || graphicsAPIs.Length != 0))
                || !allowedTypes.SequenceEqual(graphicsAPIs))
                PlayerSettings.SetGraphicsAPIs(platform, allowedTypes);
        }
        catch { }
    }

    static void SetGraphicsSettings()
    {
        Debug.Log("Setting Graphics Settings");

        const string GraphicsSettingsAssetPath = "ProjectSettings/GraphicsSettings.asset";
        SerializedObject graphicsManager = new SerializedObject(UnityEditor.AssetDatabase.LoadAllAssetsAtPath(GraphicsSettingsAssetPath)[0]);

        SerializedProperty deferred = graphicsManager.FindProperty("m_Deferred.m_Mode");
        deferred.enumValueIndex = 1;

        SerializedProperty deferredReflections = graphicsManager.FindProperty("m_DeferredReflections.m_Mode");
        deferredReflections.enumValueIndex = 1;

        SerializedProperty screenSpaceShadows = graphicsManager.FindProperty("m_ScreenSpaceShadows.m_Mode");
        screenSpaceShadows.enumValueIndex = 1;

        SerializedProperty legacyDeferred = graphicsManager.FindProperty("m_LegacyDeferred.m_Mode");
        legacyDeferred.enumValueIndex = 1;

        SerializedProperty depthNormals = graphicsManager.FindProperty("m_DepthNormals.m_Mode");
        depthNormals.enumValueIndex = 1;

        SerializedProperty motionVectors = graphicsManager.FindProperty("m_MotionVectors.m_Mode");
        motionVectors.enumValueIndex = 1;

        SerializedProperty lightHalo = graphicsManager.FindProperty("m_LightHalo.m_Mode");
        lightHalo.enumValueIndex = 1;

        SerializedProperty lensFlare = graphicsManager.FindProperty("m_LensFlare.m_Mode");
        lensFlare.enumValueIndex = 1;

#if ENV_SET_INCLUDED_SHADERS
        SerializedProperty alwaysIncluded = graphicsManager.FindProperty("m_AlwaysIncludedShaders");

#if ENV_SEARCH_FOR_SHADERS
        Resources.LoadAll("", typeof(Shader));
        System.Collections.Generic.List<Shader> foundShaders = Resources.FindObjectsOfTypeAll<Shader>()
            .Where(s => { string name = s.name.ToLower(); return 0 == (s.hideFlags & HideFlags.DontSave); })
            .GroupBy(s => s.name)
            .Select(g => g.First())
            .ToList();
#else
        System.Collections.Generic.List<Shader> foundShaders = new System.Collections.Generic.List<Shader>();
#endif

        for (int shaderIdx = 0; shaderIdx < ensureTheseShadersAreAvailable.Length; ++shaderIdx)
        {
            if (foundShaders.Any(s => s.name == ensureTheseShadersAreAvailable[shaderIdx]))
                continue;
            Shader namedShader = Shader.Find(ensureTheseShadersAreAvailable[shaderIdx]);
            if (namedShader != null)
                foundShaders.Add(namedShader);
        }

        foundShaders.Sort((s1, s2) => s1.name.CompareTo(s2.name));

        alwaysIncluded.arraySize = foundShaders.Count;
        for (int shaderIdx = 0; shaderIdx < foundShaders.Count; ++shaderIdx)
            alwaysIncluded.GetArrayElementAtIndex(shaderIdx).objectReferenceValue = foundShaders[shaderIdx];
#endif

        SerializedProperty preloaded = graphicsManager.FindProperty("m_PreloadedShaders");
        preloaded.ClearArray();
        preloaded.arraySize = 0;

        SerializedProperty spritesDefaultMaterial = graphicsManager.FindProperty("m_SpritesDefaultMaterial");
        spritesDefaultMaterial.objectReferenceValue = Shader.Find("Sprites/Default");

        SerializedProperty renderPipeline = graphicsManager.FindProperty("m_CustomRenderPipeline");
        renderPipeline.objectReferenceValue = null;

        SerializedProperty transparencySortMode = graphicsManager.FindProperty("m_TransparencySortMode");
        transparencySortMode.enumValueIndex = 0;

        SerializedProperty transparencySortAxis = graphicsManager.FindProperty("m_TransparencySortAxis");
        transparencySortAxis.vector3Value = Vector3.forward;
        
        SerializedProperty defaultRenderingPath = graphicsManager.FindProperty("m_DefaultRenderingPath");
        defaultRenderingPath.intValue = 1;

        SerializedProperty defaultMobileRenderingPath = graphicsManager.FindProperty("m_DefaultMobileRenderingPath");
        defaultMobileRenderingPath.intValue = 1;

        SerializedProperty tierSettings = graphicsManager.FindProperty("m_TierSettings");
        tierSettings.ClearArray();
        tierSettings.arraySize = 0;

#if ENV_SET_LIGHTMAP
        SerializedProperty lightmapStripping = graphicsManager.FindProperty("m_LightmapStripping");
        lightmapStripping.enumValueIndex = 1;

        SerializedProperty instancingStripping = graphicsManager.FindProperty("m_InstancingStripping");
        instancingStripping.enumValueIndex = 2;

        SerializedProperty lightmapKeepPlain = graphicsManager.FindProperty("m_LightmapKeepPlain");
        lightmapKeepPlain.boolValue = true;

        SerializedProperty lightmapKeepDirCombined = graphicsManager.FindProperty("m_LightmapKeepDirCombined");
        lightmapKeepDirCombined.boolValue = true;

        SerializedProperty lightmapKeepDynamicPlain = graphicsManager.FindProperty("m_LightmapKeepDynamicPlain");
        lightmapKeepDynamicPlain.boolValue = true;

        SerializedProperty lightmapKeepDynamicDirCombined = graphicsManager.FindProperty("m_LightmapKeepDynamicDirCombined");
        lightmapKeepDynamicDirCombined.boolValue = true;

        SerializedProperty lightmapKeepShadowMask = graphicsManager.FindProperty("m_LightmapKeepShadowMask");
        lightmapKeepShadowMask.boolValue = true;

        SerializedProperty lightmapKeepSubtractive = graphicsManager.FindProperty("m_LightmapKeepSubtractive");
        lightmapKeepSubtractive.boolValue = true;
#endif

#if ENV_SET_FOG
        SerializedProperty fogStripping = graphicsManager.FindProperty("m_FogStripping");
        fogStripping.enumValueIndex = 1;

        SerializedProperty fogKeepLinear = graphicsManager.FindProperty("m_FogKeepLinear");
        fogKeepLinear.boolValue = true;

        SerializedProperty fogKeepExp = graphicsManager.FindProperty("m_FogKeepExp");
        fogKeepExp.boolValue = true;

        SerializedProperty fogKeepExp2 = graphicsManager.FindProperty("m_FogKeepExp2");
        fogKeepExp2.boolValue = true;
#endif

        SerializedProperty albedoSwatchInfos = graphicsManager.FindProperty("m_AlbedoSwatchInfos");
        albedoSwatchInfos.ClearArray();
        albedoSwatchInfos.arraySize = 0;

        SerializedProperty lightsUseLinearIntensity = graphicsManager.FindProperty("m_LightsUseLinearIntensity");
        lightsUseLinearIntensity.boolValue = true;

        SerializedProperty lightsUseColorTemperature = graphicsManager.FindProperty("m_LightsUseColorTemperature");
        lightsUseColorTemperature.boolValue = true;

        graphicsManager.ApplyModifiedProperties();
    }

    static void SetPlayerSettings()
    {
        // asset bundles MUST be built with settings that are compatible with VRC client
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.virtualRealitySupported = true;
        PlayerSettings.stereoRenderingPath = StereoRenderingPath.SinglePass;

        EnableBatching(true);
    }

    static void SetBuildTarget()
    {
#if !VRC_CLIENT
        Debug.Log("Setting build target");

        BuildTarget target = UnityEditor.EditorUserBuildSettings.activeBuildTarget;

        if (!allowedBuildtargets.Contains(target))
        {
            Debug.LogError("Target not supported, switching to one that is.");
            target = allowedBuildtargets[0];
#pragma warning disable CS0618 // Type or member is obsolete
            EditorUserBuildSettings.SwitchActiveBuildTarget(target);
#pragma warning restore CS0618 // Type or member is obsolete
        }
#endif
    }
}
