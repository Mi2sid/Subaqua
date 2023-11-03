using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
//using UnityEngine.Rendering.PostProcessing;

public class IGMenuController : MonoBehaviour
{
    public static bool isPaused = false;
    public GameObject menuContainer;
    public GameObject settingsContainer;
    public List<AudioSource> AudiosToPause;

    public void OpenMenu()
    {
        Time.timeScale = 0f;
        foreach (AudioSource audioSource in AudiosToPause)
            audioSource.Pause();
        menuContainer.SetActive(true);
        isPaused = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        foreach (AudioSource audioSource in AudiosToPause)
            audioSource.UnPause();
        menuContainer.SetActive(false);
        isPaused = false;
        Cursor.lockState = CursorLockMode.Locked;
    }


    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
        Application.Quit();
    }

    public void OpenSettings()
    {
        settingsContainer.SetActive(true);
        menuContainer.SetActive(false);
    }

    public void CloseSettings()
    {
        settingsContainer.SetActive(false);
        menuContainer.SetActive(true);
    }

    private void Update()
    {
        if (Input.GetButtonDown("Cancel"))
        {
            if (isPaused)
            {
                if (settingsContainer.activeSelf)
                    CloseSettings();
                else
                    ResumeGame();
            }
            else
                OpenMenu();
        }

        if (OVRInput.GetDown(OVRInput.RawButton.X))
        {

            if (isPaused)
            {
                if (settingsContainer.activeSelf)
                    CloseSettings();
                else
                    ResumeGame();
            }
            else
                OpenMenu();
        }
    }
}
