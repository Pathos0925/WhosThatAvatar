using UnityEngine;
using UnityEditor;
using VRC.Core;

namespace VRC
{
    [ExecuteInEditMode]
    public class AccountEditorWindow : EditorWindow 
    {
        static bool isInitialized = false;
        static string clientInstallPath;
        static bool signingIn = false;
        static string error = null;

        static AccountEditorWindow window = null;

        public static bool FutureProofPublishEnabled { get { return UnityEditor.EditorPrefs.GetBool("futureProofPublish", DefaultFutureProofPublishEnabled); } }
        public static bool DefaultFutureProofPublishEnabled { get { return !SDKClientUtilities.IsInternalSDK();  } }

        void Update()
        {
            SignIn(false);
        }

        static string storedUsername
        {
            get
            {
                if (EditorPrefs.HasKey("sdk#username"))
                    return EditorPrefs.GetString("sdk#username");
                return null;
            }
            set
            {
                EditorPrefs.SetString("sdk#username", value);
                if (string.IsNullOrEmpty(value))
                    EditorPrefs.DeleteKey("sdk#username");
            }
        }

        static string storedPassword
        {
            get
            {
                if (EditorPrefs.HasKey("sdk#password"))
                    return EditorPrefs.GetString("sdk#password");
                return null;
            }
            set
            {
                EditorPrefs.SetString("sdk#password", value);
                if (string.IsNullOrEmpty(value))
                    EditorPrefs.DeleteKey("sdk#password");
            }
        }

        static string _username = null;
        static string _password = null;

        static string username
        {
            get
            {
                if (!string.IsNullOrEmpty(_username))
                    return _username;
                else
                    _username = storedUsername;
                return _username;
            }
            set
            {
                _username = value;
            }
        }

        static string password
        {
            get
            {
                if (!string.IsNullOrEmpty(_password))
                    return _password;
                else
                    _password = storedPassword;
                return _password;
            }
            set
            {
                _password = value;
            }
        }

        static ApiServerEnvironment serverEnvironment
        {
            get
            {
                ApiServerEnvironment env = ApiServerEnvironment.Release;
                try
                {
                    env = (ApiServerEnvironment)System.Enum.Parse(typeof(ApiServerEnvironment), UnityEditor.EditorPrefs.GetString("VRC_ApiServerEnvironment", env.ToString()));
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Invalid server environment name - " + e.ToString());
                }

                return env;
            }
            set
            {
                UnityEditor.EditorPrefs.SetString("VRC_ApiServerEnvironment", value.ToString());

                API.SetApiUrlFromEnvironment(value);
            }
        }

        public static void RefreshApiUrlSetting()
        {
            // this forces the static api url variable to be reset from the server environment set in editor prefs.
            // needed because the static variable states get cleared when entering / exiting play mode
            ApiServerEnvironment env = serverEnvironment;
            serverEnvironment = env;
        }

        [MenuItem("VRChat SDK/Settings")]
        public static void CreateWindow()
        {
            if (!RemoteConfig.IsInitialized())
            {
                VRC.Core.API.SetOnlineMode(true, "vrchat");
                RemoteConfig.Init(() => CreateWindow());
                return;
            }

            Init();
            window = EditorWindow.GetWindow<AccountEditorWindow>("VRChat Settings");
            window.Show();
        }

        public static void Init () 
        {
            if (isInitialized)
                return;

			if(!APIUser.IsLoggedInWithCredentials && ApiCredentials.Load() )
				APIUser.Login((user) => AnalyticsSDK.LoggedInUserChanged(user), null );

            clientInstallPath = SDKClientUtilities.GetSavedVRCInstallPath();
            if(string.IsNullOrEmpty(clientInstallPath))
                clientInstallPath = SDKClientUtilities.LoadRegistryVRCInstallPath();

            signingIn = false;
			isInitialized = true;

            VRCContentManagerWindow.ClearContent();
        }

        [UnityEditor.Callbacks.DidReloadScripts(int.MaxValue)]
        static void DidReloadScripts()
        {
            RefreshApiUrlSetting();
        }

        static void OnVRCInstallPathGUI()
        {
            EditorGUILayout.LabelField("VRChat Client", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Installed Client Path: ", clientInstallPath);
            if(GUILayout.Button("Edit"))
            {
                string initPath = "";
                if(!string.IsNullOrEmpty(clientInstallPath))
                    initPath = clientInstallPath;

                clientInstallPath = EditorUtility.OpenFilePanel("Choose VRC Client Exe", initPath, "exe");
                SDKClientUtilities.SetVRCInstallPath(clientInstallPath);
            }
            if(GUILayout.Button("Revert to Default"))
            {
                clientInstallPath = SDKClientUtilities.LoadRegistryVRCInstallPath();
            }

        }

        public static bool OnShowStatus()
        {
            API.SetOnlineMode(true);

            SignIn(false);

            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField(Status);

            if (APIUser.IsLoggedInWithCredentials)
            {
                OnCreatorStatusGUI();
            }

            EditorGUILayout.EndVertical();

            return APIUser.IsLoggedInWithCredentials;
        }

        static bool OnAccountGUI()
        {
            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField(Status);

            EditorGUILayout.LabelField("Account", EditorStyles.boldLabel);

            if (signingIn)
            {
                EditorGUILayout.LabelField("Signing in.");
                EditorGUILayout.EndVertical();
                return false;
            }
            else if (APIUser.IsLoggedInWithCredentials)
            {
                OnCreatorStatusGUI();

                if (GUILayout.Button("Logout"))
                {
                    storedUsername = username = null;
                    storedPassword = password = null;

                    APIUser.Logout();
                    VRCContentManagerWindow.ClearContent();
                }
            }
            else
            {
                if (signingIn)
                    EditorGUILayout.LabelField("Signing in.");
                else
                {
                    Init();
                    
                    username = EditorGUILayout.TextField("Username", username);
                    password = EditorGUILayout.PasswordField("Password", password);

                    if (GUILayout.Button("Sign In"))
                        SignIn(true);
                    if (GUILayout.Button("Sign up"))
                        Application.OpenURL("http://vrchat.com/register");
                }
            }

            {
                if (APIUser.CurrentUser == null || APIUser.CurrentUser.hasSuperPowers)
                {
                    EditorGUILayout.LabelField("API", EditorStyles.boldLabel);

                    ApiServerEnvironment newEnv = (ApiServerEnvironment)EditorGUILayout.EnumPopup("Use API", serverEnvironment);
                    if (serverEnvironment != newEnv)
                    {
                        serverEnvironment = newEnv;
                    }

                    if (APIUser.CurrentUser == null)
                    {
                        EditorGUILayout.EndVertical();
                        return false;
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("API", EditorStyles.boldLabel);

                    ApiServerEnvironment newEnv = (EditorGUILayout.Popup("Use API", serverEnvironment != ApiServerEnvironment.Beta ? 1 : 0, new string[] { "Beta", "Release" }) == 0 ? ApiServerEnvironment.Beta : ApiServerEnvironment.Release);
                    if (serverEnvironment != newEnv)
                    {
                        serverEnvironment = newEnv;
                    }
                }
            }

            // Future proof upload
            {
                EditorGUILayout.LabelField("Publish", EditorStyles.boldLabel);
                bool futureProofPublish = UnityEditor.EditorPrefs.GetBool("futureProofPublish", DefaultFutureProofPublishEnabled);

                futureProofPublish = EditorGUILayout.Toggle("Future Proof Publish", futureProofPublish);

                if (UnityEditor.EditorPrefs.GetBool("futureProofPublish", DefaultFutureProofPublishEnabled) != futureProofPublish)
                {
                    UnityEditor.EditorPrefs.SetBool("futureProofPublish", futureProofPublish);
                }
            }

            if (APIUser.CurrentUser != null)
            {
                // custom vrchat install location
                OnVRCInstallPathGUI();
            }

            EditorGUILayout.EndVertical();

            return true;
        }

        static void OnCreatorStatusGUI()
        {
            EditorGUILayout.LabelField("Logged in as:", APIUser.CurrentUser.displayName);

            if (SDKClientUtilities.IsInternalSDK())
                EditorGUILayout.LabelField("Developer Status: ", APIUser.CurrentUser.developerType.ToString());

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("World Creator Status: ", APIUser.CurrentUser.canPublishWorlds ? "Allowed to publish worlds" : "Not yet allowed to publish worlds");
            EditorGUILayout.LabelField("Avatar Creator Status: ", APIUser.CurrentUser.canPublishAvatars ? "Allowed to publish avatars" : "Not yet allowed to publish avatars");
            EditorGUILayout.EndVertical();

            if (!APIUser.CurrentUser.canPublishAllContent)
            {
                if (GUILayout.Button("More Info..."))
                {
                    VRC_SdkControlPanel.ShowContentPublishPermissionsDialog(); 
                }
            }
            

            EditorGUILayout.EndHorizontal();
        }

        void OnGUI ()
        {
            if (VRC.Core.RemoteConfig.IsInitialized())
            {
                EditorGUILayout.LabelField("Unity Version", EditorStyles.boldLabel);
                if (VRC.Core.RemoteConfig.HasKey("sdkUnityVersion"))
                {
                    string sdkUnityVersion = VRC.Core.RemoteConfig.GetString("sdkUnityVersion");
                    if (string.IsNullOrEmpty(sdkUnityVersion))
                        EditorGUILayout.LabelField("Could not fetch remote config.");
                    else if (Application.unityVersion != sdkUnityVersion)
                        EditorGUILayout.LabelField("Wrong Unity version. Please use " + sdkUnityVersion);
                    else
                        EditorGUILayout.LabelField("You are using the correct Unity version: " + sdkUnityVersion);
                }
            }
            else
            {
                VRC.Core.API.SetOnlineMode(true, "vrchat");
                VRC.Core.RemoteConfig.Init();
            }

            OnAccountGUI();
        }

        private static string Status
        {
            get
            {
                if (!APIUser.IsLoggedInWithCredentials)
                    return error == null ? "Use the settings menu to log in." : "Error in authenticating: " + error;
                if (signingIn)  
                    return "Logging in.";
                else
                    return "Connected to " + serverEnvironment.ToString();
            }
        }

        private static object syncObject = new object();
        private static void SignIn(bool explicitAttempt)
        {
            lock (syncObject)
            {
                if (signingIn
                    || APIUser.IsLoggedInWithCredentials
                    || (!explicitAttempt && string.IsNullOrEmpty(storedUsername))
                    || (!explicitAttempt && string.IsNullOrEmpty(storedPassword)))
                    return;

                signingIn = true;
            }

            Init();

            ApiCredentials.Clear();
            ApiCredentials.SetUser(username, password);
            APIUser.Login(
                delegate (APIUser user)
                {
                    signingIn = false;
                    error = null;
                    storedUsername = username;
                    storedPassword = password;
                    AnalyticsSDK.LoggedInUserChanged(user);

                    if (!APIUser.CurrentUser.canPublishAllContent)
                    {
                        if (UnityEditor.SessionState.GetString("HasShownContentPublishPermissionsDialogForUser", "") != user.id)
                        {
                            UnityEditor.SessionState.SetString("HasShownContentPublishPermissionsDialogForUser", user.id);
                            VRC_SdkControlPanel.ShowContentPublishPermissionsDialog();
                        } 
                    }
                },
                delegate (string message)
                {
                    signingIn = false;
                    storedUsername = null;
                    storedPassword = null;
                    error = message;
                    APIUser.Logout();
                    VRC.Core.Logger.Log("Error logging in: " + message);
                }
            );
        }

        private void OnDestroy()
        {
            signingIn = false;
            isInitialized = false;
        }
    }
}
