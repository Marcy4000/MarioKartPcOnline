using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CharacterSelectScreen : MonoBehaviour
{
    public DummyKart dummyKart;
    public int selectedChar;
    private ExitGames.Client.Photon.Hashtable _myCustomProprieties = new ExitGames.Client.Photon.Hashtable();

    private void OnEnable()
    {
        _myCustomProprieties = PhotonNetwork.LocalPlayer.CustomProperties;
        dummyKart.gameObject.SetActive(true);
        dummyKart.SetCharacter(GlobalData.SelectedCharacter);
    }

    private void OnDisable()
    {
        dummyKart.gameObject.SetActive(false);
    }

    public void SetSelectedChar(int chara)
    {
        selectedChar = chara;
    }

    public void ChangeCharacter()
    {
        GlobalData.SelectedCharacter = selectedChar;
        PlayerPrefs.SetInt("character", selectedChar);
        _myCustomProprieties["character"] = selectedChar;
    }
}
