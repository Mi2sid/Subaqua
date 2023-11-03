using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class DepthCounter : MonoBehaviour
{
    public TMP_Text depthText;
    
    public GameObject depthDirUp;
    public GameObject depthDirDown;
    
    public Transform player;
    float offset;
    bool down;

    IEnumerator UpdateState()
    {
        while (true)
        {
            depthText.text = ((int)(offset - player.position.y)).ToString();
            if (player.rotation.eulerAngles.x < 270 != down)
            {
                down = player.rotation.eulerAngles.x < 270;
                depthDirDown.SetActive(down);
                depthDirUp.SetActive(!down);
            }
            yield return new WaitForSeconds(.1f);
        }
    }
    
    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
        offset = player.position.y;
        down = player.rotation.eulerAngles.x < 270;

        StartCoroutine(UpdateState());
    }

    private void OnDisable()
    {
        StopCoroutine(UpdateState());
    }

    private void OnEnable()
    {
        StartCoroutine(UpdateState());
    }
}
