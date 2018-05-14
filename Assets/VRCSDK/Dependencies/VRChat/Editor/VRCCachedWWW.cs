using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

public static class VRCCachedWWW {
    public const float DefaultCacheTimeHours = 24 * 7;

    public static void ClearOld(float cacheLimitHours = DefaultCacheTimeHours)
    {
        string cacheDir = CacheDir;
        if (System.IO.Directory.Exists(cacheDir))
        {
            foreach (string fileName in System.IO.Directory.GetFiles(cacheDir))
            {
                if (GetAge(fileName) > cacheLimitHours)
                    System.IO.File.Delete(fileName);
            }
        }
    }

    private static string CacheDir
    {
        get
        {
            return Application.temporaryCachePath;
        }
    }

    public static IEnumerator Get(string url, System.Action<WWW> onDone, float cacheLimitHours = DefaultCacheTimeHours)
    {
        string cacheDir = CacheDir;
        if (!System.IO.Directory.Exists(cacheDir))
            System.IO.Directory.CreateDirectory(cacheDir);

        string hash = CreateHash(url);
        string cache = cacheDir + "/www_" + hash;
        string location = url;
        bool useCache = false;

        if (System.IO.File.Exists(cache))
        {
            if (GetAge(cache) > cacheLimitHours)
                System.IO.File.Delete(cache);
            else
            {
                location = "file://" + cache;
                useCache = true;
            }
        }
                
        while (true)
        {
            WWW target = new WWW(location);
            target.threadPriority = ThreadPriority.Low;

            while (!target.isDone)
                yield return null;

            if (!useCache)
            {
                if (System.IO.File.Exists(cache))
                    System.IO.File.Delete(cache);

                if (string.IsNullOrEmpty(target.error))
                    System.IO.File.WriteAllBytes(cache, target.bytes);

                onDone(target);
                break;
            }
            else
            {
                if (string.IsNullOrEmpty(target.error))
                {
                    onDone(target);
                    break;
                }
                else
                {
                    if (System.IO.File.Exists(cache))
                        System.IO.File.Delete(cache);

                    location = url;
                    useCache = false;
                }
            }
        }
    }

    private static string CreateHash(string _string)
    {
        SHA256 hash = SHA256.Create();
        byte[] computed_hash = hash.ComputeHash(System.Text.Encoding.Default.GetBytes(_string));
        return System.Uri.EscapeDataString(System.Convert.ToBase64String(computed_hash));
    }

    private static double GetAge(string file)
    {
        if (!System.IO.File.Exists(file))
            return 0;

        System.DateTime writeTime = System.IO.File.GetLastWriteTimeUtc(file);
        return System.DateTime.UtcNow.Subtract(writeTime).TotalHours;
    }
}
