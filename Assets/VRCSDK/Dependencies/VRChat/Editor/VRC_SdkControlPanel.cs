using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.Core;

[ExecuteInEditMode]
public class VRC_SdkControlPanel : EditorWindow
{
    static VRC_SdkControlPanel window;

    public static System.Action _EnableSpatialization = null;   // assigned in AutoAddONSPAudioSourceComponents

    const string kCantPublishContent = "Before you can upload avatars or worlds, you will need to spend some time in VRChat.";
    const string kCantPublishAvatars = "Before you can upload avatars, you will need to spend some time in VRChat.";
    const string kCantPublishWorlds = "Before you can upload worlds, you will need to spend some time in VRChat.";

    [MenuItem("VRChat SDK/Show Build Control Panel")]
    static void Init()
    {
        if (!RemoteConfig.IsInitialized())
        {
            VRC.Core.API.SetOnlineMode(true, "vrchat");
            RemoteConfig.Init(() => Init());
            return;
        }

        window = (VRC_SdkControlPanel)EditorWindow.GetWindow(typeof(VRC_SdkControlPanel));
        window.titleContent.text = "VRChat";

        window.ResetIssues();

        window.Show();
    }

    bool UseDevApi
    {
        get
        {
            return VRC.Core.API.GetApiUrl() == API.devApiUrl;
        }
    }

    private void ResetIssues()
    {
        GUIErrors.Clear();
        GUIInfos.Clear();
        GUIWarnings.Clear();
        GUILinks.Clear();
        checkedForIssues = false;
    }

    private void OnFocus()
    {
        ResetIssues();
    }

    private void OnLostFocus()
    {
        ResetIssues();    
    }

    bool checkedForIssues = false;

    Dictionary<Object, List<string>> GUIErrors = new Dictionary<Object, List<string>>();
    Dictionary<Object, List<string>> GUIWarnings = new Dictionary<Object, List<string>>();
    Dictionary<Object, List<string>> GUIInfos = new Dictionary<Object, List<string>>();
    Dictionary<Object, List<string>> GUILinks = new Dictionary<Object, List<string>>();

    void AddToReport(Dictionary<Object, List<string>> report, Object subject, string output)
    {
        if (subject == null)
            subject = this;
        if (!report.ContainsKey(subject))
            report.Add(subject, new List<string>());
        report[subject].Add(output);
    }
    
    void OnGUIError(Object subject, string output)
    {
        if (!checkedForIssues)
            AddToReport(GUIErrors, subject, output);
    }
    
    void OnGUIWarning(Object subject, string output)
    {
        if (!checkedForIssues)
            AddToReport(GUIWarnings, subject, output);
    }

    void OnGUIInformation(Object subject, string output)
    {
        if (!checkedForIssues)
            AddToReport(GUIInfos, subject, output);
    }

    void OnGUILink(Object subject, string output)
    {
        if (!checkedForIssues)
            AddToReport(GUILinks, subject, output);
    }

    VRCSDK2.VRC_SceneDescriptor[] scenes;
    VRCSDK2.VRC_AvatarDescriptor[] avatars;
    Vector2 scrollPos;

    void Update()
    {
        Repaint();
    }

    void OnGUI()
    {
        if (window == null)
            window = (VRC_SdkControlPanel)EditorWindow.GetWindow(typeof(VRC_SdkControlPanel));

        if (!VRC.AccountEditorWindow.OnShowStatus())
            return;

        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField("You cannot edit your VRChat data while the Unity Application is running");
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("General", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        ShowBuildControls();

        //window.Repaint();
    }

    void ShowBuildControls()
    {
        if (!checkedForIssues)
            EnvConfig.ConfigurePlayerSettings();

        EditorGUILayout.LabelField("Client Version Date", VRC.Core.SDKClientUtilities.GetTestClientVersionDate());
        EditorGUILayout.LabelField("SDK Version Date", VRC.Core.SDKClientUtilities.GetSDKVersionDate());

/**
        // Commented this out 12/12/2017 bc client no longer produces version files, resulting in this warning always appearing - Graham
        if (!VRC.Core.SDKClientUtilities.IsClientNewerThanSDK())
        {
            OnGUIWarning(null, "Your SDK is newer than the VRChat client you're testing with. Some SDK features may not work as expected. You can change VRC clients in VRChat SDK/Settings.");
        }
**/

        if (VRC.Core.RemoteConfig.IsInitialized())
        {
            string sdkUnityVersion = VRC.Core.RemoteConfig.GetString("sdkUnityVersion");
            if (Application.unityVersion != sdkUnityVersion)
            {
                OnGUIWarning(null, "You are not using the recommended Unity version for the VRChat SDK. Content built with this version may not work correctly. Please use Unity " + sdkUnityVersion);
            }
        }

        scenes = (VRCSDK2.VRC_SceneDescriptor[])VRC.Tools.FindSceneObjectsOfTypeAll<VRCSDK2.VRC_SceneDescriptor>();
        List<VRCSDK2.VRC_AvatarDescriptor> allavatars = VRC.Tools.FindSceneObjectsOfTypeAll<VRCSDK2.VRC_AvatarDescriptor>().ToList();
        // select only the active avatars
        avatars = allavatars.Where(av => av.gameObject.activeInHierarchy).ToArray();

        if (scenes.Length > 0 && avatars.Length > 0)
        {
            GameObject[] gos = new GameObject[avatars.Length];
            for (int i = 0; i < avatars.Length; ++i)
                gos[i] = avatars[i].gameObject;
            OnGUIError(null, "a unity scene containing a VRChat Scene Descriptor should not also contain avatars.");
        }
        else if (scenes.Length > 1)
        {
            GameObject[] gos = new GameObject[scenes.Length];
            for (int i = 0; i < scenes.Length; ++i)
                gos[i] = scenes[i].gameObject;
            OnGUIError(null, "a unity scene containing a VRChat Scene Descriptor should only contain one scene descriptor.");
        }
        else if (scenes.Length == 1)
        {
            GUILayout.Label("Scene Options", EditorStyles.boldLabel);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            EditorGUILayout.Space();
            try
            {
                EditorGUI.BeginChangeCheck();

                if (!checkedForIssues)
                    OnGUISceneCheck(scenes[0]);

                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(scenes[0]);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                }

                OnGUIScene(scenes[0]);
                OnGUIShowIssues(scenes[0]);
            }
            catch (System.Exception)
            {
            }
            EditorGUILayout.EndScrollView();
        }
        else if (avatars.Length > 0)
        {
            GUILayout.Label("Avatar Options", EditorStyles.boldLabel);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            foreach (var av in avatars)
            {
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.Space();
                if (!checkedForIssues)
                    OnGUIAvatarCheck(av);

                OnGUIAvatar(av);

                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(av);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                }
            }
            EditorGUILayout.EndScrollView();
        }
        else
        {
            OnGUIError(null, "Please add a scene descriptor or avatar descriptor to your project.");
        }
        OnGUIShowIssues();
        checkedForIssues = true;
    }

    bool showLayerHelp = false;
    int numClients = 1;

    void CheckUploadChanges(VRCSDK2.VRC_SceneDescriptor scene)
    {
        if (UnityEditor.EditorPrefs.HasKey("VRCSDK2_scene_changed") &&
                UnityEditor.EditorPrefs.GetBool("VRCSDK2_scene_changed"))
        {
            UnityEditor.EditorPrefs.DeleteKey("VRCSDK2_scene_changed");

            if (UnityEditor.EditorPrefs.HasKey("VRCSDK2_capacity"))
            {
                scene.capacity = UnityEditor.EditorPrefs.GetInt("VRCSDK2_capacity");
                UnityEditor.EditorPrefs.DeleteKey("VRCSDK2_capacity");
            }
            if (UnityEditor.EditorPrefs.HasKey("VRCSDK2_content_sex"))
            {
                scene.contentSex = UnityEditor.EditorPrefs.GetBool("VRCSDK2_content_sex");
                UnityEditor.EditorPrefs.DeleteKey("VRCSDK2_content_sex");
            }
            if (UnityEditor.EditorPrefs.HasKey("VRCSDK2_content_violence"))
            {
                scene.contentViolence = UnityEditor.EditorPrefs.GetBool("VRCSDK2_content_violence");
                UnityEditor.EditorPrefs.DeleteKey("VRCSDK2_content_violence");
            }
            if (UnityEditor.EditorPrefs.HasKey("VRCSDK2_content_gore"))
            {
                scene.contentGore = UnityEditor.EditorPrefs.GetBool("VRCSDK2_content_gore");
                UnityEditor.EditorPrefs.DeleteKey("VRCSDK2_content_gore");
            }
            if (UnityEditor.EditorPrefs.HasKey("VRCSDK2_content_other"))
            {
                scene.contentOther = UnityEditor.EditorPrefs.GetBool("VRCSDK2_content_other");
                UnityEditor.EditorPrefs.DeleteKey("VRCSDK2_content_other");
            }
            if (UnityEditor.EditorPrefs.HasKey("VRCSDK2_release_public"))
            {
                scene.releasePublic = UnityEditor.EditorPrefs.GetBool("VRCSDK2_release_public");
                UnityEditor.EditorPrefs.DeleteKey("VRCSDK2_release_public");
            }

            EditorUtility.SetDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }
    }

    bool ShouldShowLightmapWarning
    {
        get
        {
            const string GraphicsSettingsAssetPath = "ProjectSettings/GraphicsSettings.asset";
            SerializedObject graphicsManager = new SerializedObject(UnityEditor.AssetDatabase.LoadAllAssetsAtPath(GraphicsSettingsAssetPath)[0]);
            SerializedProperty lightmapStripping = graphicsManager.FindProperty("m_LightmapStripping");
            return lightmapStripping.enumValueIndex == 0;
        }
    }

    bool ShouldShowFogWarning
    {
        get
        {
            const string GraphicsSettingsAssetPath = "ProjectSettings/GraphicsSettings.asset";
            SerializedObject graphicsManager = new SerializedObject(UnityEditor.AssetDatabase.LoadAllAssetsAtPath(GraphicsSettingsAssetPath)[0]);
            SerializedProperty lightmapStripping = graphicsManager.FindProperty("m_FogStripping");
            return lightmapStripping.enumValueIndex == 0;
        }
    }

    void OnGUIShowIssues(Object subject = null)
    {
        if (subject == null)
            subject = this;

        if (GUIErrors.ContainsKey(subject))
            foreach (string error in GUIErrors[subject].Where(s => !string.IsNullOrEmpty(s)))
                EditorGUILayout.HelpBox(error, MessageType.Error);
        if (GUIWarnings.ContainsKey(subject))
            foreach (string error in GUIWarnings[subject].Where(s => !string.IsNullOrEmpty(s)))
                EditorGUILayout.HelpBox(error, MessageType.Warning);
        if (GUIInfos.ContainsKey(subject))
            foreach (string error in GUIInfos[subject].Where(s => !string.IsNullOrEmpty(s)))
                EditorGUILayout.HelpBox(error, MessageType.Info);
        if (GUILinks.ContainsKey(subject))
            foreach (string error in GUILinks[subject].Where(s => !string.IsNullOrEmpty(s)))
                EditorGUILayout.SelectableLabel(error);
    }

    void OnGUISceneCheck(VRCSDK2.VRC_SceneDescriptor scene)
    {
        CheckUploadChanges(scene);

        EditorGUILayout.InspectorTitlebar(true, scene.gameObject);
        
        if (VRC.Core.APIUser.CurrentUser != null && VRC.Core.APIUser.CurrentUser.hasScriptingAccess && !CustomDLLMaker.DoesScriptDirExist())
        {
            CustomDLLMaker.CreateDirectories();
        }

        Vector3 g = Physics.gravity;
        if (g.x != 0.0f || g.z != 0.0f)
            OnGUIWarning(scene, "Gravity vector is not straight down. Though we support different gravity, player orientation is always 'upwards' so things don't always behave as you intend.");
        if (g.y > 0)
            OnGUIWarning(scene, "Gravity vector is not straight down, inverted or zero gravity will make walking extremely difficult.");
        if (g.y == 0)
            OnGUIWarning(scene, "Zero gravity will make walking extremely difficult, though we support different gravity, player orientation is always 'upwards' so this may not have the effect you're looking for.");

        #if PLAYMAKER
            if (VRCSDK2.VRC_PlaymakerHelper.ValidatePlaymaker() == false)
                OnGUIError(scene, VRCSDK2.VRC_PlaymakerHelper.GetErrors());
        #endif

        scene.useAssignedLayers = true;
        if (scene.useAssignedLayers)
        {
            if (!UpdateLayers.AreLayersSetup())
                OnGUIWarning(scene, "Layers are not setup properly. Please press the button above.");

            if (UpdateLayers.AreLayersSetup() && !UpdateLayers.IsCollisionLayerMatrixSetup())
                OnGUIWarning(scene, "Physics Collision Layer Matrix is not setup correctly. Please press the button above.");
        }

        // warn those without scripting access if they choose to script locally
        if(VRC.Core.APIUser.CurrentUser != null && !VRC.Core.APIUser.CurrentUser.hasScriptingAccess && CustomDLLMaker.DoesScriptDirExist())
        {
            OnGUIWarning(scene, "Your account does not have permissions to upload custom scripts. You can test locally but need to contact VRChat to publish your world with scripts.");
        }

        foreach (VRCSDK2.VRC_DataStorage ds in GameObject.FindObjectsOfType<VRCSDK2.VRC_DataStorage>())
        {
            VRCSDK2.VRC_ObjectSync os = ds.GetComponent<VRCSDK2.VRC_ObjectSync>();
            if (os != null && os.SynchronizePhysics)
                OnGUIWarning(scene, ds.name + " has a VRC_DataStorage and VRC_ObjectSync, with SynchronizePhysics enabled.");
        }

        // auto create VRCScript dir for those with access
        if (VRC.Core.APIUser.CurrentUser != null && VRC.Core.APIUser.CurrentUser.hasScriptingAccess && !CustomDLLMaker.DoesScriptDirExist())
        {
            CustomDLLMaker.CreateDirectories();
        }
        
        if (scene.UpdateTimeInMS < (int)(1000f / 90f * 3f))
            OnGUIWarning(scene, "Room has a very fast update rate; experience may suffer with many users.");
    }

    void OnGUIScene(VRCSDK2.VRC_SceneDescriptor scene)
    {
        string lastUrl = VRC_SdkBuilder.GetLastUrl();
        bool lastBuildPresent = lastUrl != null;

        string worldVersion = "-1";
        PipelineManager[] pms = (PipelineManager[])VRC.Tools.FindSceneObjectsOfTypeAll<PipelineManager>();
        if (pms.Length == 1 && !string.IsNullOrEmpty(pms[0].blueprintId))
        {
            if (scene.apiWorld == null)
            {
                ApiWorld world = API.FromCacheOrNew<ApiWorld>(pms[0].blueprintId);
                world.Fetch(null, false, 
                    (c) => scene.apiWorld = c.Model as ApiWorld, 
                    (c) =>
                    {
                        if (c.Code == 404)
                        {
                            Debug.LogErrorFormat("Could not load world {0} because it didn't exist.", pms[0].blueprintId);
                            ApiCache.Invalidate<ApiWorld>(pms[0].blueprintId);
                        }
                        else
                            Debug.LogErrorFormat("Could not load world {0} because {1}", pms[0].blueprintId, c.Error);
                    });
                scene.apiWorld = world;
            }
            worldVersion = (scene.apiWorld as ApiWorld).version.ToString();
        }
        EditorGUILayout.LabelField("World Version: " + worldVersion);

        EditorGUILayout.Space();

        if (scene.useAssignedLayers)
        {
            if (!UpdateLayers.AreLayersSetup() && GUILayout.Button("Setup Layers"))
            {
                bool doIt = EditorUtility.DisplayDialog("Setup Layers for VRChat", "This adds all VRChat layers to your project and pushes any custom layers down the layer list. If you have custom layers assigned to gameObjects, you'll need to reassign them. Are you sure you want to continue?", "Do it!", "Don't do it");
                if (doIt)
                {
                    UpdateLayers.SetupEditorLayers();
                }
            }

            if (UpdateLayers.AreLayersSetup() && !UpdateLayers.IsCollisionLayerMatrixSetup() && GUILayout.Button("Setup Collision Layer Matrix"))
            {
                bool doIt = EditorUtility.DisplayDialog("Setup Collision Layer Matrix for VRChat", "This will setup the correct physics collisions in the PhysicsManager for VRChat layers. Are you sure you want to continue?", "Do it!", "Don't do it");
                if (doIt)
                {
                    UpdateLayers.SetupCollisionLayerMatrix();
                }
            }
        }

        scene.autoSpatializeAudioSources = EditorGUILayout.ToggleLeft("Apply 3D spatialization to AudioSources automatically at runtime (override settings by adding an ONSPAudioSource component to game object)", scene.autoSpatializeAudioSources);
        if (GUILayout.Button("Enable 3D spatialization on all 3D AudioSources in scene now"))
        {
            bool doIt = EditorUtility.DisplayDialog("Enable Spatialization", "This will add an ONSPAudioSource script to every 3D AudioSource in the current scene, and enable default settings for spatialization.  Are you sure you want to continue?", "Do it!", "Don't do it");
            if (doIt)
            {
                if (_EnableSpatialization != null)
                    _EnableSpatialization();
                else
                    Debug.LogError("VrcSdkControlPanel: EnableSpatialization callback not found!");
            }
        }

        GUI.enabled = (GUIErrors.Count == 0 && checkedForIssues);
        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField("Test", EditorStyles.boldLabel);
        numClients = EditorGUILayout.IntField("Number of Clients", numClients);
        if (lastBuildPresent == false)
            GUI.enabled = false;
        if (GUILayout.Button("Last Build"))
        {
            VRC_SdkBuilder.shouldBuildUnityPackage = false;
            VRC_SdkBuilder.numClientsToLaunch = numClients;
            VRC_SdkBuilder.RunLastExportedSceneResource();
        }
        if (APIUser.CurrentUser.hasSuperPowers)
        {
            if (GUILayout.Button("Copy Test URL"))
            {
                TextEditor te = new TextEditor();
                te.text = lastUrl;
                te.SelectAll();
                te.Copy();
            }
        }
        if (lastBuildPresent == false)
            GUI.enabled = true;
        if (GUILayout.Button("New Build"))
        {
            EnvConfig.ConfigurePlayerSettings();
            VRC_SdkBuilder.shouldBuildUnityPackage = false;
            VRC.AssetExporter.CleanupUnityPackageExport();  // force unity package rebuild on next publish
            VRC_SdkBuilder.numClientsToLaunch = numClients;
            VRC_SdkBuilder.PreBuildBehaviourPackaging();
            VRC_SdkBuilder.ExportSceneResourceAndRun();
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField("Publish", EditorStyles.boldLabel);
        if (lastBuildPresent == false)
            GUI.enabled = false;
        if (GUILayout.Button("Last Build"))
        {
            if (APIUser.CurrentUser.canPublishWorlds)
            {
                VRC_SdkBuilder.shouldBuildUnityPackage = VRC.AccountEditorWindow.FutureProofPublishEnabled;
                VRC_SdkBuilder.UploadLastExportedSceneBlueprint();
            }
            else
            {
                ShowContentPublishPermissionsDialog();
            }
        }
        if (lastBuildPresent == false)
            GUI.enabled = true;
        if (GUILayout.Button("New Build"))
        {
            if (APIUser.CurrentUser.canPublishWorlds)
            {
                EnvConfig.ConfigurePlayerSettings();
                VRC_SdkBuilder.shouldBuildUnityPackage = VRC.AccountEditorWindow.FutureProofPublishEnabled;
                VRC_SdkBuilder.PreBuildBehaviourPackaging();
                VRC_SdkBuilder.ExportAndUploadSceneBlueprint();
            }
            else
            {
                ShowContentPublishPermissionsDialog();
            }
        }
        EditorGUILayout.EndVertical();
        GUI.enabled = true;
    }

    void OnGUISceneLayer(int layer, string name, string description)
    {
        if (LayerMask.LayerToName(layer) != name)
            OnGUIError(null, "Layer " + layer + " must be renamed to '" + name + "'");

        if (showLayerHelp)
            OnGUIInformation(null, "Layer " + layer + " " + name + "\n" + description);
    }

    int CountPolygons(Renderer r)
    {
        int result = 0;
        SkinnedMeshRenderer smr = r as SkinnedMeshRenderer;
        if (smr != null)
        {
            if (smr.sharedMesh == null)
                return 0;

            for (int i = 0; i < smr.sharedMesh.subMeshCount; ++i)
                result += smr.sharedMesh.GetTriangles(i).Length / 3;
        }

        ParticleSystemRenderer pr = r as ParticleSystemRenderer;
        if (pr != null)
        {
            result += pr.GetComponent<ParticleSystem>().main.maxParticles;
        }

        MeshRenderer mr = r as MeshRenderer;
        if (mr != null)
        {
            var mf = mr.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
                return 0;
            for (int i = 0; i < mf.sharedMesh.subMeshCount; ++i)
                result += mf.sharedMesh.GetTriangles(i).Length / 3;
        }

        return result;
    }

    void AnalyzeGeometry(GameObject go, out Bounds bounds, out int polycount)
    {
        polycount = 0;
        bounds = new Bounds(go.transform.position, Vector3.zero);
        List<Renderer> ignore = new List<Renderer>();

        var lods = go.GetComponentsInChildren<LODGroup>();
        foreach (var lod in lods)
        {
            LOD[] options = lod.GetLODs();

            int highestLodPolies = 0;
            foreach (LOD l in options)
            {
                int thisLodPolies = 0;
                foreach (Renderer r in l.renderers)
                {
                    ignore.Add(r);
                    thisLodPolies += CountPolygons(r);
                }
                if (thisLodPolies > highestLodPolies)
                    highestLodPolies = thisLodPolies;
            }

            polycount += highestLodPolies;
        }

        var renderers = go.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            if( (r as ParticleSystemRenderer) == null )
                bounds.Encapsulate(r.bounds);

            if (ignore.Contains(r) == false)
                polycount += CountPolygons(r);
        }

        bounds.center -= go.transform.position;
    }

    bool IsAncestor(Transform ancestor, Transform child)
    {
        bool found = false;
        Transform thisParent = child.parent;
        while (thisParent != null)
        {
            if (thisParent == ancestor) { found = true; break; }
            thisParent = thisParent.parent;
        }

        return found;
    }

    List<Transform> FindBonesBetween(Transform top, Transform bottom)
    {
        List<Transform> list = new List<Transform>();
        if (top == null || bottom == null) return list;
        Transform bt = top.parent;
        while (bt != bottom && bt != null)
        {
            list.Add(bt);
            bt = bt.parent;
        }
        return list;
    }

    // Attempts to remap a mecanim rig so that the upper chest bone
    // is blank, by moving the upper chest bone to chest and rebuilding
    // CURRENTLY DOES NOT WORK!
    void UpperChestFix(VRCSDK2.VRC_AvatarDescriptor ad, GameObject avObj, Animator anim)
    {
        // if upper chest was mapped we need to reconfigure rig
        // by moving upper chest to chest
        Transform pelvis = anim.GetBoneTransform(HumanBodyBones.Hips);
        Transform upchest = anim.GetBoneTransform(HumanBodyBones.UpperChest);
        Transform chest = anim.GetBoneTransform(HumanBodyBones.Chest);
        Transform torso = anim.GetBoneTransform(HumanBodyBones.Spine);
        Avatar origAvatar = anim.avatar;

        if (upchest != null)
        {
            // get every child transform of the animator
            Transform[] allBones = anim.GetComponentsInChildren<Transform>();

            // get a list of the extra spine bones between spine and pelvis
            List<Transform> extras = FindBonesBetween(torso, pelvis);

            HumanDescription desc = new HumanDescription();
            desc.upperArmTwist = 0.5f;
            desc.lowerArmTwist = 0.5f;
            desc.upperLegTwist = 0.5f;
            desc.lowerLegTwist = 0.5f;
            desc.armStretch = 0.05f;
            desc.legStretch = 0.05f;
            desc.feetSpacing = 0.0f;
            List<HumanBone> hbList = new List<HumanBone>();
            List<SkeletonBone> sbList = new List<SkeletonBone>();
            HumanBodyBones[] hbbArray = (HumanBodyBones[])System.Enum.GetValues(typeof(HumanBodyBones));
            Dictionary<Transform, string> hbbDict = new Dictionary<Transform, string>();

            for (int i=0; i<hbbArray.Length; i++)
            {
                Transform t = anim.GetBoneTransform(hbbArray[i]);
                string n = hbbArray[i].ToString();
                if (t != null && n != "LastBone")
                {
                    hbbDict[t] = n;
                    //Debug.LogError("Dictionary Added:"+hbbArray[i].ToString());
                }
            }

            foreach (Transform bt in allBones)
            {
                // map the human bones
                if (hbbDict.Keys.Contains(bt))
                {
                    string hbName = hbbDict[bt];
                    //Debug.LogError("Processing: "+hbName);
                    if (hbName != "Spine" && bt != null && !extras.Contains(bt))
                    {
                        if (bt == upchest) hbName = "Chest";
                        else if (bt == chest) hbName = "Spine";
                        HumanBone hb = new HumanBone();
                        hb.boneName = bt.name;
                        hb.humanName = hbName;
                        //Debug.Log("Mapped human bone:" + hb.humanName + " to " + hb.boneName);
                        hbList.Add(hb);
                    }
                    else
                    {
                        //Debug.LogError("Skipped:" + hbbDict[bt]);
                    }
                }

                if (bt != null)
                {
                    // THESE POSITIONS/ROTATIONS MUST BE FOR TPOSE !!!
                    SkeletonBone sb = new SkeletonBone();
                    sb.name = bt.name;
                    sb.position = bt.position;
                    sb.rotation = bt.rotation;
                    sb.scale = bt.localScale;
                    sbList.Add(sb);
                }
            }

            // add any root bones above hip
            Transform root = pelvis.parent;
            while (root != null && root != anim.transform)
            {
                // THESE POSITIONS/ROTATIONS MUST BE FOR TPOSE !!!
                SkeletonBone sb = new SkeletonBone();
                sb.name = root.name;
                sb.position = root.position;
                sb.rotation = root.rotation;
                sb.scale = root.localScale;
                sbList.Add(sb);
                root = root.parent;
            }

            desc.human = hbList.ToArray();
            desc.skeleton = sbList.ToArray();
            anim.avatar = AvatarBuilder.BuildHumanAvatar(avObj, desc);
            if (anim.avatar.isValid && anim.avatar.isHuman)
            {
                anim.avatar.name = "{ADJUSTED}"+origAvatar.name;
                // shift all the bone mappings
                torso = chest;
                chest = upchest;
                upchest = null;
            }
            else
            {
                OnGUIError(ad, "Automatic rig adjustment on "+origAvatar.name+" failed. You will need to manually configure the humanoid rig. Make sure the UpperChest slot is empty.");
                anim.avatar = origAvatar;
                return;
            }
        }

        if (anim.avatar.name.StartsWith("{ADJUSTED}"))
        {
            OnGUIWarning(ad, "Your rig has the UPPERCHEST mapped in the Humanoid Rig, and was automatically corrected " +
                             "to use the CHEST instead. If you run into issues we recommend leaving the " +
                             "UPPERCHEST blank and mapping your top spine bone to the CHEST.");
        }
    }

    bool AnalyzeIK(VRCSDK2.VRC_AvatarDescriptor ad, GameObject avObj, Animator anim)
    {
        bool hasHead = false;
        bool hasFeet = false;
        bool hasHands = false;
        bool hasThreeFingers = false;
        //bool hasToes = false;
        bool correctSpineHierarchy = false;
        bool correctArmHierarchy = false;
        bool correctLegHierarchy = false;

        bool status = true;

        Transform head = anim.GetBoneTransform(HumanBodyBones.Head);
        Transform lfoot = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
        Transform rfoot = anim.GetBoneTransform(HumanBodyBones.RightFoot);
        Transform lhand = anim.GetBoneTransform(HumanBodyBones.LeftHand);
        Transform rhand = anim.GetBoneTransform(HumanBodyBones.RightHand);

        hasHead = null!=head;
        hasFeet = (null!=lfoot && null!=rfoot);
        hasHands = (null!=lhand && null!=rhand);

        if (!hasHead || !hasFeet || !hasHands)
        {
            OnGUIError(ad, "Humanoid avatar must have head, hands and feet bones mapped.");
            return false;
        }

        Transform lthumb = anim.GetBoneTransform(HumanBodyBones.LeftThumbProximal);
        Transform lindex = anim.GetBoneTransform(HumanBodyBones.LeftIndexProximal);
        Transform lmiddle = anim.GetBoneTransform(HumanBodyBones.LeftMiddleProximal);
        Transform rthumb = anim.GetBoneTransform(HumanBodyBones.RightThumbProximal);
        Transform rindex = anim.GetBoneTransform(HumanBodyBones.RightIndexProximal);
        Transform rmiddle = anim.GetBoneTransform(HumanBodyBones.RightMiddleProximal);

        hasThreeFingers = null!=lthumb && null!=lindex && null!=lmiddle && null!=rthumb && null!=rindex && null!=rmiddle;

        if (!hasThreeFingers)
        {
            // although its only a warning, we return here because the rest
            // of the analysis is for VRIK
            OnGUIWarning(ad, "Thumb, Index, and Middle finger bones are not mapped, Full-Body IK will be disabled."); 
            status = false;
        }

        if (anim.GetBoneTransform(HumanBodyBones.UpperChest) != null)
        {
            OnGUIError(ad, "Your rig has the UPPERCHEST mapped in the Humanoid Rig. This will cause problems with IK.");
            return false;
        }

        Transform pelvis = anim.GetBoneTransform(HumanBodyBones.Hips);
        Transform chest = anim.GetBoneTransform(HumanBodyBones.Chest);
        Transform torso = anim.GetBoneTransform(HumanBodyBones.Spine);

        Transform neck = anim.GetBoneTransform(HumanBodyBones.Neck);
        Transform lclav = anim.GetBoneTransform(HumanBodyBones.LeftShoulder);
        Transform rclav = anim.GetBoneTransform(HumanBodyBones.RightShoulder);

        if (null==neck || null==lclav || null==rclav || null==pelvis || null==torso || null==chest)
        {
            OnGUIError(ad, "Spine hierarchy missing elements, make sure that Pelvis, Spine, Chest, Neck and Shoulders are mapped.");
            return false;
        }

        correctSpineHierarchy = lclav.parent == chest && rclav.parent == chest && neck.parent == chest;

        if (!correctSpineHierarchy)
        {
            OnGUIError(ad, "Spine hierarchy incorrect. Make sure that the parent of both Shoulders and the Neck is the Chest.");
            return false;
        }

        Transform lshoulder = anim.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        Transform lelbow = anim.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        Transform rshoulder = anim.GetBoneTransform(HumanBodyBones.RightUpperArm);
        Transform relbow = anim.GetBoneTransform(HumanBodyBones.RightLowerArm);

        correctArmHierarchy = lshoulder.GetChild(0) == lelbow && lelbow.GetChild(0) == lhand &&
            rshoulder.GetChild(0) == relbow && relbow.GetChild(0) == rhand;

        if (!correctArmHierarchy)
        {
            OnGUIWarning(ad, "LowerArm is not first child of UpperArm or Hand is not first child of LowerArm: you may have problems with Forearm rotations.");
            status = false;
        }

        Transform lhip = anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        Transform lknee = anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        Transform rhip = anim.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        Transform rknee = anim.GetBoneTransform(HumanBodyBones.RightLowerLeg);

        correctLegHierarchy = lhip.GetChild(0) == lknee && lknee.GetChild(0) == lfoot &&
            rhip.GetChild(0) == rknee && rknee.GetChild(0) == rfoot;

        if (!correctLegHierarchy)
        {
            OnGUIWarning(ad, "LowerLeg is not first child of UpperLeg or Foot is not first child of LowerLeg: you may have problems with Shin rotations.");
            status = false;
        }

        if ( !(IsAncestor(pelvis, rfoot) && IsAncestor(pelvis, lfoot) && IsAncestor(pelvis, lhand) || IsAncestor(pelvis, rhand) || IsAncestor(pelvis, lhand)) )
        {
            OnGUIWarning(ad, "This avatar has a split heirarchy (Hips bone is not the ancestor of all humanoid bones). IK may not work correctly.");
            status = false;
        }

        // if thigh bone rotations diverge from 180 from hip bone rotations, full-body tracking/ik does not work well
        Vector3 hipLocalUp = pelvis.InverseTransformVector(Vector3.up);
        Vector3 legLDir = lhip.TransformVector(hipLocalUp);
        Vector3 legRDir = rhip.TransformVector(hipLocalUp);
        float angL = Vector3.Angle(Vector3.up, legLDir);
        float angR = Vector3.Angle(Vector3.up, legRDir);
        if ( angL < 175f || angR < 175f )
        {
            string angle = string.Format("{0:F1}", Mathf.Min(angL, angR));
            OnGUIWarning(ad, "The angle between pelvis and thigh bones should be close to 180 degrees (this avatar's angle is "+angle+"). Your avatar may not work well with full-body IK and Tracking.");
            status = false;
        }
        return status;
    }

    void OnGUIAvatarCheck(VRCSDK2.VRC_AvatarDescriptor avatar)
    {
        int polycount;
        Bounds bounds;
        AnalyzeGeometry(avatar.gameObject, out bounds, out polycount);
        if (polycount < 10000)
            OnGUIInformation(avatar, "Polygons: " + polycount);
        else if (polycount < 15000)
            OnGUIWarning(avatar, "Polygons: " + polycount + " - Please try to reduce your avatar poly count to less thatn 10k.");
        else if (polycount < 20000)
            OnGUIWarning(avatar, "Polygons: " + polycount + " - This avatar will not perform well on many systems.");
        else
            OnGUIError(avatar, "Polygons: " + polycount + " - This avatar has too many polygons. It must have less than 20k and should have less than 10k.");

        if (bounds.size.x > 5f || bounds.size.y > 6.0f || bounds.size.z > 5f)
            OnGUIError(avatar, "This avatar measures too large on at least one axis. It must be <5m on a side but it's bounds are " + bounds.size.ToString());

        var eventHandler = avatar.GetComponentInChildren<VRCSDK2.VRC_EventHandler>();
        if (eventHandler != null)
        {
            OnGUIError(avatar, "This avatar contains an EventHandler, which is not currently supported in VRChat.");
        }

        if (avatar.lipSync == VRCSDK2.VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape && avatar.VisemeSkinnedMesh == null)
            OnGUIError(avatar, "This avatar uses Visemes but the Face Mesh is not specified.");

        var anim = avatar.GetComponent<Animator>();
        if (anim == null)
        {
            OnGUIWarning(avatar, "This avatar does not contain an animator, and will not animate in VRChat.");
        }
        else if (anim.isHuman == false)
        {
            OnGUIWarning(avatar, "This avatar is not imported as a humanoid rig and will not play VRChat's provided animation set.");
        }
        else if (avatar.gameObject.activeInHierarchy == false)
        {
            OnGUIError(avatar, "Your avatar is disabled in the scene heirarchy!");
        }
        else
        {
            Transform foot = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
            Transform shoulder = anim.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            if (foot == null)
                OnGUIError(avatar, "Your avatar is humanoid, but it's feet aren't specified!");
            if (shoulder == null)
                OnGUIError(avatar, "Your avatar is humanoid, but it's upper arms aren't specified!");

            if (foot != null && shoulder != null)
            {
                Vector3 footPos = foot.position - avatar.transform.position;
                if (footPos.y < 0)
                    OnGUIWarning(avatar, "Avatar feet are beneath the avatar's origin (the floor). That's probably not what you want.");
                Vector3 shoulderPosition = shoulder.position - avatar.transform.position;
                if (shoulderPosition.y < 0.2f)
                    OnGUIError(avatar, "This avatar is too short. The minimum is 20cm shoulder height.");
                else if (shoulderPosition.y < 1.0f)
                    OnGUIWarning(avatar, "This avatar is short. This is probably shorter than you want.");
                else if (shoulderPosition.y > 5.0f)
                    OnGUIWarning(avatar, "This avatar is too tall. The maximum is 5m shoulder height.");
                else if (shoulderPosition.y > 2.5f)
                    OnGUIWarning(avatar, "This avatar is tall. This is probably taller than you want.");
            }
            
            if ( AnalyzeIK(avatar, avatar.gameObject, anim) == false )
                OnGUILink(avatar, "See https://docs.vrchat.com/docs/rig-requirements for details."); 
        }

        IEnumerable<Component> componentsToRemove = VRCSDK2.AvatarValidation.FindIllegalComponents(avatar.Name, avatar.gameObject);
        HashSet<string> componentsToRemoveNames = new HashSet<string>();
        foreach (Component c in componentsToRemove)
        {
            if (componentsToRemoveNames.Contains(c.GetType().Name) == false)
                componentsToRemoveNames.Add(c.GetType().Name);
        }

        if (componentsToRemoveNames.Count > 0)
            OnGUIError(avatar, "The following component types are found on the Avatar and will be removed by the client: " + string.Join(", ", componentsToRemoveNames.ToArray()));

        if (VRCSDK2.AvatarValidation.EnforceAudioSourceLimits(avatar.gameObject).Count > 0)
            OnGUIWarning(avatar, "Audio sources found on Avatar, they will be adjusted to safe limits, if necessary.");

        if (avatar.gameObject.GetComponentInChildren<Camera>() != null)
            OnGUIWarning(avatar, "Cameras are removed from non-local avatars at runtime.");
    }

    void OnGUIAvatar(VRCSDK2.VRC_AvatarDescriptor avatar)
    {
        EditorGUILayout.InspectorTitlebar(avatar.gameObject.activeInHierarchy, avatar.gameObject);

        GUI.enabled = (GUIErrors.Count == 0 && checkedForIssues) || APIUser.CurrentUser.developerType == APIUser.DeveloperType.Internal;
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Build & Publish"))
        {
            if (APIUser.CurrentUser.canPublishAvatars)
            {
                VRC_SdkBuilder.shouldBuildUnityPackage = VRC.AccountEditorWindow.FutureProofPublishEnabled;
                VRC_SdkBuilder.ExportAndUploadAvatarBlueprint(avatar.gameObject); 
            }
            else
            {
                ShowContentPublishPermissionsDialog();
            }
        }
        EditorGUILayout.EndHorizontal();
        GUI.enabled = true;

        OnGUIShowIssues(avatar);
    }

    public static void ShowContentPublishPermissionsDialog()
    {
        if (!RemoteConfig.IsInitialized())
        {
            RemoteConfig.Init(() => ShowContentPublishPermissionsDialog());
            return;
        }

        string message = RemoteConfig.GetString("sdkNotAllowedToPublishMessage");
        int result = UnityEditor.EditorUtility.DisplayDialogComplex("VRChat SDK", message, "Developer FAQ", "VRChat Discord", "OK");
        if (result == 0)
        {
            ShowDeveloperFAQ();
        }
        if (result == 1)
        {
            ShowVRChatDiscord();
        }
    }

    [MenuItem("VRChat SDK/Help/Developer FAQ")]
    public static void ShowDeveloperFAQ()
    {
        if (!RemoteConfig.IsInitialized())
        {
            RemoteConfig.Init(() => ShowDeveloperFAQ());
            return;
        }

        Application.OpenURL(RemoteConfig.GetString("sdkDeveloperFaqUrl"));
    }

    [MenuItem("VRChat SDK/Help/VRChat Discord")]
    public static void ShowVRChatDiscord()
    {
        if (!RemoteConfig.IsInitialized())
        {
            RemoteConfig.Init(() => ShowVRChatDiscord());
            return;
        }

        Application.OpenURL(RemoteConfig.GetString("sdkDiscordUrl"));
    }
}
