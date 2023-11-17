using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cutscene : MonoBehaviour
{
    public static Cutscene instance;
    public AudioSource introMusic;
    private GameObject mainCam;
    Animator animator;
    public bool isPlaying = false;

    private void Awake()
    {
        instance = this;
        animator = GetComponent<Animator>();
    }

    public void PlayCutscene(GameObject mainCamera)
    {
        Debug.Log(animator);
        isPlaying = true;
        mainCam = mainCamera;
        mainCamera.SetActive(false);
        animator.Play("cutscene");
        introMusic.Play();
    }

    public void CutsceneEnded()
    {
        isPlaying = false; 
        mainCam.SetActive(true);
        Countdown.Instance.StartCountdown();
        gameObject.SetActive(false);
    }
}
