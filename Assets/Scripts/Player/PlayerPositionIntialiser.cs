using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlayerPositionIntialiser : MonoBehaviour
{

    [SerializeField] private Transform seascooterTransform;
    [SerializeField] private MainCamera mainCamera;
   void Awake()
    {
        transform.position = new Vector3(Random.Range(0f, 1000f), mainCamera.WaterHeight + 0.2f, Random.Range(0f, 1000f));
        seascooterTransform.position = transform.position + new Vector3(0, -1f, 1);
    }


}
    