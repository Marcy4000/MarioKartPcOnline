using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using System.IO;
using Photon.Realtime;

public class PlayerManager : MonoBehaviourPun
{
    private bool allPlayersLoaded;
    private ExitGames.Client.Photon.Hashtable _playerCustomProprietes = new ExitGames.Client.Photon.Hashtable();
    private GameObject loadingScreen;

    private void Start()
    {
        loadingScreen = GameObject.FindGameObjectWithTag("LoadingScreen");
        if (photonView.IsMine)
        {
            StartCoroutine(LoadTrack());
        }
    }

    private IEnumerator LoadTrack()
    {
        DiscordController.instance.UpdateStatusInfo("Doing a polished race", $"Current track: {GlobalData.Stages[GlobalData.SelectedStage]}", "maric_rast", "Image made by AI", GlobalData.CharPngNames[GlobalData.SelectedCharacter], $"Currently playing as {GlobalData.CharPngNames[GlobalData.SelectedCharacter]}");

        AsyncOperation load = SceneManager.LoadSceneAsync(GlobalData.Stages[GlobalData.SelectedStage], LoadSceneMode.Additive);
        load.allowSceneActivation = true;

        yield return new WaitUntil(() => load.isDone);

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(GlobalData.Stages[GlobalData.SelectedStage]));

        _playerCustomProprietes = PhotonNetwork.LocalPlayer.CustomProperties;
        _playerCustomProprietes["playerLoaded"] = true;
        PhotonNetwork.LocalPlayer.SetCustomProperties(_playerCustomProprietes);
        GlobalData.HasSceneLoaded = true;
        CreateController();
        if (GlobalData.SpawnBots)
        {
            CreateBots();
        }

        GlobalData.AllPlayersLoaded = false;
        do
        {
            allPlayersLoaded = true;
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                object loaded;
                if (p.CustomProperties.TryGetValue("playerLoaded", out loaded))
                {
                    if (!(bool)loaded)
                    {
                        allPlayersLoaded = false;
                        break;
                    }
                }
                else
                {
                    allPlayersLoaded = false;
                    break;
                }
            }

            yield return null;
        } while (!allPlayersLoaded);

        GlobalData.AllPlayersLoaded = true;

        //Cutscene.instance.PlayCutscene(Camera.main.gameObject);
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("StartGame", RpcTarget.AllViaServer);
        }
    }

    private void CreateController()
    {
        Transform spawnPoint = SpawnManager.Instance.getSpawnPoint(GetCurrentId());
        PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "NewKart"), spawnPoint.position, spawnPoint.rotation);
    }

    private int GetCurrentId()
    {
        Player[] players = PhotonNetwork.PlayerList;
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == PhotonNetwork.LocalPlayer)
            {
                return i;
            }
        }
        return 0;
    }

    private void CreateBots()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Not master client, so no bot car instantiated");
            return;
        }

        for (int i = PhotonNetwork.PlayerList.Length; i < GlobalData.PlayerCount; i++)
        {
            Transform spawnPoint = SpawnManager.Instance.getSpawnPoint(i);
            PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "BotKart"), spawnPoint.position, spawnPoint.rotation);
            Debug.Log("Spawned bot!");
        }
    }

    [PunRPC]
    public void StartGame()
    {
        Debug.Log("Starting Game!");
        Destroy(loadingScreen);
        Cutscene.instance.PlayCutscene(Camera.main.gameObject);
        _playerCustomProprietes = PhotonNetwork.LocalPlayer.CustomProperties;
        _playerCustomProprietes["playerLoaded"] = false;
        PhotonNetwork.LocalPlayer.SetCustomProperties(_playerCustomProprietes);
    }
}
