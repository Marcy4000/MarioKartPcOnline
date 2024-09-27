using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyController : MonoBehaviour
{
    public static LobbyController Instance { get; private set; }

    private int maxPartyMembers = 12;
    private Player localPlayer;
    private Lobby partyLobby;

    private float lobbyHeartBeatTimer = 15.0f;
    private float lobbyUpdateTimer = 1.1f;

    private string localPlayerName;

    private ILobbyEvents lobbyEvents;

    private bool loadResultsScreen = false;

#if UNITY_WEBGL
    private string connectionType = "wss";
#else
    private string connectionType = "dtls";
#endif

    public Lobby Lobby => partyLobby;
    public Player Player => localPlayer;

    public bool ShouldLoadResultsScreen { get => loadResultsScreen; set => loadResultsScreen = value; }

    public ILobbyEvents LobbyEvents => lobbyEvents;

    public event Action<Lobby> onLobbyUpdate;
    public event Action OnJoinedLobby;

    private void Awake()
    {
        if (Instance != null && this != Instance)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        localPlayerName = $"TestPlayer {UnityEngine.Random.Range(0, 1000)}";

        InitializeUnityAuthentication();
    }

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += (clientId) =>
        {
            if (clientId != NetworkManager.Singleton.LocalClientId)
            {
                return;
            }
            StartCoroutine(UpdatePlayerOwnerID(clientId));
        };
    }

    public void StartGame()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            return;
        }

        ChangePlayerName(PlayerPrefs.GetString("nickname", $"Player N.{UnityEngine.Random.Range(0, 69420)}"));
        StartCoroutine(LoadLobbyAsync());
    }

    private IEnumerator LoadLobbyAsync()
    {
        //LoadingScreen.Instance.ShowGenericLoadingScreen();
        var task = SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
        while (!task.isDone)
        {
            yield return null;
        }
        //LoadingScreen.Instance.HideGenericLoadingScreen();
    }

    private void Update()
    {
        HandleLobbyHeartBeat();

        HandleLobbyPollForUpdates();
    }

    private async void InitializeUnityAuthentication()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            //LoadingScreen.Instance.ShowGenericLoadingScreen();
            InitializationOptions options = new InitializationOptions();

            string eviromentName = Debug.isDebugBuild ? "development" : "production";
            options.SetEnvironmentName(eviromentName);

            options.SetProfile(UnityEngine.Random.Range(0, 1000).ToString());

            await UnityServices.InitializeAsync(options);

            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            localPlayer = new Player(AuthenticationService.Instance.PlayerId, AuthenticationService.Instance.Profile, new Dictionary<string, PlayerDataObject>
            {
                {"PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, localPlayerName)},
                {"OwnerID", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "0")},
            });

#if UNITY_WEBGL
            NetworkManager.Singleton.GetComponent<UnityTransport>().UseWebSockets = true;
#endif

            //LoadingScreen.Instance.HideGenericLoadingScreen();
        }
    }

    private async Task SubscribeToLobbyEvents(string lobbyID)
    {
        var lobbyEventCallbacks = new LobbyEventCallbacks();

        lobbyEventCallbacks.KickedFromLobby += () =>
        {
            partyLobby = null;
            NetworkManager.Singleton.Shutdown();
        };

        lobbyEventCallbacks.LobbyChanged += (changes) =>
        {
            changes.ApplyToLobby(partyLobby);
            onLobbyUpdate?.Invoke(Lobby);
        };

        lobbyEventCallbacks.DataChanged += (changes) =>
        {
            onLobbyUpdate?.Invoke(Lobby);
        };

        try
        {
            lobbyEvents = await Lobbies.Instance.SubscribeToLobbyEventsAsync(lobbyID, lobbyEventCallbacks);
        }
        catch (LobbyServiceException ex)
        {
            switch (ex.Reason)
            {
                case LobbyExceptionReason.AlreadySubscribedToLobby: Debug.LogWarning($"Already subscribed to lobby[{Lobby.Id}]. We did not need to try and subscribe again. Exception Message: {ex.Message}"); break;
                case LobbyExceptionReason.SubscriptionToLobbyLostWhileBusy: Debug.LogError($"Subscription to lobby events was lost while it was busy trying to subscribe. Exception Message: {ex.Message}"); throw;
                case LobbyExceptionReason.LobbyEventServiceConnectionError: Debug.LogError($"Failed to connect to lobby events. Exception Message: {ex.Message}"); throw;
                default: Debug.LogError(ex.Message); return;
            }
        }
    }

    private async void HandleLobbyHeartBeat()
    {
        if (partyLobby != null)
        {
            if (partyLobby.HostId != localPlayer.Id)
            {
                return;
            }

            lobbyHeartBeatTimer -= Time.deltaTime;
            if (lobbyHeartBeatTimer <= 0)
            {
                lobbyHeartBeatTimer = 15.0f;
                await LobbyService.Instance.SendHeartbeatPingAsync(partyLobby.Id);
            }
        }
    }

    private async void HandleLobbyPollForUpdates()
    {
        if (partyLobby != null)
        {
            lobbyUpdateTimer -= Time.deltaTime;
            if (lobbyUpdateTimer <= 0)
            {
                lobbyUpdateTimer = 1.1f;
                partyLobby = await LobbyService.Instance.GetLobbyAsync(partyLobby.Id);
                localPlayer = partyLobby.Players.Find(player => player.Id == localPlayer.Id);
                onLobbyUpdate?.Invoke(Lobby);
            }
        }
    }

    private async Task<JoinAllocation> JoinRelay(string joinCode)
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            return joinAllocation;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);
            return default;
        }
    }

    public async void CreateLobby(string lobbyName, bool privateLobby)
    {
        try
        {
            //LoadingScreen.Instance.ShowGenericLoadingScreen();
            var partyLobbyOptions = new CreateLobbyOptions()
            {
                IsPrivate = privateLobby,
                Player = localPlayer,
                Data = new Dictionary<string, DataObject>
                {
                    {"SelectedMap", new DataObject(DataObject.VisibilityOptions.Member, "RemoatStadium")}
                },
            };
            partyLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPartyMembers, partyLobbyOptions);
            Debug.Log($"Joined lobby: {partyLobby.Name}, code: {partyLobby.LobbyCode}");

            Allocation allocation = await AllocateRelay();
            string relayJoinCode = await GetRelayJoinCode(allocation);

            await LobbyService.Instance.UpdateLobbyAsync(partyLobby.Id, new UpdateLobbyOptions()
            {
                Data = new Dictionary<string, DataObject>
                {
                    {"RelayJoinCode", new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode)}
                }
            });

            await SubscribeToLobbyEvents(partyLobby.Id);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(allocation, connectionType));

            if (NetworkManager.Singleton.StartHost())
            {
                //NetworkManager.Singleton.SceneManager.OnSceneEvent += LoadingScreen.Instance.OnSceneEvent;
                NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
            }

            onLobbyUpdate?.Invoke(partyLobby);
            OnJoinedLobby?.Invoke();

            //LoadingScreen.Instance.HideGenericLoadingScreen();
        }
        catch (LobbyServiceException e)
        {
            //LoadingScreen.Instance.HideGenericLoadingScreen();
            Debug.LogError($"Failed to create party lobby: {e.Message}");
        }
    }

    public async void TryLobbyJoin(string joinCode)
    {
        try
        {
            //LoadingScreen.Instance.ShowGenericLoadingScreen();
            var joinOptions = new JoinLobbyByCodeOptions()
            {
                Player = localPlayer
            };

            partyLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(joinCode, joinOptions);

            JoinAllocation joinAllocation = await JoinRelay(partyLobby.Data["RelayJoinCode"].Value);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, connectionType));
            Debug.Log($"Joined lobby: {partyLobby.Name}");

            await SubscribeToLobbyEvents(partyLobby.Id);

            if (NetworkManager.Singleton.StartClient())
            {
                //NetworkManager.Singleton.SceneManager.OnSceneEvent += LoadingScreen.Instance.OnSceneEvent;
                NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
            }

            onLobbyUpdate?.Invoke(partyLobby);
            OnJoinedLobby?.Invoke();

            //LoadingScreen.Instance.HideGenericLoadingScreen();
        }
        catch (LobbyServiceException e)
        {
            //LoadingScreen.Instance.HideGenericLoadingScreen();
            Debug.LogError($"Failed to join party lobby: {e.Message}");
        }
    }

    public async Task<bool> QuickJoin()
    {
        try
        {
            QuickJoinLobbyOptions options = new QuickJoinLobbyOptions();

            options.Player = localPlayer;

            /*options.Filter = new List<QueryFilter>()
            {
                new QueryFilter(
                    field: QueryFilter.FieldOptions.MaxPlayers,
                    op: QueryFilter.OpOptions.GE,
                    value: "10")
             };*/

            partyLobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);

            //LoadingScreen.Instance.ShowGenericLoadingScreen();

            JoinAllocation joinAllocation = await JoinRelay(partyLobby.Data["RelayJoinCode"].Value);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, connectionType));
            Debug.Log($"Joined lobby: {partyLobby.Name}");

            await SubscribeToLobbyEvents(partyLobby.Id);

            if (NetworkManager.Singleton.StartClient())
            {
                //NetworkManager.Singleton.SceneManager.OnSceneEvent += LoadingScreen.Instance.OnSceneEvent;
                NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
            }

            onLobbyUpdate?.Invoke(partyLobby);
            OnJoinedLobby?.Invoke();

            //LoadingScreen.Instance.HideGenericLoadingScreen();
            return true;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogWarning(e);
            return false;
        }
    }

    private async void UpdatePlayerData(UpdatePlayerOptions options)
    {
        if (Lobby == null || localPlayer == null)
        {
            return;
        }

        try
        {
            await LobbyService.Instance.UpdatePlayerAsync(Lobby.Id, localPlayer.Id, options);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    private async void UpdateLobbyData(UpdateLobbyOptions options)
    {
        if (Lobby == null)
        {
            return;
        }

        try
        {
            await LobbyService.Instance.UpdateLobbyAsync(Lobby.Id, options);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    private async Task<string> GetRelayJoinCode(Allocation allocation)
    {
        try
        {
            var relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"Relay join code: {relayJoinCode}");
            return relayJoinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);
            return default;
        }
    }

    public async void KickPlayer(string playerID)
    {
        if (partyLobby.HostId != localPlayer.Id)
        {
            return;
        }

        await RemoveFromParty(playerID);
    }

    private IEnumerator UpdatePlayerOwnerID(ulong id)
    {
        UpdatePlayerOptions options = new UpdatePlayerOptions();
        options.Data = localPlayer.Data;
        options.Data["OwnerID"].Value = id.ToString();

        Debug.Log($"Changed owner ID to {options.Data["OwnerID"].Value}");
        yield return null;
        UpdatePlayerData(options);
    }

    public void ChangePlayerName(string newName)
    {
        UpdatePlayerOptions options = new UpdatePlayerOptions();
        options.Data = localPlayer.Data;
        options.Data["PlayerName"].Value = newName;
        Debug.Log($"Changed name to {options.Data["PlayerName"].Value}");

        UpdatePlayerData(options);
    }

    public void ChangeLobbyVisibility(bool isPrivate)
    {
        if (partyLobby.HostId != localPlayer.Id)
        {
            return;
        }

        UpdateLobbyOptions options = new UpdateLobbyOptions();
        options.Data = partyLobby.Data;
        options.MaxPlayers = partyLobby.MaxPlayers;
        options.HostId = partyLobby.HostId;
        options.Name = partyLobby.Name;
        options.IsPrivate = isPrivate;

        UpdateLobbyData(options);
    }

    private async Task<Allocation> AllocateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPartyMembers - 1);

            return allocation;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);

            return default;
        }
    }

    /*public async void LeaveLobby()
    {
        LoadingScreen.Instance.ShowGenericLoadingScreen();
        await RemoveFromParty(localPlayer.Id);
        partyLobby = null;
        NetworkManager.Singleton.Shutdown();

        playerNetworkManagers.Clear();

        lobbyUI.ShowMainMenuUI();
        LoadingScreen.Instance.HideGenericLoadingScreen();
    }*/

    private async void LeaveLobbyNoGUI()
    {
        await RemoveFromParty(localPlayer.Id);
        partyLobby = null;
        NetworkManager.Singleton.Shutdown();
    }

    private async Task RemoveFromParty(string playerID)
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(partyLobby.Id, playerID);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    public void StartMatch()
    {
        if (partyLobby.HostId != localPlayer.Id)
        {
            return;
        }
        ChangeLobbyVisibility(true);
        // UPDATE TO ACTUAL GAME SCENE
        NetworkManager.Singleton.SceneManager.LoadScene("CharacterSelect", LoadSceneMode.Single);
    }

    public void LoadGameMap()
    {
        if (partyLobby.HostId != localPlayer.Id)
        {
            return;
        }
        string selectedMap = partyLobby.Data["SelectedMap"].Value;
        NetworkManager.Singleton.SceneManager.LoadScene(selectedMap, LoadSceneMode.Single);
    }

    public void LoadResultsScreen()
    {
        if (partyLobby.HostId != localPlayer.Id)
        {
            return;
        }

        //LoadingScreen.Instance.ShowGenericLoadingScreen();

        NetworkManager.Singleton.SceneManager.LoadScene("LobbyScene", LoadSceneMode.Single);
    }

    private IEnumerator LoadResultsScreenAsync()
    {
        var task = SceneManager.LoadSceneAsync("GameResults", LoadSceneMode.Additive);
        while (!task.isDone)
        {
            yield return null;
        }
    }

    public void ReturnToLobby(bool leaveLobby)
    {
        if (leaveLobby) LeaveLobbyNoGUI();
        StartCoroutine(LoadLobbyAsync());
    }

    public void ReturnToHomeWithoutLeavingLobby()
    {
        StartCoroutine(ReturnToHomeWithoutLeavingLobbyAsync());
    }

    private IEnumerator ReturnToHomeWithoutLeavingLobbyAsync()
    {
        //LoadingScreen.Instance.ShowGenericLoadingScreen();
        var loadTask = SceneManager.UnloadSceneAsync("GameResults");

        yield return loadTask;

        yield return new WaitForSeconds(0.1f);

        //LoadingScreen.Instance.HideGenericLoadingScreen();
    }

    private void OnSceneEvent(SceneEvent sceneEvent)
    {
        switch (sceneEvent.SceneEventType)
        {
            case SceneEventType.LoadComplete:
                if (sceneEvent.SceneName == "LobbyScene" && loadResultsScreen)
                {
                    StartCoroutine(LoadResultsScreenAsync());
                    loadResultsScreen = false;
                }
                break;
        }
    }

    public Player GetPlayerByID(string playerID)
    {
        foreach (var player in partyLobby.Players)
        {
            if (player.Id == playerID)
            {
                return player;
            }
        }

        return null;
    }

    public bool IsLocalPlayerHost()
    {
        return partyLobby.HostId == localPlayer.Id;
    }

    /*public bool IsPlayerInResultScreen(Player player)
    {
        foreach (var playerNetworkManager in playerNetworkManagers)
        {
            if (playerNetworkManager.LocalPlayer.Id == player.Id)
            {
                return playerNetworkManager.IsInResultScreen;
            }
        }

        return false;
    }

    public bool IsAnyPlayerInResultScreen()
    {
        foreach (var playerNetworkManager in playerNetworkManagers)
        {
            if (playerNetworkManager.IsInResultScreen)
            {
                return true;
            }
        }

        return false;
    }*/
}