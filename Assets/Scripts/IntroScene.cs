using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class IntroScene : MonoBehaviour
{
    public bool hasCutsceneFinished = false;
    [SerializeField] private AudioSource mariokartpconline;
    [SerializeField] private AudioSource music;
    [SerializeField] private Animator menu;

    private InputAction anyKeyAction;
    private InputAction returnKeyAction;

    private void Awake()
    {
        anyKeyAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/anyKey");
        returnKeyAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/enter");

        anyKeyAction.Enable();
        returnKeyAction.Enable();
    }

    private void Update()
    {
        if (hasCutsceneFinished && anyKeyAction.triggered)
        {
            GlobalData.HasIntroPlayed = true;
            CloseMenu();
        }

        if (!hasCutsceneFinished && returnKeyAction.triggered)
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

    private void OnDestroy()
    {
        anyKeyAction.Disable();
        returnKeyAction.Disable();
    }
}
