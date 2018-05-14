using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class CacheAPIHandler : MonoBehaviour
{
    public static string CACHE_API = "https://1clvpmsyk5.execute-api.us-east-2.amazonaws.com/vrcCache1/";


    public static IEnumerator GetCachedAvatars(Action<CachedAvatarResponse> response, string search = "", string lastKey = "", Action<string> onError = null)
    {
        try
        {
            var url = CACHE_API + "avatars?";

            if (!string.IsNullOrEmpty(search))
                url += "&search=" + search;
            if (!string.IsNullOrEmpty(lastKey))
                url += "&lastKey=" + lastKey;
            

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
                    response(Newtonsoft.Json.JsonConvert.DeserializeObject<CachedAvatarResponse>(www.downloadHandler.text));
                }
            }
        }
        finally
        {

        }
    }

    public static List<CachedAvatar> AvatarListToCachedList(List<VRCAPIHandler.AvatarListItem> list)
    {
        var newList = new List<CachedAvatar>();
        //no linq for compatability
        for (int i = 0; i < list.Count; i++)
        {
            var cachedAvatar = new CachedAvatar();
            cachedAvatar.assetUrl = list[i].assetUrl;
            cachedAvatar.authorId = list[i].authorId;
            cachedAvatar.authorName = list[i].authorName;
            cachedAvatar.description = list[i].description;
            cachedAvatar.featured = list[i].featured;
            cachedAvatar.id = list[i].id;
            cachedAvatar.imageUrl = list[i].imageUrl;
            cachedAvatar.name = list[i].name;
            cachedAvatar.releaseStatus = list[i].releaseStatus;
            cachedAvatar.thumbnailImageUrl = list[i].thumbnailImageUrl;
            cachedAvatar.unityPackageUpdated = list[i].unityPackageUpdated;
            cachedAvatar.version = list[i].version;
            newList.Add(cachedAvatar);
        }
        return newList;
    }


    [Serializable]
    public class CachedAvatarResponse
    {
        public List<CachedAvatar> Items;
        public int Count;
        public int ScannedCount;
        public LastEvaluatedKey LastEvaluatedKey;
        public List<VRCAPIHandler.AvatarListItem> ItemsToAvatarListItems()
        {
            var items = new List<VRCAPIHandler.AvatarListItem>();
            for(int i = 0; i < Items.Count; i++)
            {
                items.Add(Items[i].toListItem());
            }
            return items;
        }
    }

    [Serializable]
    public class LastEvaluatedKey
    {
        public string id;
    }

    [Serializable]
    public class CachedAvatar
    {
        public string id;
        public string name;
        public string description;
        public string authorId;
        public string authorName;
        public string assetUrl;
        public string imageUrl;
        public string thumbnailImageUrl;
        public string releaseStatus;
        public int version;
        public bool featured;
        public bool unityPackageUpdated;

        public worldsFound worldsFound;

        public VRCAPIHandler.AvatarListItem toListItem()
        {
            var newItem = new VRCAPIHandler.AvatarListItem();
            newItem.assetUrl = assetUrl;
            newItem.authorId = authorId;
            newItem.authorName = authorName;
            newItem.description = description;
            newItem.featured = featured;
            newItem.id = id;
            newItem.imageUrl = imageUrl;
            newItem.name = name;
            newItem.releaseStatus = releaseStatus;
            newItem.thumbnailImageUrl = thumbnailImageUrl;
            newItem.unityPackageUpdated = unityPackageUpdated;
            newItem.version = version;
            return newItem;
        }

        //public int listDateAdded;
        //public int LastModified;
    }
    [Serializable]
    public class worldsFound
    {
        public string datatype; //String Set
        public Dictionary<string, string> contents; //key and value seem to be the same thing.
        public override string ToString()
        {
            var toReturn = "";
            foreach (KeyValuePair<string, string> entry in contents)
            {
                toReturn += entry.Value + "\r\n";
            }
            return toReturn;
        }
    }

}
