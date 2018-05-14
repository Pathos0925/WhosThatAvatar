using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using VRC.Core;
using System;
using System.IO;

namespace VRCSDK2
{
    public class RuntimeWorldCreation : RuntimeAPICreation
    {
        public GameObject waitingPanel;
        public GameObject blueprintPanel;
        public GameObject errorPanel;

        public Text titleText;
        public InputField blueprintName;
        public InputField blueprintDescription;
        public InputField worldCapacity;
        public RawImage bpImage;
        public Image liveBpImage;
        public Toggle shouldUpdateImageToggle;
        public Toggle releasePublic;
        public Toggle contentNsfw;

        public Toggle contentSex;
        public Toggle contentViolence;
        public Toggle contentGore;
        public Toggle contentOther;

        public Toggle contentFeatured;
        public Toggle contentSDKExample;

        public Image showInWorldsMenuGroup;
        public Toggle showInActiveWorlds;
        public Toggle showInPopularWorlds;
        public Toggle showInNewWorlds;

        public UnityEngine.UI.Button uploadButton;

        private ApiWorld worldRecord;

#if UNITY_EDITOR
        new void Start()
        {
            if (!Application.isEditor || !Application.isPlaying)
                return;

            base.Start();

            Application.runInBackground = true;
            UnityEngine.VR.VRSettings.enabled = false;

            uploadButton.onClick.AddListener(SetupUpload);

            shouldUpdateImageToggle.onValueChanged.AddListener(ToggleUpdateImage);

            releasePublic.gameObject.SetActive(false);

            ApiCredentials.Load();
            APIUser.Login(
                delegate (APIUser user)
                {
                    UserLoggedInCallback(user);
                },
                delegate (string err)
                {
                    VRC.Core.Logger.LogError("Could not log in - " + err, DebugLevel.Always);
                    blueprintPanel.SetActive(false);
                    errorPanel.SetActive(true);
                }
            );
        }

        void UserLoggedInCallback(APIUser user)
        {
            pipelineManager.user = user;

            API.Fetch<ApiWorld>(pipelineManager.blueprintId,
                (c) =>
                {
                    Debug.Log("<color=magenta>Updating an existing world.</color>");
                    worldRecord = c.Model as ApiWorld;
                    pipelineManager.completedSDKPipeline = !string.IsNullOrEmpty(worldRecord.authorId);
                    SetupUI();
                },
                (c) =>
                {
                    Debug.Log("<color=magenta>World record not found, creating a new world.</color>");
                    worldRecord = new ApiWorld { capacity = 8 };
                    pipelineManager.completedSDKPipeline = false;
                    worldRecord.id = pipelineManager.blueprintId;
                    SetupUI();
                });
        }

        void SetupUI()
        {
            if (!ValidateAssetBundleBlueprintID(worldRecord.id))
            {
                blueprintPanel.SetActive(false);
                errorPanel.SetActive(true);
                OnSDKPipelineError("The asset bundle is out of date.  Please rebuild the scene using 'New Build'.", "The blueprint ID in the scene does not match the id in the asset bundle.");
                return;
            }

            contentFeatured.gameObject.SetActive(APIUser.CurrentUser.hasSuperPowers);
            contentSDKExample.gameObject.SetActive(APIUser.CurrentUser.hasSuperPowers);

            if (APIUser.Exists(pipelineManager.user))
            {
                waitingPanel.SetActive(false);
                blueprintPanel.SetActive(true);
                errorPanel.SetActive(false);

                if (string.IsNullOrEmpty(worldRecord.authorId) || worldRecord.authorId == pipelineManager.user.id)
                {
                    titleText.text = "Configure World";
                    blueprintName.text = worldRecord.name;
                    worldCapacity.text = worldRecord.capacity.ToString();
                    contentSex.isOn = worldRecord.tags.Contains("content_sex");
                    contentViolence.isOn = worldRecord.tags.Contains("content_violence");
                    contentGore.isOn = worldRecord.tags.Contains("content_gore");
                    contentOther.isOn = worldRecord.tags.Contains("content_other");
                    shouldUpdateImageToggle.interactable = isUpdate;
                    shouldUpdateImageToggle.isOn = !isUpdate;
                    liveBpImage.enabled = !isUpdate;
                    bpImage.enabled = isUpdate;

                    if (!APIUser.CurrentUser.hasSuperPowers)
                    {
                        releasePublic.gameObject.SetActive(false);
                        releasePublic.isOn = false;
                        releasePublic.interactable = false;

                        contentFeatured.isOn = contentSDKExample.isOn = false;
                    }
                    else
                    {
                        contentFeatured.isOn = worldRecord.tags.Contains("content_featured");
                        contentSDKExample.isOn = worldRecord.tags.Contains("content_sdk_example");
                        releasePublic.isOn = worldRecord.releaseStatus == "public";
                        releasePublic.gameObject.SetActive(true);
                    }

                    // "show in worlds menu"
                    if (APIUser.CurrentUser.hasSuperPowers)
                    {
                        showInWorldsMenuGroup.gameObject.SetActive(true);
                        showInActiveWorlds.isOn = !worldRecord.tags.Contains("admin_hide_active");
                        showInPopularWorlds.isOn = !worldRecord.tags.Contains("admin_hide_popular");
                        showInNewWorlds.isOn = !worldRecord.tags.Contains("admin_hide_new");
                    }
                    else
                    {
                        showInWorldsMenuGroup.gameObject.SetActive(false);
                    }

                    blueprintDescription.text = worldRecord.description;

                    ImageDownloader.DownloadImage(worldRecord.imageUrl, delegate (Texture2D obj) {
                        bpImage.texture = obj;
                    });
                }
                else // user does not own world id associated with descriptor
                {
                    Debug.LogErrorFormat("{0} is not an owner of {1}", worldRecord.authorId, pipelineManager.user.id);
                    blueprintPanel.SetActive(false);
                    errorPanel.SetActive(true);
                }
            }
            else
            {
                waitingPanel.SetActive(true);
                blueprintPanel.SetActive(false);
                errorPanel.SetActive(false);

                if (!APIUser.CurrentUser.hasSuperPowers)
                {
                    releasePublic.gameObject.SetActive(false);
                    releasePublic.isOn = false;
                    releasePublic.interactable = false;
                }
                else
                {
                    releasePublic.gameObject.SetActive(true);
                    releasePublic.isOn = false;
                }
            }
        }

        public void SetupUpload()
        {
            uploadTitle = "Preparing For Upload";
            isUploading = true;

            string abPath = UnityEditor.EditorPrefs.GetString("currentBuildingAssetBundlePath");

            string pluginPath = "";
            if (APIUser.CurrentUser.hasScriptingAccess)
                pluginPath = UnityEditor.EditorPrefs.GetString("externalPluginPath");


            string unityPackagePath = UnityEditor.EditorPrefs.GetString("VRC_exportedUnityPackagePath");

            UnityEditor.EditorPrefs.SetBool("VRCSDK2_scene_changed", true);
            UnityEditor.EditorPrefs.SetInt("VRCSDK2_capacity", System.Convert.ToInt16(worldCapacity.text));
            UnityEditor.EditorPrefs.SetBool("VRCSDK2_content_sex", contentSex.isOn);
            UnityEditor.EditorPrefs.SetBool("VRCSDK2_content_violence", contentViolence.isOn);
            UnityEditor.EditorPrefs.SetBool("VRCSDK2_content_gore", contentGore.isOn);
            UnityEditor.EditorPrefs.SetBool("VRCSDK2_content_other", contentOther.isOn);
            UnityEditor.EditorPrefs.SetBool("VRCSDK2_release_public", releasePublic.isOn);
            UnityEditor.EditorPrefs.SetBool("VRCSDK2_content_featured", contentFeatured.isOn);
            UnityEditor.EditorPrefs.SetBool("VRCSDK2_content_sdk_example", contentSDKExample.isOn);

            if (string.IsNullOrEmpty(worldRecord.id))
            {
                pipelineManager.AssignId();
                worldRecord.id = pipelineManager.blueprintId;
            }

            string blueprintId = worldRecord.id;
            int version = Mathf.Max(1, worldRecord.version + 1);
            PrepareVRCPathForS3(abPath, blueprintId, version, ApiWorld.VERSION);

            if (!string.IsNullOrEmpty(pluginPath) && System.IO.File.Exists(pluginPath))
            {
                Debug.Log("Found plugin path. Preparing to upload!");
                PreparePluginPathForS3(pluginPath, blueprintId, version, ApiWorld.VERSION);
            }
            else
            {
                Debug.Log("Did not find plugin path. No upload occuring!");
            }

            if (!string.IsNullOrEmpty(unityPackagePath) && System.IO.File.Exists(unityPackagePath))
            {
                Debug.Log("Found unity package path. Preparing to upload!");
                PrepareUnityPackageForS3(unityPackagePath, blueprintId, version, ApiWorld.VERSION);
            }

            StartCoroutine(UploadNew());
        }

        IEnumerator UploadNew()
        {
            bool caughtInvalidInput = false;
            if (!ValidateNameInput(blueprintName))
                caughtInvalidInput = true;

            if (caughtInvalidInput)
                yield break;

            // upload unity package
            if (!string.IsNullOrEmpty(uploadUnityPackagePath))
            {
                yield return StartCoroutine(UploadFile(uploadUnityPackagePath, isUpdate ? worldRecord.unityPackageUrl : "", GetFriendlyWorldFileName("Unity package"), "Unity package",
                    delegate (string fileUrl)
                    {
                        cloudFrontUnityPackageUrl = fileUrl;
                    }
                ));
            }

            // upload plugin
            if (!string.IsNullOrEmpty(uploadPluginPath))
            {
                yield return StartCoroutine(UploadFile(uploadPluginPath, isUpdate ? worldRecord.pluginUrl : "", GetFriendlyWorldFileName("Plugin"), "Plugin",
                    delegate (string fileUrl)
                    {
                        cloudFrontPluginUrl = fileUrl;
                    }
                ));
            }

            // upload asset bundle
            if (!string.IsNullOrEmpty(uploadVrcPath))
            {
                yield return StartCoroutine(UploadFile(uploadVrcPath, isUpdate ? worldRecord.assetUrl : "", GetFriendlyWorldFileName("Asset bundle"), "Asset bundle",
                    delegate (string fileUrl)
                    {
                        cloudFrontAssetUrl = fileUrl;
                    }
                ));
            }

            if (isUpdate)
                yield return StartCoroutine(UpdateBlueprint());
            else
                yield return StartCoroutine(CreateBlueprint());

            OnSDKPipelineComplete();
        }

        private string GetFriendlyWorldFileName(string type)
        {
            return "World - " + blueprintName.text + " - " + type + " - " + Application.unityVersion + "_" + ApiWorld.VERSION.ApiVersion +
                   "_" + API.GetAssetPlatformString() + "_" + API.GetServerEnvironmentForApiUrl();
        }

        List<string> BuildTags()
        {
            var tags = new List<string>();
            if (contentSex.isOn)
                tags.Add("content_sex");
            if (contentViolence.isOn)
                tags.Add("content_violence");
            if (contentGore.isOn)
                tags.Add("content_gore");
            if (contentOther.isOn)
                tags.Add("content_other");

            if (APIUser.CurrentUser.hasSuperPowers)
            {
                if (contentFeatured.isOn)
                    tags.Add("content_featured");
                if (contentSDKExample.isOn)
                    tags.Add("content_sdk_example");
            }

            // "show in worlds menu"
            if (APIUser.CurrentUser.hasSuperPowers)
            {
                if (!showInActiveWorlds.isOn)
                    tags.Add("admin_hide_active");
                if (!showInPopularWorlds.isOn)
                    tags.Add("admin_hide_popular");
                if (!showInNewWorlds.isOn)
                    tags.Add("admin_hide_new");
            }

            return tags;
        }

        protected override IEnumerator CreateBlueprint()
        {
            yield return StartCoroutine(UpdateImage(isUpdate ? worldRecord.imageUrl : "", GetFriendlyWorldFileName("Image")));

            SetUploadProgress("Saving Blueprint to user", "Almost finished!!", 0.0f);
            ApiWorld world = new ApiWorld
            {
                id = worldRecord.id,
                authorName = pipelineManager.user.displayName,
                authorId = pipelineManager.user.id,
                name = blueprintName.text,
                imageUrl = cloudFrontImageUrl,
                assetUrl = cloudFrontAssetUrl,
                unityPackageUrl = cloudFrontUnityPackageUrl,
                pluginUrl = cloudFrontPluginUrl,
                description = blueprintDescription.text,
                tags = BuildTags(),
                releaseStatus = (releasePublic.isOn) ? ("public") : ("private"),
                capacity = System.Convert.ToInt16(worldCapacity.text),
                occupants = 0,
                shouldAddToAuthor = true
            };

            if (APIUser.CurrentUser.hasScriptingAccess && UnityEditor.EditorPrefs.HasKey("pluginNamespace"))
                world.scriptNamespace = UnityEditor.EditorPrefs.GetString("pluginNamespace");

            if (APIUser.CurrentUser.hasSuperPowers)
                world.isCurated = contentFeatured.isOn || contentSDKExample.isOn;
            else
                world.isCurated = false;

            bool doneUploading = false;
            world.Post(
                (c) =>
                {
                    ApiWorld savedBP = (ApiWorld)c.Model;
                    pipelineManager.blueprintId = savedBP.id;
                    UnityEditor.EditorPrefs.SetString("blueprintID-" + pipelineManager.GetInstanceID().ToString(), savedBP.id);
                    Debug.Log("Setting blueprintID on pipeline manager and editor prefs");
                    doneUploading = true;
                },
                (c) => { doneUploading = true; Debug.LogError(c.Error); });

            while (!doneUploading)
                yield return null;
        }

        protected override IEnumerator UpdateBlueprint()
        {
            bool doneUploading = false;

            worldRecord.name = blueprintName.text;
            worldRecord.description = blueprintDescription.text;
            worldRecord.capacity = System.Convert.ToInt16(worldCapacity.text);
            worldRecord.assetUrl = cloudFrontAssetUrl;
            worldRecord.pluginUrl = cloudFrontPluginUrl;
            worldRecord.tags = BuildTags();
            worldRecord.releaseStatus = (releasePublic.isOn) ? ("public") : ("private");
            worldRecord.unityPackageUrl = cloudFrontUnityPackageUrl;
            worldRecord.isCurated = contentFeatured.isOn || contentSDKExample.isOn;
            if (UnityEditor.EditorPrefs.HasKey("pluginNamespace"))
                worldRecord.scriptNamespace = UnityEditor.EditorPrefs.GetString("pluginNamespace");

            if (shouldUpdateImageToggle.isOn)
            {
                yield return StartCoroutine(UpdateImage(isUpdate ? worldRecord.imageUrl : "", GetFriendlyWorldFileName("Image")));

                worldRecord.imageUrl = cloudFrontImageUrl;
            }

            SetUploadProgress("Saving Blueprint", "Almost finished!!", 0.0f);
            worldRecord.Save((c) => doneUploading = true, (c) => { doneUploading = true; Debug.LogError(c.Error); });

            while (!doneUploading)
                yield return null;
        }

        void ToggleUpdateImage(bool isOn)
        {
            if (isOn)
            {
                bpImage.enabled = false;
                liveBpImage.enabled = true;
            }
            else
            {
                bpImage.enabled = true;
                liveBpImage.enabled = false;
                ImageDownloader.DownloadImage(worldRecord.imageUrl, delegate (Texture2D obj) {
                    bpImage.texture = obj;
                });
            }
        }

        void OnDestroy()
        {
            UnityEditor.EditorUtility.ClearProgressBar();
            UnityEditor.EditorPrefs.DeleteKey("currentBuildingAssetBundlePath");
            UnityEditor.EditorPrefs.DeleteKey("externalPluginPath");
        }
#endif
    }
}


