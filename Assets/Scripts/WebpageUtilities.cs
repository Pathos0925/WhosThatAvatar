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

   

    public static string GetURLParameters()
    {
        try
        {
            string param = GetParam();
            Debug.Log("Found querystring avatar: " + param);
            return param;
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
    

}
