using UnityEngine;

[System.Serializable]
public class NoiseParameters
{
    [Min(0.001f)] public float scale = 1;
    [Range(0, 1)] public float persistance = 1;
    [Range(1, 2)] public float lacunarity = 1;
    [Range(1, 10)] public int octaves = 1;
}
