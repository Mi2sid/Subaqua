using System;
using System.Collections;
using System.Collections.Generic;
using PM1_Debug;
using UnityEngine;
//using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

public class SettingsController : MonoBehaviour
{
    [Header("Objects to toggle")]
    public Debug_Master debugMaster;
    public GameObject postProcessingVolume;
    public GameObject depthCounter;

    public Material terrainMaterial;


    [Header("UI Togglers")]
    public Toggle postProcessToggle;
    public Toggle AOToggle;
    public Toggle DebugUIToggle;
    public Toggle DepthCounterToggle;
    public Toggle CausticsToggle;
    public Toggle ProceduralTextureToggle;

   // private AmbientOcclusion AOSettings;

    private void OnEnable()
    {
        DebugUIToggle.isOn = Vars.DEBUG_MODE;
    }

    private void Awake()
    {
        //AOSettings = postProcessingVolume.GetComponent<PostProcessVolume>().profile.GetSetting<AmbientOcclusion>();
        postProcessToggle.isOn = postProcessingVolume.activeSelf;
        AOToggle.gameObject.SetActive(postProcessToggle.isOn);
        //AOToggle.isOn = AOSettings.active;
        DebugUIToggle.isOn = Vars.DEBUG_MODE;
        DepthCounterToggle.isOn = depthCounter.activeSelf;
        terrainMaterial.SetInt("_Procedural", ProceduralTextureToggle.isOn ? 1 : 0);
        terrainMaterial.SetInt("_DoCaustics", CausticsToggle.isOn ? 1 : 0);
    }

    public void UpdatePostProcessingState()
    {
        postProcessingVolume.SetActive(postProcessToggle.isOn);
        AOToggle.gameObject.SetActive(postProcessToggle.isOn);
    }

    public void UpdateAOState()
    {
        //AOSettings.active = AOToggle.isOn;
    }

    public void UpdateDebugUIState()
    {
        debugMaster.SetDebugState(DebugUIToggle.isOn);
    }

    public void UpdateDepthCounterState()
    {
        depthCounter.SetActive(DepthCounterToggle.isOn);
    }

    public void ToggleCausticsState(bool state)
    {
        /*foreach (Caustics caustic in FindObjectsOfType<Caustics>(true))
        {
            //caustic.gameObject.SetActive(CausticsToggle.isOn);
            //caustic.GetComponent<Projector>().enabled = CausticsToggle.isOn; 
        }*/

        terrainMaterial.SetInt("_DoCaustics", state ? 1 : 0);
    }
    public void ToggleProceduralTexture(bool state) {
        terrainMaterial.SetInt("_Procedural", state ? 1 : 0);
    } 
}
