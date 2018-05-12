using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager instance;
    public Light DirectionalLight;
    public const float LIGHT_INTENSITY_MIN = 0f;
    public const float LIGHT_INTENSITY_MAX = 2.5f;
    public List<Material> SkyboxMaterials;
    public List<Shader> ReplacementShaders;
    public Shader ReplacementShader
    {
        get;
        private set;
    }

    public void SetSkybox(int index)
    {
        Debug.Log("New Skybox: " + SkyboxMaterials[index].name);
        RenderSettings.skybox = SkyboxMaterials[index];
    }
    public void SetReplacmentShader(int index)
    {
        Debug.Log("New replacement shader: " + ReplacementShaders[index].name);
        ReplacementShader = ReplacementShaders[index];
    }

    public float GetLightIntensity()
    {
        return DirectionalLight.intensity;
    }
    public void SetLightIntensity(float value)
    {
        Debug.Log("New light intensity: " + value);
        DirectionalLight.intensity = value;
    }

    public float GetAmbientIntensity()
    {
        return RenderSettings.ambientIntensity;
    }
    public void SetAmbientIntensity(float value)
    {
        Debug.Log("New ambient intensity: " + value);
        RenderSettings.ambientIntensity = value;
    }

    private void Awake()
    {
        instance = this;
    }
    // Use this for initialization
    void Start ()
    {
        ReplacementShader = ReplacementShaders[0];


    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
