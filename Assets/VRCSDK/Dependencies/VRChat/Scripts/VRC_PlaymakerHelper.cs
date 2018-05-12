#if PLAYMAKER
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HutongGames.PlayMaker.Actions;

namespace VRCSDK2
{
    public class VRC_PlaymakerHelper : MonoBehaviour
    {
        static string logString = "";
        public static string GetErrors()
        {
            return logString;
        }

        public static bool ValidatePlaymaker( GameObject root = null, bool runtime = false )
        {
            logString = ""; 
            bool result = true;

            PlayMakerFSM[] fsms;
            if (root != null)
                fsms = root.GetComponentsInChildren<PlayMakerFSM>();
            else
                fsms = Resources.FindObjectsOfTypeAll<PlayMakerFSM>();
            foreach (var fsm in fsms)
            {
                logString += "Playmaker FSM Analysis - " + fsm.gameObject.name + " - " + fsm.name + System.Environment.NewLine;
                if (AnalyzePlaymakerFSM(fsm) == false)
                {
                    if (runtime)
                        Destroy(fsm);
                    result = false;
                }
            }

            EnforcePlaymakerGuiSettings();
            WarnLimitedPlaymakerActions(root);

            return result;
        }

        static bool AnalyzePlaymakerFSM(PlayMakerFSM pm)
        {
            bool result = true;

            if (pm.Fsm.Initialized == false)
                return true;

            foreach (var e in pm.Fsm.Events)
            {
                if (ValidatePlaymakerEvent(e.Name) == false)
                    result = false;
            }

            foreach (var s in pm.Fsm.States)
            {
                foreach (var a in s.Actions)
                {
                    string name = a.GetType().ToString();
                    if (ValidatePlaymakerAction(name) == false)
                        result = false;
                }
            }
            return result;
        }

        static bool ValidatePlaymakerAction(string typeName)
        {
            var names = typeName.Split('.');
            string name = names[names.Length - 1];

            // this is the blacklist
            switch (name)
            {
                case "AddComponent":
                case "AddScript":
                case "ApplicationQuit":
                case "ApplicationRunInBackground":
                case "SetGameVolume":
                case "CameraFadeIn":
                case "CameraFadeOut":
                case "ControllerMove":
                case "ControllerSettings":
                case "ControllerSimpleMove":
                case "CutToCamera":
                case "DestroyComponent":
                case "DeviceOrientationEvent":
                case "DevicePlayFullScreenMovie":
                case "DeviceShakeEvent":
                case "DeviceVibrate":
                case "DontDestroyOnLoad":
                case "DrawFullscreenColor":
                case "FindGameObject":
                case "FindClosest":
                case "GetButton":
                case "GetButtonDown":
                case "GetButtonUp":
                case "GetComponent":
                case "SetObjectValue":
                case "SetProperty":
                case "GetDeviceAcceleration":
                case "GetDeviceRoll":
                case "GetIPhoneSettings":
                case "AnyKey":
                case "AxisEvent":
                case "GetAxis":
                case "GetAxisVector":
                case "GetKey":
                case "GetKeyDown":
                case "GetKeyUp":
                case "GetLocationInfo":
                case "GetMainCamera":
                case "GetMouseButton":
                case "GetMouseButtonDown":
                case "GetMouseButtonUp":
                case "GetMouseX":
                case "GetMouseY":
                case "GetParent":
                case "GetProperty":
                case "GetRandomObject":
                case "GetRoot":
                case "GetScreenHeight":
                case "GetScreenWidth":
                case "GetTouchCount":
                case "GetTouchInfo":
                case "GUIAction":
                case "GUIBox":
                case "GUIButton":
                case "GUIHorizontalSlider":
                case "GUILabel":
                case "GUITooltip":
                case "GUIVerticalSlider":
                case "GUIElementHitTest":
                case "GUILayoutAction":
                case "GUILayoutBeginArea":
                case "GUILayoutBeginAreaFollowObject":
                case "GUILayoutBeginCentered":
                case "GUILayoutBeginHorizontal":
                case "GUILayoutBeginScrollView":
                case "GUILayoutBeginVertical":
                case "GUILayoutBox":
                case "GUILayoutButton":
                case "GUILayoutConfirmPasswordField":
                case "GUILayoutEmailField":
                case "GUILayoutEndArea":
                case "GUILayoutEndCentered":
                case "GUILayoutEndHorizontal":
                case "GUILayoutEndScrollView":
                case "GUILayoutEndVertical":
                case "GUILayoutFlexibleSpace":
                case "GUILayoutFloatField":
                case "GUILayoutFloatLabel":
                case "GUILayoutHorizontalSlider":
                case "GUILayoutIntField":
                case "GUILayoutIntLabel":
                case "GUILayoutLabel":
                case "GUILayoutPasswordField":
                case "GUILayoutRepeatButton":
                case "GUILayoutSpace":
                case "GUILayoutTextField":
                case "GUILayoutTextLabel":
                case "GUILayoutToggle":
                case "GUILayoutToolbar":
                case "GUILayoutVerticalSlider":
                case "UGUICanvasForceUpdateCanvases":
                case "UGUIGetSelectedGameObject":
                case "ResetGUIMatrix":
                case "RotateGUI":
                case "ScaleGUI":
                case "EnableGUI":
                case "DrawTexture":
                case "HasComponent":
                case "InvokeMethod":
                case "CallMethod":
                case "CallStaticMethod":
                case "EnableBehaviour":
                case "LoadLevel":
                case "LoadLevelNum":
                case "MouseLook":
                case "MouseLook2":
                case "MousePick":
                case "MousePickEvent":
                case "MousePick2d":
                case "MousePick2dEvent":
                case "ScreenPick":
                case "ScreenPick2d":
                case "ResetInputAxes":
                case "TransformInputToWorldSpace":
                case "PlayerPrefsDeleteAll":
                case "PlayerPrefsDeleteKey":
                case "PlayerPrefsGetFloat":
                case "PlayerPrefsGetInt":
                case "PlayerPrefsGetString":
                case "PlayerPrefsHasKey":
                case "PlayerPrefsSetFloat":
                case "PlayerPrefsSetInt":
                case "PlayerPrefsSetString":
                case "RestartLevel":
                case "AllowSceneActivation":
                case "CreateScene":
                case "GetSceneActivateChangedEventData":
                case "GetSceneBuildIndex":
                case "GetSceneCount":
                case "GetSceneCountInBuildSettings":
                case "GetSceneIsDirty":
                case "GetSceneIsLoaded":
                case "GetSceneIsValid":
                case "GetSceneLoadedEventData":
                case "GetSceneName":
                case "GetScenePath":
                case "GetSceneProperties":
                case "GetSceneRootCount":
                case "GetSceneRootGameObjects":
                case "GetSceneUnloadedEventData":
                case "LoadScene":
                case "LoadSceneAsync":
                case "MergeScenes":
                case "MoveGameObjectToScene":
                case "SendActiveSceneChangedEvent":
                case "SendSceneLoadedEvent":
                case "GetSceneActionBase":
                case "SendSceneUnloadedEvent":
                case "SetActiveScene":
                case "UnloadScene":
                case "UnloadSceneAsync":
                case "SendMessage":
                case "StartCoroutine":
                case "SetGUIAlpha":
                case "SetGUIBackgroundColor":
                case "SetGUIColor":
                case "SetGUIContentColor":
                case "SetGUIDepth":
                case "SetGUISkin":
                case "SetGUIText":
                case "SetGUITexture":
                case "SetGUITextureAlpha":
                case "SetGUITextureAlphaColor":
                case "SetMaterialMovieTexture":
                case "MovieTextureAudioSettings":
                case "PauseMovieTexture":
                case "PlayMovieTexture":
                case "StopMovieTexture":
                case "SetMainCamera":
                case "SetMouseCursor":
                case "StartLocationServiceUpdates":
                case "StopLocationServiceUpdates":
                case "SwipeGestureEvent":
                case "TakeScreenshot":
                case "TouchEvent":
                case "TouchGUIEvent":
                case "TouchObjectEvent":
                case "TouchObject2dEvent":
                case "ProjectLocationToMap":
                case "UseGUILayout":
                case "Explosion":
                case "GetCollisionInfo":
                case "GetParticleCollisionInfo":
                case "GetRaycastAllInfo":
                case "GetRaycastHitInfo":
                case "Raycast":
                case "RaycastAll":
                case "TriggerEvent":
                case "CollisionEvent":
                case "WWWObject":
                    logString += "Playmaker Provided Action (" + name + ") is unsupported by VRChat." + System.Environment.NewLine;
                    return false;
                default:
                    return true;
            }
        }

        static bool ValidatePlaymakerEvent(string name)
        {
            switch (name)
            {
                case "LEVEL LOADED":
                case "MOUSE DOWN":
                case "MOUSE DRAG":
                case "MOUSE ENTER":
                case "MOUSE EXIT":
                case "MOUSE OVER":
                case "MOUSE UP":
                case "MOUSE UP AS BUTTON":
                case "CONTROLLER COLLIDER HIT":
                case "PLAYER CONNECTED":
                case "SERVER INITIALIZED":
                case "CONNECTED TO SERVER":
                case "PLAYER DISCONNECTED":
                case "DISCONNECTED FROM SERVER":
                case "FAILED TO CONNECT":
                case "FAILED TO CONNECT TO MASTER SERVER":
                case "MASTER SERVER EVENT":
                case "NETWORK INSTANTIATE":
                case "APPLICATION FOCUS":
                case "APPLICATION PAUSE":
                case "APPLICATION QUIT":
                    logString += "Playmaker System Event (" + name + ") is unsupported by VRChat." + System.Environment.NewLine;
                    return false;
                default:
                    return true;
            }
        }

        static void EnforcePlaymakerGuiSettings()
        {
            PlayMakerGUI[] guis;

            guis = Resources.FindObjectsOfTypeAll<PlayMakerGUI>();
            if (guis != null && guis.Length > 0)
            {
                guis[0].controlMouseCursor = false;
            }
        }

        static void WarnLimitedPlaymakerActions(GameObject root)
        {
            // warn about any limitations or restrictions on built-in playmaker actions here.
        }
    }
}
#endif