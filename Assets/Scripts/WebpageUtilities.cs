using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class WebpageUtilities : MonoBehaviour
{
    //Querystring utilities
    [DllImport("__Internal")]
    private static extern string GetParam();

    [DllImport("__Internal")]
    private static extern void SetParam(string str);

    [DllImport("__Internal")]
    private static extern void SetParamReplace(string str);

    //external textbox to allow copy and pasting avatar ids
    [DllImport("__Internal")]
    private static extern void ShowOverlay(string exportedGameSave);

    [DllImport("__Internal")]
    private static extern void HideOverlay();

    public static string GetURLParameters()
    {
        try
        {
            return GetParam();
        }
        catch
        {
            Debug.Log("Could not get GetURLParameters");
            return "";
        }
    }
    public static void SetURLParameters(string newParameters, bool replace = false)
    {
        if (replace)
        {
            try
            {
                SetParamReplace(newParameters);
            }
            catch
            {

            }
        }
        else
        {
            try
            {
                SetParam(newParameters);
            }
            catch
            {

            }
        }
        
    }

    public static void ShowImportExportOverlay(string gameSave)
    {
        ShowOverlay(gameSave);
        ToggleCaptureAllInput(false);
    }

    public static void ImportFromWebOverlay(string importStr)
    {
        HideOverlay();
        UIManager.instance.SetLoadedAvatarId(importStr);
        //LoadGameFromImportedString(importStr);
        ToggleCaptureAllInput(true);
    }

    public static void CloseOverlay(string param)
    {
        HideOverlay();
        ToggleCaptureAllInput(true);
    }

    public static void ToggleCaptureAllInput(bool captureAll)
    {
#if !UNITY_EDITOR && UNITY_WEBGL
        WebGLInput.captureAllKeyboardInput = captureAll;
#endif
    }

}
