using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using Photon.Realtime;
using System;
using System.Collections;

public class Launcher : MonoBehaviourPunCallbacks
{
    public static Launcher Instance;

    [SerializeField] TMP_InputField roomNameInputField, privateRoomInputField;
    [SerializeField] TMP_Text errorString, roomNameText;
    [SerializeField] RectTransform roomListContent, playerListContent;
    [SerializeField] GameObject roomListItemPrefab, playerListPrefab, startGameButton, selectedCourse, roomSettingsButton;

    private bool isSwitchingRegion = false; // Flag to track region switching

    void Start()
    {
        Instance = this;
        GlobalData.HasSceneLoaded = false;
        GlobalData.AllPlayersLoaded = false;

        if (PhotonNetwork.IsConnected)
        {
            MenuManager.instance.PlayMusic();
            if (GlobalData.ReturnToLobby)
            {
                OnJoinedRoom();
            }
            else
            {
                if (PhotonNetwork.InRoom)
                {
                    LeaveRoom();
                }
                else if (!PhotonNetwork.InLobby)
                {
                    PhotonNetwork.JoinLobby();
                }
                else
                {
                    if (MenuManager.instance != null && MenuManager.instance.CurrentMenuName() == "Loading")
                    {
                        MenuManager.instance.OpenMenu("MainMenu");
                    }
                }
            }
            return;
        }

        GlobalData.SelectedRegion = PlayerPrefs.GetInt("region");
        Debug.Log("Connecting To Master");
        MenuManager.instance.OpenMenu("Loading");
        PhotonNetwork.EnableCloseConnection = true;
        ConnectToSelectedRegion();
    }

    private void ConnectToSelectedRegion()
    {
        Debug.Log($"Connecting to region: {GlobalData.Regions[GlobalData.SelectedRegion]}");
        PhotonNetwork.ConnectUsingSettings(new AppSettings
        {
            AppIdRealtime = "b7c9977c-c203-4d1f-8e73-1941233841cd",
            UseNameServer = true,
            Protocol = 0,
            EnableProtocolFallback = true,
            FixedRegion = GlobalData.Regions[GlobalData.SelectedRegion]
        });
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log($"Connected To Master ({PhotonNetwork.GetPing()})");
        PhotonNetwork.JoinLobby();
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log($"Disconnected from Master: {cause}");
        if (isSwitchingRegion)
        {
            isSwitchingRegion = false;
            ConnectToSelectedRegion();
        }
        else
        {
            if (MenuManager.instance != null && MenuManager.instance.CurrentMenuName() != "Intro")
            {
                MenuManager.instance.OpenMenu("MainMenu");
            }
        }
    }

    public override void OnJoinedLobby()
    {
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

        PhotonNetwork.CreateRoom(roomNameInputField.text.ToUpper(), new RoomOptions() { MaxPlayers = GlobalData.PlayerCount, IsVisible = !privateRoom });
        MenuManager.instance.OpenMenu("Loading");
    }

    public void StartSingleplayerMode()
    {
        PhotonNetwork.CreateRoom($"{PhotonNetwork.LocalPlayer.NickName}'s Singleplayer Room", new RoomOptions() { MaxPlayers = 1, IsVisible = false, IsOpen = false });
        MenuManager.instance.OpenMenu("Loading");
    }

    public override void OnJoinedRoom()
    {
        DiscordController.instance.UpdateStatusInfo("Enjoying the online experience", $"In a lobby ({PhotonNetwork.PlayerList.Length}/{GlobalData.PlayerCount})", "maric_rast", "Image made by AI", GlobalData.CharPngNames[GlobalData.SelectedCharacter], $"Currently playing as {GlobalData.CharPngNames[GlobalData.SelectedCharacter]}"); roomNameText.text = PhotonNetwork.CurrentRoom.Name;
        MenuManager.instance.OpenMenu("RoomMenu");
        foreach (Transform child in playerListContent)
        {
            Destroy(child.gameObject);
        }
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            Instantiate(playerListPrefab, playerListContent).GetComponent<PlayerListItem>().SetUp(player);
        }
        roomListContent.sizeDelta = new Vector2(roomListContent.sizeDelta.x, 75 * PhotonNetwork.PlayerList.Length);
        startGameButton.SetActive(PhotonNetwork.IsMasterClient);
        selectedCourse.SetActive(PhotonNetwork.IsMasterClient);
        roomSettingsButton.SetActive(PhotonNetwork.IsMasterClient);
    }

    public void JoinPrivateRoom()
    {
        if (string.IsNullOrEmpty(privateRoomInputField.text))
        {
            return;
        }

        DiscordController.instance.UpdateStatusInfo("Enjoying the online experience", $"In a private lobby", "maric_rast", "Image made by AI", GlobalData.CharPngNames[GlobalData.SelectedCharacter], $"Currently playing as {GlobalData.CharPngNames[GlobalData.SelectedCharacter]}");

        PhotonNetwork.JoinRoom(privateRoomInputField.text.ToUpper());
        MenuManager.instance.OpenMenu("Loading");
    }

    public void StartGame()
    {
        PhotonNetwork.LoadLevel(1);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
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
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        errorString.text = "Room Creation Failed: " + message;
        MenuManager.instance.OpenMenu("ErrorMenu");
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
        MenuManager.instance.OpenMenu("Loading");
    }

    public override void OnLeftRoom()
    {
        if (!isSwitchingRegion)
        {
            PhotonNetwork.JoinLobby();
        }
        MenuManager.instance.OpenMenu("MainMenu");
    }

    public void JoinRoom(RoomInfo info)
    {
        PhotonNetwork.JoinRoom(info.Name);
        MenuManager.instance.OpenMenu("Loading");
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
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
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Instantiate(playerListPrefab, playerListContent).GetComponent<PlayerListItem>().SetUp(newPlayer);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        errorString.text = "Failed joining room: " + message;
        MenuManager.instance.OpenMenu("ErrorMenu");
    }

    private void SetSettingsValues()
    {
        PhotonNetwork.NickName = PlayerPrefs.GetString("nickname", $"Player N.{UnityEngine.Random.Range(0, 69420)}");
        GlobalData.SelectedCharacter = PlayerPrefs.GetInt("character", 0);
        GlobalData.UseController = intToBool(PlayerPrefs.GetInt("controller"));
        GlobalData.ShowName = intToBool(PlayerPrefs.GetInt("showName"));
        PhotonNetwork.LocalPlayer.CustomProperties["character"] = GlobalData.SelectedCharacter;
        GlobalData.Score = PlayerPrefs.GetInt("score");
        PhotonNetwork.LocalPlayer.CustomProperties["score"] = GlobalData.Score;
        PhotonNetwork.LocalPlayer.CustomProperties["bio"] = PlayerPrefs.GetString("bio");

        if (!string.IsNullOrEmpty(PlayerPrefs.GetString("emblem")))
        {
            Texture2D tex = IMG2Sprite.LoadTextureFromBytes(Convert.FromBase64String(PlayerPrefs.GetString("emblem")));
            tex = IMG2Sprite.Resize(tex, 64, 64);
            PhotonNetwork.LocalPlayer.CustomProperties["emblem"] = tex.EncodeToPNG();
        }
    }

    public void SwitchRegion(int newRegionIndex)
    {
        Debug.Log($"Switching region initiated to index {newRegionIndex}");
        MenuManager.instance.OpenMenu("Loading");
        isSwitchingRegion = true;
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }
        else
        {
            isSwitchingRegion = false;
            ConnectToSelectedRegion();
        }
    }
}
