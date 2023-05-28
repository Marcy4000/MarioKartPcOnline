using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using System.IO;
using Photon.Realtime;

public class PlayerManager : MonoBehaviourPun
{
    PhotonView pv;
    public bool hasLoaded = false;
    public bool allPlayersLoaded;
    public PlayerManager[] players;
    private ExitGames.Client.Photon.Hashtable _playerCustomProprietes = new ExitGames.Client.Photon.Hashtable();

    private void Awake()
    {
        pv = GetComponent<PhotonView>();
    }

    private void Start()
    {
        players = FindObjectsOfType<PlayerManager>();
        if (pv.IsMine)
        {
            StartCoroutine(DoThing());
        }
    }

    IEnumerator DoThing()
    {
        Discord_Controller.instance.UpdateStatusInfo("Doing a polished race", $"Current track: {GlobalData.stages[GlobalData.SelectedStage]}", "maric_rast", "Image made by AI", GlobalData.charPngNames[GlobalData.SelectedCharacter], $"Currently playing as {GlobalData.charPngNames[GlobalData.SelectedCharacter]}"); 
        AsyncOperation load = SceneManager.LoadSceneAsync(GlobalData.stages[GlobalData.SelectedStage], LoadSceneMode.Additive);
        load.allowSceneActivation = true;
        while (!load.isDone)
        {
            yield return null;
        }
        pv.RPC("SetHasLoaded", RpcTarget.MasterClient, load.isDone);
        _playerCustomProprietes = PhotonNetwork.LocalPlayer.CustomProperties;
        _playerCustomProprietes["playerLoaded"] = true;
        PhotonNetwork.LocalPlayer.SetCustomProperties(_playerCustomProprietes);
        GlobalData.HasSceneLoaded = true;
        CreateController();
        CreateBots();
        /*if (PhotonNetwork.IsMasterClient)
        {
            while (!CheckIfReady())
            {
                yield return null;
            }
            pv.RPC("StartGame", RpcTarget.AllBuffered);
        }*/

        
        do
        {
            allPlayersLoaded = true;
            foreach (Photon.Realtime.Player p in PhotonNetwork.PlayerList)
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
        } while (allPlayersLoaded == false);

        if (allPlayersLoaded)
        {
            Cutscene.instance.PlayCutscene(Camera.main.gameObject);
        }
    }

    private bool CheckIfReady()
    {
        bool readyToStart = true;
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].hasLoaded == false)
            {
                readyToStart = false;
            }
        }
        return readyToStart;
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
            Debug.Log("Spawned thing!");
        }
    }

    [PunRPC]
    public void SetHasLoaded(bool value, PhotonMessageInfo info)
    {
        if (info.Sender == pv.Owner)
        {
            hasLoaded = value;
        }
    }

    [PunRPC]
    public void StartGame()
    {
        Cutscene.instance.PlayCutscene(Camera.main.gameObject);
    }
}
