using System;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class Launcher : MonoBehaviour
{
    public static Launcher Instance;

    [SerializeField] TMP_InputField roomNameInputField, privateRoomInputField;
    [SerializeField] TMP_Text errorString, roomNameText;
    [SerializeField] RectTransform roomListContent, playerListContent;
    [SerializeField] GameObject roomListItemPrefab, playerListPrefab, startGameButton, selectedCourse, roomSettingsButton;

    void Start()
    {
        Instance = this;
        GlobalData.HasSceneLoaded = false;
        GlobalData.AllPlayersLoaded = false;

        if (GlobalData.HasIntroPlayed)
        {
            MenuManager.instance.OpenMenu("MainMenu");
            MenuManager.instance.PlayMusic();
        }
        else
        {
            MenuManager.instance.OpenMenu("Intro");
        }
        Debug.Log("Joined Lobby");

        SetSettingsValues();

        LobbyController.Instance.OnJoinedLobby += OnJoinedLobby;

        DiscordController.instance.UpdateStatusInfo("Enjoying the online experience", "Browsing the menus...", "maric_rast", "Image made by AI", GlobalData.CharPngNames[GlobalData.SelectedCharacter], $"Currently playing as {GlobalData.CharPngNames[GlobalData.SelectedCharacter]}");
    }

    bool intToBool(int val)
    {
        if (val != 0)
            return true;
        else
            return false;
    }

    public void CreateRoom(bool privateRoom)
    {
        if (string.IsNullOrEmpty(roomNameInputField.text))
        {
            return;
        }

        LobbyController.Instance.CreateLobby(roomNameInputField.text, privateRoom);
        MenuManager.instance.OpenMenu("Loading");
    }

    public void StartSingleplayerMode()
    {
        LobbyController.Instance.CreateLobby($"{LobbyController.Instance.Player.Data["PlayerName"].Value}'s Singleplayer Room", true);
        MenuManager.instance.OpenMenu("Loading");
    }

    public void OnJoinedLobby()
    {
        //DiscordController.instance.UpdateStatusInfo("Enjoying the online experience", $"In a lobby ({LobbyController.Instance.Lobby.Players.Count}/{GlobalData.PlayerCount})", "maric_rast", "Image made by AI", GlobalData.CharPngNames[GlobalData.SelectedCharacter], $"Currently playing as {GlobalData.CharPngNames[GlobalData.SelectedCharacter]}"); roomNameText.text = PhotonNetwork.CurrentRoom.Name;
        MenuManager.instance.OpenMenu("RoomMenu");

        foreach (Transform child in playerListContent)
        {
            Destroy(child.gameObject);
        }

        foreach (Player player in LobbyController.Instance.Lobby.Players)
        {
            Instantiate(playerListPrefab, playerListContent).GetComponent<PlayerListItem>().Initialize(player);
        }

        roomListContent.sizeDelta = new Vector2(roomListContent.sizeDelta.x, 75 * LobbyController.Instance.Lobby.Players.Count);
        startGameButton.SetActive(LobbyController.Instance.IsLocalPlayerHost());
        selectedCourse.SetActive(LobbyController.Instance.IsLocalPlayerHost());
        roomSettingsButton.SetActive(LobbyController.Instance.IsLocalPlayerHost());
    }

    public void JoinPrivateRoom()
    {
        if (string.IsNullOrEmpty(privateRoomInputField.text))
        {
            return;
        }

        DiscordController.instance.UpdateStatusInfo("Enjoying the online experience", $"In a private lobby", "maric_rast", "Image made by AI", GlobalData.CharPngNames[GlobalData.SelectedCharacter], $"Currently playing as {GlobalData.CharPngNames[GlobalData.SelectedCharacter]}");

        LobbyController.Instance.TryLobbyJoin(privateRoomInputField.text);
        MenuManager.instance.OpenMenu("Loading");
    }

    /*public override void OnMasterClientSwitched(Player newMasterClient)
    {
        startGameButton.SetActive(PhotonNetwork.IsMasterClient);
        selectedCourse.SetActive(PhotonNetwork.IsMasterClient);
        roomSettingsButton.SetActive(PhotonNetwork.IsMasterClient);
        foreach (Transform child in playerListContent)
        {
            Destroy(child.gameObject);
        }
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            Instantiate(playerListPrefab, playerListContent).GetComponent<PlayerListItem>().SetUp(player);
        }
    }*/

    /*public void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (Transform child in roomListContent)
        {
            Destroy(child.gameObject);
        }

        foreach (RoomInfo room in roomList)
        {
            if (room.RemovedFromList)
            {
                continue;
            }
            Instantiate(roomListItemPrefab, roomListContent).GetComponent<RoomListItem>().SetUp(room);
        }
        roomListContent.sizeDelta = new Vector2(roomListContent.sizeDelta.x, 75 * roomList.Count);
    }*/

    public void OnPlayerEnteredRoom(Player newPlayer)
    {
        Instantiate(playerListPrefab, playerListContent).GetComponent<PlayerListItem>().Initialize(newPlayer);
    }

    public void OnJoinRoomFailed(short returnCode, string message)
    {
        errorString.text = "Failed joining room: " + message;
        MenuManager.instance.OpenMenu("ErrorMenu");
    }

    private void SetSettingsValues()
    {
        GlobalData.SelectedCharacter = PlayerPrefs.GetInt("character", 0);
        GlobalData.UseController = intToBool(PlayerPrefs.GetInt("controller"));
        GlobalData.ShowName = intToBool(PlayerPrefs.GetInt("showName"));
        //PhotonNetwork.LocalPlayer.CustomProperties["character"] = GlobalData.SelectedCharacter;
        GlobalData.Score = PlayerPrefs.GetInt("score");
        //PhotonNetwork.LocalPlayer.CustomProperties["score"] = GlobalData.Score;
        //PhotonNetwork.LocalPlayer.CustomProperties["bio"] = PlayerPrefs.GetString("bio");

        /*if (!string.IsNullOrEmpty(PlayerPrefs.GetString("emblem")))
        {
            Texture2D tex = IMG2Sprite.LoadTextureFromBytes(Convert.FromBase64String(PlayerPrefs.GetString("emblem")));
            tex = IMG2Sprite.Resize(tex, 64, 64);
            PhotonNetwork.LocalPlayer.CustomProperties["emblem"] = tex.EncodeToPNG();
        }*/
    }
}
