using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundFaunaParameters : MonoBehaviour
{
    [Header("Biomes in which the object can spawn")]
    public GroundManager.Biome[] biomes;


    [Header("Rotation")]
    public GroundManager.Rotation rotation;
}