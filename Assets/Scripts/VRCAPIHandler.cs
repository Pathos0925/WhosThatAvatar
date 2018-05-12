using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class VRCAPIHandler : MonoBehaviour {

    //hardcoded apikey. Consider getting this from VRChats API instead
    public static string APIKEY = "JlE5Jldo5Jibnk5O5hTx6XVqsJu4WJ26";

    // We cannot use VRChats native API as it doesn't have the required CORS headers. We have to use a CORS proxy.
    //"To access cross-domain WWW resources in WebGL, the server you are trying to access needs to authorize this using CORS."
    //See: https://docs.unity3d.com/Manual/webgl-networking.html
    
    public static string VRCHAT_API = "https://api.vrchat.cloud/api/1/";

    public static string PROXY_API = "https://li4f8b2mw7.execute-api.us-east-2.amazonaws.com/CORS-proxy-json/";
    public static string PROXY_FILE_API = "https://fx5zpl3dmh.execute-api.us-east-2.amazonaws.com/CORS-proxy-bin/"; //returns only binary data. capped at around 4 to 6 MB.
   
    public static IEnumerator GetAvatarInfo(string avatarId, Action<AvatarInfo> response, Action<string> onError = null)
    {        
        try
        {
            var url = CheckProxyUrl(VRCHAT_API) + "avatars/" + avatarId + "?apiKey=" + APIKEY;

            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                yield return www.Send();

                if (www.isError)
                {
                    Debug.Log("Response Error: " + www.error);
                    if (onError != null)
                        onError(www.error);
                }
                else
                {
                    Debug.Log("Response: " + www.downloadHandler.text);                    
                    response(JsonUtility.FromJson<AvatarInfo>(www.downloadHandler.text));
                }
            }           
        }
        finally
        {

        }            
    }
    public static string CheckProxyUrl(string url, bool forceFile = false)
    {
        //force cors proxy if in WebGL, otherwise just use the native api
#if UNITY_WEBGL && !UNITY_EDITOR
        if (url.ToUpper().Contains("FILE") || forceFile)
        {
            return url.Replace(VRCHAT_API, PROXY_FILE_API);
        }
        else
        {
            return url.Replace(VRCHAT_API, PROXY_API);
        }
#else
        return url;
#endif
    }

    public static IEnumerator GetAvatarsList(Action<List<AvatarListItem>> response, int offset = 0, string search = "", string order = "", string sort = "", Action<string> onError = null)
    {
        try
        {
            var url = CheckProxyUrl(PROXY_API) + "avatars?";
            url += "apiKey=" + APIKEY;
            if (!string.IsNullOrEmpty(order))
                url += "&order=" + order;
            if (!string.IsNullOrEmpty(sort))
                url += "&sort=" + sort;
            if (!string.IsNullOrEmpty(search))
                url += "&search=" + search;

            url += "&offset=" + offset.ToString();

            Debug.Log("Get Avatar List. URL: " + url);

            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                yield return www.Send();

                if (www.isError)
                {
                    Debug.Log("Response Error: " + www.error);
                    if (onError != null)
                        onError(www.error);
                }
                else
                {
                    Debug.Log("Response: " + www.downloadHandler.text);
                    response(Newtonsoft.Json.JsonConvert.DeserializeObject<List<AvatarListItem>>(www.downloadHandler.text));
                }
            }
        }
        finally
        {

        }
    }

    [Serializable]
    public class AvatarInfo
    {
        public string id;
        public string name;
        public string description;
        public string authorId;
        public string authorName;
        public List<string> tags;

        public string assetUrl;
        public string imageUrl;
        public string thumbnailImageUrl;
        public string releaseStatus;
        public int version;
        public bool featured;
        public List<UnityPackage> unityPackages;

        public bool unityPackageUpdated;
        public string unityPackageUrl;
    }
    [Serializable]
    public class UnityPackage
    {
        public string id;
        public string assetUrl;
        public string unityVersion;
        public int unitySortNumber;
        public int assetVersion;
        public string platform;
        public string created_at;
    }
    [Serializable]
    public class AvatarListItem
    {
        public string id;
        public string name;
        public string description;
        public string authorId;
        public string authorName;
        public List<string> tags;
        public string assetUrl;
        public string imageUrl;
        public string thumbnailImageUrl;
        public string releaseStatus;
        public int version;
        public bool featured;
        public List<UnityPackage> unityPackages;
        public bool unityPackageUpdated;
        public string unityPackageUrl;
    }
}
