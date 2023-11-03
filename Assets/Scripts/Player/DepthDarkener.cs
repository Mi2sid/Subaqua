using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DepthDarkener : MonoBehaviour
{
    [SerializeField] private MainCamera waterController;
    [SerializeField] private float densityModifier = 0.002f;
    [SerializeField] float sunModifier = 0.06f;
    [SerializeField] float ambientModifier = 0.0001f;
    private float waterLevel;
    private float baseLightIntensity;
    private float baseDensity;
    private Light sunLight;
    private float baseAmbientIntensity;
    private float baseAmbientIntensityMult;
    private Color baseFogColor;

    private void Start()
    {
        baseDensity = waterController.Density;
        sunLight = RenderSettings.sun;
        waterLevel = waterController.WaterHeight;
        baseLightIntensity = sunLight.intensity;
        baseAmbientIntensity = RenderSettings.ambientIntensity;
        baseAmbientIntensityMult = RenderSettings.reflectionIntensity;
        baseFogColor = RenderSettings.fogColor;
    }

    void Update()
    {
        float distance = Mathf.Abs(transform.position.y - waterLevel);
        waterController.Density = Math.Min(1f, baseDensity + distance * densityModifier);
        sunLight.intensity = baseLightIntensity - distance * sunModifier;
        RenderSettings.ambientIntensity = baseAmbientIntensity - distance * ambientModifier;
        RenderSettings.reflectionIntensity = baseAmbientIntensityMult - distance * ambientModifier;
        if (transform.position.y - waterLevel >= -0.5f) RenderSettings.fog = false;
        else RenderSettings.fog = true;
        RenderSettings.fogColor = new Color( Math.Clamp(baseFogColor.r-distance * ambientModifier, 0, baseFogColor.r), Math.Clamp(baseFogColor.g - distance * ambientModifier, 0, baseFogColor.g), Math.Clamp(baseFogColor.b - distance * ambientModifier, 0, baseFogColor.b));
    }
}