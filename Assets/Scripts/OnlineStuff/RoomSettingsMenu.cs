using TMPro;
using UnityEngine;
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
        privateRoomToggle.isOn = LobbyController.Instance.Lobby.IsPrivate;
        maxPlayersInput.text = LobbyController.Instance.Lobby.MaxPlayers.ToString();
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
            //PhotonNetwork.CurrentRoom.MaxPlayers = players;
        }
    }

    public void OnMaxPlayersChange(string value)
    {
        int maxPlayers = int.Parse(value);

        if (maxPlayers < 0 || maxPlayers > 12)
        {
            maxPlayersInput.text = LobbyController.Instance.Lobby.MaxPlayers.ToString();
            return;
        }

        SetMaxPlayers(maxPlayers);
    }

    public void SetRoomVisibility(bool visible)
    {
        LobbyController.Instance.ChangeLobbyVisibility(!visible);
    }

    public void SetSpawnBots(bool value)
    {
        GlobalData.SpawnBots = value;
    }
}
