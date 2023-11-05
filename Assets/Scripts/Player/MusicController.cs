using System;
using System.Collections;
using System.Collections.Generic;
using Oculus.Platform.Samples.VrHoops;
using UnityEngine;
using Random = UnityEngine.Random;
using TMPro;
public class MusicController : MonoBehaviour
{
    [SerializeField] private TMP_Text musicText;
    [SerializeField] private Animator musicTextAnimator;
    [SerializeField] private Animator musicPanelAnimator;
    [SerializeField] private Animator musicSpeakerAnimator;
    [SerializeField] private AudioSource musicPlayer;
    [SerializeField] private List<AudioClip> musics;
    [SerializeField] private float maxTimeBetweenMusics = 60;
    private float nextMusicTime;
    private List<AudioClip> playedMusics;
    private void Start()
    {
        playedMusics = new List<AudioClip>();
        nextMusicTime = Random.Range(10, 30);
        StartCoroutine(NextMusic());
    }

    IEnumerator NextMusic()
    {
        while (true)
        {
            yield return new WaitForSeconds(nextMusicTime);
            int musicIndex = Random.Range(0, musics.Count);
            musicPlayer.clip = musics[musicIndex];
            musicText.text = musicPlayer.clip.name;
            musicTextAnimator.SetTrigger("show");
            musicPanelAnimator.SetTrigger("show");
            musicSpeakerAnimator.SetTrigger("show");
            if (musics.Count == 1)
            {
                for (int i = 0; i < playedMusics.Count; i++)
                    musics.Add(playedMusics[i]);
                playedMusics.Clear();
            }
            playedMusics.Add(musics[musicIndex]);
            musics.RemoveAt(musicIndex);
            musicPlayer.Play();
            nextMusicTime = musicPlayer.clip.length + Random.Range(30, maxTimeBetweenMusics);
        }
    }
}
