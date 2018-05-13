#define TEST_DECOMPRESS //comment this out in build. This is to test the WebGL decompressor.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using VRCAvatarAssetbundleDecompressor;

public class AvatarLoader : MonoBehaviour
{
    public static AvatarLoader instance;
    private AssetBundle loadedAssetBundle;
    private GameObject instantiatedAvatar;
    private MouseOrbitImproved mouseOrbit;
    bool abort = false;
    private byte[] buff;

    //Unity 5.5 and later does not support extracting an assetbundle compressed with LZMA in WebGL: https://blogs.unity3d.com/cn/2016/09/20/understanding-memory-in-unity-webgl/
    //we have to manually decompress and rebuild blocks and metadata. LZMA decompress block contents.    
    //also, appearently unity wont open assetbundles built for another version, even if is able to do so.
    //see Decompressor.Attempt for how this is done.

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(instance);
        }
        instance = this;
        mouseOrbit = GameObject.FindObjectOfType<MouseOrbitImproved>();
    }


    void Start ()
    {
        //LoadAvatar("avtr_88723b40-92a1-4d6c-b867-9072f027f680");

#if UNITY_WEBGL && !UNITY_EDITOR
        var avatarQuerystring = WebpageUtilities.GetURLParameters();
        if (!string.IsNullOrEmpty(avatarQuerystring))
        {
            LoadAvatar(avatarQuerystring);
        }
#endif       
       
    }

    void PrintOrReplaceShaders(GameObject go, bool shared = false, bool replace = false)
    {
        if (go)
        {
            Renderer[] componentsInChildren = go.GetComponentsInChildren<Renderer>(true);
            List<Renderer> list = new List<Renderer>();
            list.AddRange(componentsInChildren);
            Debug.Log(list.Count.ToString() + " total shaders");
            int replacedShaderCount = 0;
            for (int i = 0; i < list.Count; i++)
            {
                Material[] materials;
                if (!shared)
                    materials = list[i].materials;
                else
                    materials = list[i].sharedMaterials;

                for (int j = 0; j < materials.Length; j++)
                {
                    Material material = materials[j];
                    string name = material.shader.name;
                    if (replace)
                    {
                        var foundShader = Shader.Find(name);
                        replacedShaderCount++;
                        //check if the shader loaded from the assetbundle, if not use a proxy shader
                        if (foundShader != null)
                        {
                            material.shader = foundShader;
                        }
                        else
                        {
                            if (SettingsManager.instance.ReplacementShader.name.Contains("ProxyShaderNone"))
                            {
                                //will probably just be pink
                            }
                            else
                            {
                                material.shader = SettingsManager.instance.ReplacementShader;
                                Debug.Log("Shader " + name + " could not be found and was replaced with " + SettingsManager.instance.ReplacementShader.name);
                            }
                        }                        
                    }
                    else
                    {
                        Debug.Log("Found shader: " + name);
                    }
                }
            }
            if (replace)
                Debug.Log("Replaced " + replacedShaderCount.ToString() + " shaders.");
        }
    }
    void Update ()
    {
		if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (instantiatedAvatar)
                PrintOrReplaceShaders(instantiatedAvatar);
        }       
    }

    public void LoadAvatar(string avatarID)
    {
        //todo: Disable ui?
        //todo: cancel button
        UIManager.instance.SetMainTitle("Loading avatar", "Getting avatar info...");
        StartCoroutine(VRCAPIHandler.GetAvatarInfo(avatarID, (avatarInfo) =>
        {
            if (avatarInfo.releaseStatus != "public")
            {
                UIManager.instance.SetMainTitle(null, "This avatar is not public and cannot be viewed.");
                return;
            }

            UIManager.instance.SetMainTitle(avatarInfo.name, "By: " + avatarInfo.authorName);
            StartCoroutine(DownloadAvatar(VRCAPIHandler.CheckProxyUrl(avatarInfo.assetUrl), () =>
            {
                StartCoroutine(InstantiateAvatar(() =>
                {
                    UIManager.instance.SetMainTitle(avatarInfo.name, "By: " + avatarInfo.authorName);
                    UIManager.instance.SetLoadedAvatarId(avatarInfo.id);

                    GameObject mouseOrbitFollow = new GameObject("MouseOrbitFollow");
                    mouseOrbitFollow.transform.position = instantiatedAvatar.GetComponentInChildren<Renderer>().bounds.center;
                    mouseOrbit.target = mouseOrbitFollow.transform;
                    if (avatarInfo.name == null)
                        avatarInfo.name = "";
                    if (avatarInfo.authorName == null)
                        avatarInfo.authorName = "?";

                    WebpageUtilities.SetURLParameters(avatarInfo.id);
                    //todo: enable UI
                }));
            }));
        }, (error) =>
        {
            UIManager.instance.SetMainTitle(null, "Could not find that avatar");
        }));
    }

    public IEnumerator DownloadAvatar(string url, Action onDownloaded)
    {
        bool errorShown = false;
        Debug.Log("Downloading avatar from: " + url);
        abort = false;
        bool success = false;
        try
        {
            if (instantiatedAvatar)
            {
                Debug.Log("Destroying previous loaded avatar...");
                Destroy(instantiatedAvatar);
            }
            if (loadedAssetBundle != null)
            {
                Debug.Log("Unloading previous assetbundle...");
                try
                {
                    loadedAssetBundle.Unload(true);
                }
                catch
                {
                    Debug.Log("Error unloading previous assetbundle...");
                }
            }

            UIManager.instance.SetMainTitle(null, "Downloading...");
            
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                www.Send();
                Debug.Log("Downloading...");
                while (!www.isDone && !abort)
                {
                    UIManager.instance.SetMainLoadingBarProgress(www.downloadProgress);
                    //Debug.Log("Downloading " + www.downloadProgress);//comment this out in build
                    yield return null;
                }                

                if (abort)
                {
                    Debug.Log("Aborting from download...");
                    UIManager.instance.SetMainLoadingBarProgress(1f);
                    UIManager.instance.SetMainTitle("", "Cancelled");
                }
                else
                {
                    if (www.downloadHandler.data.Length < 200 && www.downloadHandler.data[0] != 0x55)
                    {
                        Debug.Log(www.downloadHandler.text);
                        var errorResponse = JsonUtility.FromJson<error>(www.downloadHandler.text);
                        if (errorResponse!= null && errorResponse.message != null)
                        {
                            if (errorResponse.message == "Too large")
                            {
                                UIManager.instance.SetMainTitle("Error", "File is too large. Use the standalone client to view it.");
                                errorShown = true;
                            }
                            else
                            {
                                UIManager.instance.SetMainTitle("Error", "API Error. Does the file still exist?");
                                errorShown = true;
                            }
                        }
                    }
                    else
                    {
                        Debug.Log("Decompressing, client may hang...");
                        yield return new WaitForEndOfFrame();

                        //This is only needed for WebGL. Otherwise just use the assetbundle how you would any other.
                        var assetbundleData = www.downloadHandler.data;

#if UNITY_WEBGL && !UNITY_EDITOR || TEST_DECOMPRESS
                        assetbundleData = Decompressor.Attempt(www.downloadHandler.data);
#else
                        //assetbundleData = www.downloadHandler.data;
#endif
                        Debug.Log("Loading assetbundle async...");
                        yield return new WaitForEndOfFrame();
                        var request = AssetBundle.LoadFromMemoryAsync(assetbundleData);
                        yield return new WaitForEndOfFrame();
                        while (!request.isDone)
                        {
                            yield return new WaitForEndOfFrame();
                        }
                        loadedAssetBundle = request.assetBundle;
                        success = true;
                    }                    
                }
            }                 
        }
        finally
        {
            if (!success)
            {
                if (!errorShown)
                {
                    if (!abort)
                    {
                        UIManager.instance.SetMainTitle("Error", "Could not load avatar!");
                        Debug.Log("Coulden't load avatar");
                    }
                    else
                    {
                        UIManager.instance.SetMainTitle("Error", "Aborted!");
                        Debug.Log("Aborted!");
                    }
                }
                
            }
            else
            {
                onDownloaded();
            }
        }
    }
    public IEnumerator InstantiateAvatar( Action onInstantiated)
    {
        if (loadedAssetBundle == null)
        {
            UIManager.instance.SetMainTitle("Error", "Could not load avatar!");
            yield break;
        }
        UIManager.instance.SetMainTitle(null, "Loading...");
        var request = loadedAssetBundle.LoadAssetWithSubAssetsAsync("assets/_customavatar.prefab");
        while(!request.isDone && !abort)
        {
            Debug.Log("loading " + request.progress);//comment this out in build
            UIManager.instance.SetMainLoadingBarProgress(request.progress);
            yield return null;
        }
        if (abort)
        {
            Debug.Log("Aborting from instantiation...");
            request = null;
        }
        else
        {
            UIManager.instance.SetMainTitle(null, "Instantiating...");

            instantiatedAvatar = Instantiate(request.asset) as GameObject;
            instantiatedAvatar.transform.position = Vector3.zero;

            //WebGL has issues with alot of shaders.
#if UNITY_WEBGL && !UNITY_EDITOR
            PrintOrReplaceShaders(instantiatedAvatar, false, true);
#endif

            if (instantiatedAvatar.GetComponent<Animator>())
            {
                instantiatedAvatar.GetComponent<Animator>().runtimeAnimatorController = GameObject.FindObjectOfType<ObjectHolder>().animationController;
            }

            UIManager.instance.SetMainLoadingBarProgress(1f);
            //UIManager.instance.SetStatusText();

            onInstantiated();
        }
    }

    //for testing with predownloaded assetbundles
    public void LoadAssetBundleFromStreamingFolder(string filename)
    {
        TextAsset asset = Resources.Load(filename) as TextAsset;
        loadedAssetBundle = AssetBundle.LoadFromMemory(asset.bytes);

        if (loadedAssetBundle == null)
        {
            Debug.Log("Failed to load AssetBundle!");
            return;
        }
        var request = loadedAssetBundle.LoadAssetWithSubAssets("assets/_customavatar.prefab");

        instantiatedAvatar = Instantiate(request[0]) as GameObject;
        instantiatedAvatar.transform.position = Vector3.zero;
        
        GameObject mouseOrbitFollow = new GameObject("MouseOrbitFollow");
        mouseOrbitFollow.transform.position = instantiatedAvatar.GetComponentInChildren<Renderer>().bounds.center;
        mouseOrbit.target = mouseOrbitFollow.transform;    
    }

    public byte[] LoadFile(string filename)
    {
        var path = Application.dataPath + "/StreamingAssets/" + filename;
        return File.ReadAllBytes(path);
    }

    [Serializable]
    public class error
    {
        public string message;
        public int status_code;
    }
   
}
