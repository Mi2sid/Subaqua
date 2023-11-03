using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloraParameters : MonoBehaviour
{
    [Header("Biomes in which the object can spawn")]
    public GroundManager.Biome[] biomes;


    [Header("Rotation")]
    public GroundManager.Rotation rotation;
}