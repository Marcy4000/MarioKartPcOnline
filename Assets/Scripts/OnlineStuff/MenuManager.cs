using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public static MenuManager instance;

    [SerializeField] Menu[] menus;
    [SerializeField] AudioSource music;

    private void Awake()
    {
        instance = this;
    }

    public void OpenMenu(string menuName)
    {
        for (int i = 0; i < menus.Length; i++)
        {
            if (menus[i].menuName == menuName)
            {
                menus[i].Open();
            }
            else if (menus[i].open)
            {
                CloseMenu(menus[i]);
            }
        }
    }

    public void OpenMenu(Menu menuObj)
    {
        for (int i = 0; i < menus.Length; i++)
        {
            if (menus[i].open)
            {
                CloseMenu(menus[i]);
            }
        }
        menuObj.Open();
    }

    public void CloseMenu(Menu menuObj)
    {
        menuObj.Close();
    }

    public void PlayMusic(AudioClip clip)
    {
        music.Stop();
        music.clip = clip;
        music.Play();
    }

    public void PlayMusic()
    {
        music.Play();
    }

    public string CurrentMenuName()
    {
        for (int i = 0; i < menus.Length; i++)
        {
            if (menus[i].open)
            {
                return menus[i].menuName;
            }
        }
        return null;
    }
}
