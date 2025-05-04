using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections;
using Unity.Services.Lobbies.Models;

public class PlayerManager : NetworkBehaviour
{
    private bool allPlayersLoaded;
    private GameObject loadingScreen;
    public static PlayerManager Instance { get; private set; }

    private void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (GlobalData.HasSceneLoaded)
        {
            loadingScreen = GameObject.Find("LoadingScreen");
            StartCoroutine(LoadTrack());
        }
    }

    private IEnumerator LoadTrack()
    {
        DiscordController.instance.UpdateStatusInfo(
            "Doing a polished race", 
            $"Current track: {GlobalData.Stages[GlobalData.SelectedStage]}", 
            "maric_rast", 
            "Image made by AI", 
            GlobalData.CharPngNames[GlobalData.SelectedCharacter], 
            $"Currently playing as {GlobalData.CharPngNames[GlobalData.SelectedCharacter]}"
        );

        AsyncOperation load = SceneManager.LoadSceneAsync(GlobalData.Stages[GlobalData.SelectedStage], LoadSceneMode.Additive);
        load.allowSceneActivation = true;

        yield return new WaitUntil(() => load.isDone);

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(GlobalData.Stages[GlobalData.SelectedStage]));

        GlobalData.HasSceneLoaded = true;
        CreateController();

        if (GlobalData.SpawnBots && IsHost)
        {
            CreateBots();
        }

        yield return StartCoroutine(WaitForAllPlayersToLoad());
        
        if (IsHost)
        {
            StartGameServerRpc();
        }
    }

    private IEnumerator WaitForAllPlayersToLoad()
    {
        yield return new WaitUntil(() => NetworkManager.Singleton.ConnectedClients.Count == GlobalData.PlayerCount || 
                                       (NetworkManager.Singleton.ConnectedClients.Count > 0 && !NetworkManager.Singleton.IsHost));
        
        allPlayersLoaded = true;
        GlobalData.AllPlayersLoaded = true;
    }

    private void CreateController()
    {
        Transform spawnPoint = SpawnManager.Instance.getSpawnPoint((int)GetCurrentId());
        GameObject playerPrefab = NetworkManager.Singleton.NetworkConfig.PlayerPrefab;
        
        if (NetworkManager.Singleton.IsHost)
        {
            var player = NetworkManager.Singleton.ConnectedClients[NetworkManager.Singleton.LocalClientId].PlayerObject;
            if (player != null)
            {
                player.transform.position = spawnPoint.position;
                player.transform.rotation = spawnPoint.rotation;
            }
        }
    }

    private ulong GetCurrentId()
    {
        if (!NetworkManager.Singleton.IsHost)
            return NetworkManager.Singleton.LocalClientId;

        return 0;
    }

    private void CreateBots()
    {
        if (!IsHost) return;

        int currentPlayers = NetworkManager.Singleton.ConnectedClients.Count;
        for (int i = currentPlayers; i < GlobalData.PlayerCount; i++)
        {
            Transform spawnPoint = SpawnManager.Instance.getSpawnPoint(i);
            SpawnBotServerRpc(spawnPoint.position, spawnPoint.rotation);
        }
    }

    [ServerRpc]
    private void SpawnBotServerRpc(Vector3 position, Quaternion rotation)
    {
        GameObject botPrefab = Resources.Load<GameObject>("PhotonPrefabs/BotKart");
        GameObject bot = Instantiate(botPrefab, position, rotation);
        NetworkObject netObj = bot.GetComponent<NetworkObject>();
        netObj.Spawn();
    }

    [ServerRpc]
    private void StartGameServerRpc()
    {
        StartGameClientRpc();
    }

    [ClientRpc]
    private void StartGameClientRpc()
    {
        Debug.Log("Starting Game!");
        if (loadingScreen != null)
        {
            Destroy(loadingScreen);
        }
        Cutscene.instance.PlayCutscene(Camera.main.gameObject);
    }
}
