using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSoundController : MonoBehaviour
{
    [SerializeField] private AudioSource engineAudioSource;
    [SerializeField] private float maxMoveSpeed = 1.2f;

    private float baseVolume;
    private Vector3 lastPos = Vector3.zero;


    private void Start()
    {
        baseVolume = engineAudioSource.volume;
        lastPos = transform.position;
    }

    void Update()
    {
        float moveSpeed = Vector3.Distance(lastPos, transform.position) / Time.deltaTime;
        lastPos = transform.position;
        engineAudioSource.volume = baseVolume * (moveSpeed / maxMoveSpeed);
    }
}
