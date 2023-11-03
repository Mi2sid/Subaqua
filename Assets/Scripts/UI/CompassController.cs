using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CompassController : MonoBehaviour
{
    Transform player;
    Vector3 north;

    public GameObject img;

    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
    }

    // Update is called once per frame
    void Update()
    {
        
        //layer.localEulerAngles = north;
        img.transform.Rotate(0.0f, 0.0f, player.eulerAngles.y-north.z, Space.Self);
        north.z = player.eulerAngles.y;
    }
}
