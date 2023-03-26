using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cutscene : MonoBehaviour
{
    public static Cutscene instance;
    public AudioSource introMusic;
    private GameObject mainCam;
    Animator animator;

    private void Start()
    {
        instance = this;
        animator = GetComponent<Animator>();
    }

    public void PlayCutscene(GameObject mainCamera)
    {
        mainCam = mainCamera;
        mainCamera.SetActive(false);
        animator.Play("cutscene");
        introMusic.Play();
    }

    public void CutsceneEnded()
    {
        mainCam.SetActive(true);
        Countdown.Instance.StartCountdown();
        gameObject.SetActive(false);
    }
}
