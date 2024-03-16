using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using UnityEngine.UI;

public class RoomSettingsMenu : MonoBehaviour
{
    [SerializeField] GameObject menuHolder;
    [SerializeField] TMP_InputField maxPlayersInput;
    [SerializeField] Toggle privateRoomToggle, botsToggle;

    private void Start()
    {
        UpdateSettingsValue();
        CloseMenu();
    }

    private void OnDisable()
    {
        CloseMenu();
    }

    private void UpdateSettingsValue()
    {
        privateRoomToggle.isOn = !PhotonNetwork.CurrentRoom.IsVisible;
        maxPlayersInput.text = PhotonNetwork.CurrentRoom.MaxPlayers.ToString();
        botsToggle.isOn = GlobalData.SpawnBots;
    }

    public void OpenMenu()
    {
        UpdateSettingsValue();
        menuHolder.SetActive(true);
    }

    public void CloseMenu()
    {
        menuHolder.SetActive(false);
    }

    private void SetMaxPlayers(int players)
    {
        if (players > 0 && players <= GlobalData.PlayerCount)
        {
            PhotonNetwork.CurrentRoom.MaxPlayers = players;
        }
    }

    public void OnMaxPlayersChange(string value)
    {
        int maxPlayers = int.Parse(value);

        if (maxPlayers < 0 || maxPlayers > 12)
        {
            maxPlayersInput.text = PhotonNetwork.CurrentRoom.MaxPlayers.ToString();
            return;
        }

        SetMaxPlayers(maxPlayers);
    }

    public void SetRoomVisibility(bool visible)
    {
        PhotonNetwork.CurrentRoom.IsVisible = !visible;
    }

    public void SetSpawnBots(bool value)
    {
        GlobalData.SpawnBots = value;
    }
}
