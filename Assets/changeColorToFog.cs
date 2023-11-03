using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]

public class changeColorToFog : MonoBehaviour
{
    SpriteRenderer objectRenderer;

    // Start is called before the first frame update
    void Start()
    {
        objectRenderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        objectRenderer.color = RenderSettings.fogColor;
    }
}
