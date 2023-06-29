using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using Photon.Realtime;
using System;

public class Launcher : MonoBehaviourPunCallbacks
{
    public static Launcher Instance;

    [SerializeField] TMP_InputField roomNameInputField, privateRoomInputField;
    [SerializeField] TMP_Text errorString, roomNameText;
    [SerializeField] RectTransform roomListContent, playerListContent;
    [SerializeField] GameObject roomListItemPrefab, playerListPrefab, startGameButton, selectedCourse;

    void Start()
    {
        Instance = this;
        GlobalData.HasSceneLoaded = false;
        GlobalData.selectedRegion = PlayerPrefs.GetInt("region");
        Debug.Log("Connecting To Master");
        MenuManager.instance.OpenMenu("Loading");
        PhotonNetwork.ConnectUsingSettings(new AppSettings
        {
            AppIdRealtime = "b7c9977c-c203-4d1f-8e73-1941233841cd",
            UseNameServer = true,
            Protocol = 0,
            EnableProtocolFallback = true,
            FixedRegion = GlobalData.regions[GlobalData.selectedRegion]
        });
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log($"Connected To Master ({PhotonNetwork.GetPing()})");
        PhotonNetwork.JoinLobby();
        PhotonNetwork.AutomaticallySyncScene = true;
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
        PhotonNetwork.NickName = PlayerPrefs.GetString("nickname", $"Player N.{UnityEngine.Random.Range(0, 69420)}");
        GlobalData.SelectedCharacter = PlayerPrefs.GetInt("character", 0);
        GlobalData.UseController = intToBool(PlayerPrefs.GetInt("controller"));
        GlobalData.showName = intToBool(PlayerPrefs.GetInt("showName"));
        PhotonNetwork.LocalPlayer.CustomProperties["character"] = GlobalData.SelectedCharacter;
        GlobalData.score = PlayerPrefs.GetInt("score");
        PhotonNetwork.LocalPlayer.CustomProperties["score"] = GlobalData.score;

        if (!string.IsNullOrEmpty(PlayerPrefs.GetString("emblem")))
        {
            Texture2D tex = IMG2Sprite.LoadTextureFromBytes(Convert.FromBase64String(PlayerPrefs.GetString("emblem")));
            tex = IMG2Sprite.Resize(tex, 64, 64);
            PhotonNetwork.LocalPlayer.CustomProperties["emblem"] = tex.EncodeToPNG();
        }
        Discord_Controller.instance.UpdateStatusInfo("Enjoying the online experience", "Browsing the menus...", "maric_rast", "Image made by AI", GlobalData.charPngNames[GlobalData.SelectedCharacter], $"Currently playing as {GlobalData.charPngNames[GlobalData.SelectedCharacter]}");
        //GlobalData.UseController = intToBool(PlayerPrefs.GetInt("controller", 0));
    }

    bool intToBool(int val)
    {
        if (val != 0)
            return true;
        else
            return false;
    }

    public void CreateRoom()
    {
        if (string.IsNullOrEmpty(roomNameInputField.text))
        {
            return;
        }
        PhotonNetwork.CreateRoom(roomNameInputField.text, new RoomOptions() { MaxPlayers = GlobalData.PlayerCount });
        MenuManager.instance.OpenMenu("Loading");
    }

    public void CreatePrivateRoom()
    {
        string roomName = "";
        char[] digits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
        for (int i = 0; i < 12; i++)
        {
            roomName += digits[UnityEngine.Random.Range(0, 9)];
        }
        PhotonNetwork.CreateRoom(roomName, new RoomOptions() { MaxPlayers = GlobalData.PlayerCount, IsVisible = false });
        MenuManager.instance.OpenMenu("Loading");
    }

    public override void OnJoinedRoom()
    {
        Discord_Controller.instance.UpdateStatusInfo("Enjoying the online experience", $"In a lobby ({PhotonNetwork.PlayerList.Length}/{GlobalData.PlayerCount})", "maric_rast", "Image made by AI", GlobalData.charPngNames[GlobalData.SelectedCharacter], $"Currently playing as {GlobalData.charPngNames[GlobalData.SelectedCharacter]}"); roomNameText.text = PhotonNetwork.CurrentRoom.Name;
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
    }

    public void JoinPrivateRoom()
    {
        Discord_Controller.instance.UpdateStatusInfo("Enjoying the online experience", $"In a private lobby", "maric_rast", "Image made by AI", GlobalData.charPngNames[GlobalData.SelectedCharacter], $"Currently playing as {GlobalData.charPngNames[GlobalData.SelectedCharacter]}"); if (string.IsNullOrEmpty(privateRoomInputField.text))
        {
            return;
        }
        PhotonNetwork.JoinRoom(privateRoomInputField.text);
        MenuManager.instance.OpenMenu("Loading");
    }

    public void StartGame()
    {
        PhotonNetwork.LoadLevel(1);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        startGameButton.SetActive(PhotonNetwork.IsMasterClient);
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
}
