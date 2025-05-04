using System;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Netcode;
using UnityEngine;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode.Transports.UTP;

public class Launcher : MonoBehaviour
{
    public static Launcher Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TMP_InputField roomNameInputField;
    [SerializeField] private TMP_InputField privateRoomInputField;
    [SerializeField] private TMP_Text errorText;
    [SerializeField] private TMP_Text roomNameText;
    [SerializeField] private RectTransform roomListContent;
    [SerializeField] private RectTransform playerListContent;

    [Header("Prefabs")]
    [SerializeField] private GameObject roomListItemPrefab;
    [SerializeField] private GameObject playerListPrefab;
    [SerializeField] private GameObject startGameButton;
    [SerializeField] private GameObject selectedCourse;
    [SerializeField] private GameObject roomSettingsButton;

    private Lobby currentLobby;
    private string playerId;

    private async void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        try
        {
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            playerId = AuthenticationService.Instance.PlayerId;
            Debug.Log($"Player signed in with ID: {playerId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize Unity Services: {e.Message}");
            errorText.text = "Failed to initialize networking services";
            MenuManager.instance.OpenMenu("ErrorMenu");
            return;
        }

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

        SetSettingsValues();
        UpdateDiscordStatus();
    }

    private void OnDestroy()
    {
        if (currentLobby != null)
        {
            LeaveLobby();
        }
    }

    public async void CreateRoom(bool isPrivate)
    {
        if (string.IsNullOrEmpty(roomNameInputField.text))
        {
            return;
        }

        try
        {
            MenuManager.instance.OpenMenu("Loading");

            // Creo il Relay server per il networking
            hostData = await RelayService.Instance.CreateAllocationAsync(GlobalData.PlayerCount);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(hostData.AllocationId);

            // Creo la lobby
            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = isPrivate,
                Data = new Dictionary<string, DataObject>
                {
                    { "RelayJoinCode", new DataObject(DataObject.VisibilityOptions.Member, joinCode) }
                }
            };

            currentLobby = await LobbyService.Instance.CreateLobbyAsync(roomNameInputField.text, GlobalData.PlayerCount, options);
            
            // Avvio il networking come host
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(hostData);
            NetworkManager.Singleton.StartHost();

            OnJoinedLobby();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create room: {e.Message}");
            errorText.text = $"Failed to create room: {e.Message}";
            MenuManager.instance.OpenMenu("ErrorMenu");
        }
    }

    public async void StartSingleplayerMode()
    {
        try
        {
            var hostData = await RelayService.Instance.CreateAllocationAsync(1);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(hostData);
            NetworkManager.Singleton.StartHost();
            // Carica direttamente la scena in single player
            NetworkManager.Singleton.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to start singleplayer: {e.Message}");
            errorText.text = $"Failed to start singleplayer mode";
            MenuManager.instance.OpenMenu("ErrorMenu");
        }
    }

    private void OnJoinedLobby()
    {
        MenuManager.instance.OpenMenu("RoomMenu");
        roomNameText.text = currentLobby.Name;
        UpdatePlayerList();
        UpdateUIElements();
        UpdateDiscordStatus();
    }

    public async void JoinPrivateRoom()
    {
        if (string.IsNullOrEmpty(privateRoomInputField.text))
        {
            return;
        }

        try
        {
            MenuManager.instance.OpenMenu("Loading");
            
            currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(privateRoomInputField.text);
            string relayJoinCode = currentLobby.Data["RelayJoinCode"].Value;
            
            joinData = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(joinData);
            NetworkManager.Singleton.StartClient();

            OnJoinedLobby();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to join room: {e.Message}");
            errorText.text = $"Failed to join room: {e.Message}";
            MenuManager.instance.OpenMenu("ErrorMenu");
        }
    }

    private void UpdatePlayerList()
    {
        foreach (Transform child in playerListContent)
        {
            Destroy(child.gameObject);
        }

        foreach (Player player in currentLobby.Players)
        {
            Instantiate(playerListPrefab, playerListContent).GetComponent<PlayerListItem>().Initialize(player);
        }

        roomListContent.sizeDelta = new Vector2(roomListContent.sizeDelta.x, 75 * currentLobby.Players.Count);
    }

    private void UpdateUIElements()
    {
        bool isHost = NetworkManager.Singleton.IsHost;
        startGameButton.SetActive(isHost);
        selectedCourse.SetActive(isHost);
        roomSettingsButton.SetActive(isHost);
    }

    private async void LeaveLobby()
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, playerId);
            if (NetworkManager.Singleton.IsHost)
            {
                await LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error leaving lobby: {e.Message}");
        }

        NetworkManager.Singleton.Shutdown();
        currentLobby = null;
    }

    private void SetSettingsValues()
    {
        GlobalData.SelectedCharacter = PlayerPrefs.GetInt("character", 0);
        GlobalData.UseController = PlayerPrefs.GetInt("controller") != 0;
        GlobalData.ShowName = PlayerPrefs.GetInt("showName") != 0;
        GlobalData.Score = PlayerPrefs.GetInt("score");
    }

    private void UpdateDiscordStatus()
    {
        string status = currentLobby != null 
            ? $"In a lobby ({currentLobby.Players.Count}/{GlobalData.PlayerCount})" 
            : "Browsing the menus...";
            
        DiscordController.instance.UpdateStatusInfo(
            "Enjoying the online experience",
            status,
            "maric_rast",
            "Image made by AI",
            GlobalData.CharPngNames[GlobalData.SelectedCharacter],
            $"Currently playing as {GlobalData.CharPngNames[GlobalData.SelectedCharacter]}"
        );
    }
}
