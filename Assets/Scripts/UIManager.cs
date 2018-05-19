using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIManager : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public static UIManager instance;
    private Text AvatarNameMainText;
    private Text AvatarNameSubtitleText;
    private Text StatusText;
    private Text InformationText;

    private InputField AvatarIdInput;
    private Button GetAvatarButton;
    private Button CopyFromAddressbarButton;

    //settings panel
    private GameObject SettingsPanel;
    private Button SettingsToggleButton;
    private Dropdown SkyboxDropdown;
    private Slider LightIntensitySlider;
    private Slider AmbientIntensitySlider;
    private Dropdown ReplacementShaderDropdown;
    

    //testing. delete later
    private InputField AvatarIdInputStreaming;
    private Button GetAvatarButtonStreaming;


    private GameObject BottomPanel;

    public LoadingBar mainLoadingBar;

    public static bool isOver = false;

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("Mouse enter");
        isOver = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("Mouse exit");
        isOver = false;
    }

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(instance);
        }
        instance = this;
    }
    void Start ()
    {
        AvatarNameMainText = GameObject.Find("AvatarNameMainText").GetComponent<Text>();
        AvatarNameSubtitleText = GameObject.Find("AvatarNameSubtitleText").GetComponent<Text>();
        StatusText = GameObject.Find("StatusText").GetComponent<Text>();
        InformationText = GameObject.Find("InformationText").GetComponent<Text>();

        BottomPanel = GameObject.Find("BottomPanel");
        BottomPanel.SetActive(false);

        //top panel
        AvatarIdInput = GameObject.Find("AvatarIdInput").GetComponent<InputField>();
        AvatarIdInput.onValueChanged.AddListener((newValue) =>
        {
            Regex r = new Regex("^[a-zA-Z\\d-_]+$");
            if (r.IsMatch(newValue))
            {
                WebpageUtilities.SetURLParameters(newValue, true);
            }
            else
            {
                Debug.Log("not regex match");
            }
        });

        GetAvatarButton = GameObject.Find("GetAvatarButton").GetComponent<Button>();
        GetAvatarButton.onClick.AddListener(() =>
        {
            AvatarLoader.instance.LoadAvatar(AvatarIdInput.text);
        });
        /*
        CopyFromAddressbarButton = GameObject.Find("CopyFromAddressbarButton").GetComponent<Button>();
        CopyFromAddressbarButton.onClick.AddListener(() =>
        {
            var queryString = WebpageUtilities.GetURLParameters();
            if (!string.IsNullOrEmpty(queryString))
            {
                AvatarIdInput.text = queryString;
            }
        });
        */
        //settings
        SettingsPanel = GameObject.Find("SettingsPanel");
        SettingsToggleButton = GameObject.Find("SettingsToggleButton").GetComponent<Button>();
        SettingsToggleButton.onClick.AddListener(() =>
        {
            SettingsPanel.SetActive(!SettingsPanel.activeSelf);            
        });
        SkyboxDropdown = GameObject.Find("SkyboxDropdown").GetComponent<Dropdown>();
        SkyboxDropdown.options = GetSkyboxDropdownOptions();
        SkyboxDropdown.onValueChanged.AddListener((value) =>
        {
            SettingsManager.instance.SetSkybox(value);
        });

        LightIntensitySlider = GameObject.Find("LightIntensitySlider").GetComponent<Slider>();
        LightIntensitySlider.maxValue = SettingsManager.LIGHT_INTENSITY_MAX;
        LightIntensitySlider.minValue = SettingsManager.LIGHT_INTENSITY_MIN;
        LightIntensitySlider.value = SettingsManager.instance.GetLightIntensity();
        LightIntensitySlider.onValueChanged.AddListener((value) =>
        {
            SettingsManager.instance.SetLightIntensity(value);
        });
        AmbientIntensitySlider = GameObject.Find("AmbientIntensitySlider").GetComponent<Slider>();
        AmbientIntensitySlider.maxValue = SettingsManager.LIGHT_INTENSITY_MAX;
        AmbientIntensitySlider.minValue = SettingsManager.LIGHT_INTENSITY_MIN;
        AmbientIntensitySlider.value = SettingsManager.instance.GetAmbientIntensity();
        AmbientIntensitySlider.onValueChanged.AddListener((value) =>
        {
            SettingsManager.instance.SetAmbientIntensity(value);
        });

        ReplacementShaderDropdown = GameObject.Find("ReplacementShaderDropdown").GetComponent<Dropdown>();
        ReplacementShaderDropdown.options = GetReplacmentShadersDropdownOptions();
        ReplacementShaderDropdown.onValueChanged.AddListener((value) =>
        {
            SettingsManager.instance.SetReplacmentShader(value);
        });



        SettingsPanel.SetActive(false);
        mainLoadingBar.gameObject.SetActive(false);
        //StartCoroutine(UpdateInputFromQueryString());
    }	
    

    //this doesnt seem to work?
    private IEnumerator UpdateInpasdasdfutFromQueryString()
    {
        yield return null;
#if UNITY_WEBGL && !UNITY_EDITOR
        if (!Application.isEditor)
        {
            yield return new WaitForSeconds(1f);
            var queryString = WebpageUtilities.GetURLParameters();
            if (!string.IsNullOrEmpty(queryString))
            {
                AvatarIdInput.text = queryString;
            }
        }    
#endif   
    }
    
    public void SetInformationText(string value)
    {
        InformationText.text = value;
    }

    public void SetMainTitle(string avatarNameMainText = null, string avatarNameSubtitleText = null)
    {
        if (avatarNameMainText != null)
            AvatarNameMainText.text = avatarNameMainText;
        if (avatarNameSubtitleText != null)
            AvatarNameSubtitleText.text = avatarNameSubtitleText;
        
    }
    
    public void SetMainLoadingBarProgress(float progress)
    {
        if (progress == 1f)
        {
            mainLoadingBar.SetProgress(0f, "");
            mainLoadingBar.gameObject.SetActive(false);
            //BottomPanel.SetActive(false);
        }
        else
        {
            //BottomPanel.SetActive(true);
            mainLoadingBar.gameObject.SetActive(true);
            mainLoadingBar.SetProgress(progress, Mathf.Round((progress * 100)).ToString());
        }
    }
    public void SetLoadedAvatarId(string id)
    {
        AvatarIdInput.text = id;
    }

    private List<Dropdown.OptionData> GetSkyboxDropdownOptions()
    {
        var options = new List<Dropdown.OptionData>();
        foreach(var skybox in SettingsManager.instance.SkyboxMaterials)
        {
            options.Add(new Dropdown.OptionData(skybox.name));
        }
        return options;
    }
    private List<Dropdown.OptionData> GetReplacmentShadersDropdownOptions()
    {
        var options = new List<Dropdown.OptionData>();
        foreach (var shader in SettingsManager.instance.ReplacementShaders)
        {
            options.Add(new Dropdown.OptionData(shader.name));
        }
        return options;
    }
}
