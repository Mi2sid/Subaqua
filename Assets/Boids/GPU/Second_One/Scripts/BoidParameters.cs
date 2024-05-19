using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BoidParameters", menuName = "ScriptableObjects/BoidParameters", order = 2)]
public class BoidParameters : ScriptableObject
{
    // Settings
    [HideInInspector]
    public float avgSpeed;

    [Header("Fishs")]

    public float minSpeed = 1;
    public float maxSpeed = 2;
    public float escapeSpeed = 4;

    public float percRay = 3;
    public float avoidRay = 3;
    public float maxSteerForce = 3;

    [Header("Collisions")]

    public LayerMask obstacleLayer;

    //public float sphereRad = .5f;
    public float weight = 10;
    public float detectCollDst = 10;

    [Header("Scare")]

    public LayerMask scareLayer;

    public bool canBeScared = true;

    void Awake()
    {
        avgSpeed = (minSpeed + maxSpeed) / 2;
    }
}