using System.Collections;
using System.Collections.Generic;
using UnityEditor.Purchasing;
using UnityEngine;

public class IntroScene : MonoBehaviour
{
    public bool hasCutsceneFinished = false;
    [SerializeField] private AudioSource mariokartpconline;
    [SerializeField] private AudioSource music;
    [SerializeField] private Animator menu;

    private void Update()
    {
        if (hasCutsceneFinished && Input.anyKeyDown)
        {
            GlobalData.HasIntroPlayed = true;
            CloseMenu();
        }

        if (!hasCutsceneFinished && Input.GetKeyDown(KeyCode.Return))
        {
            music.Play();
            GlobalData.HasIntroPlayed = true;
            MenuManager.instance.OpenMenu("MainMenu");
        }
    }

    public void PlaySoundEffect()
    {
        mariokartpconline.Play();
        music.PlayDelayed(1.54f);
        hasCutsceneFinished = true;
    }

    private void CloseMenu()
    {
        menu.Play("Close");
    }

    public void LoadMenu()
    {
        MenuManager.instance.OpenMenu("MainMenu");
    }
}
