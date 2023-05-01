using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using System;
using System.IO;
using Photon.Realtime;

public class RoomManager : MonoBehaviourPunCallbacks
{
    public static RoomManager Instance;
    public int SelectedStage;
    PhotonView pv;

    private void Awake()
    {
        if (Instance)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
        pv = GetComponent<PhotonView>();
        Instance = this;
    }

    public override void OnEnable()
    {
        base.OnEnable();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }


    public override void OnDisable()
    {
        base.OnDisable();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    public void CallCommand(int thing)
    {
        pv.RPC("ChangeSelectedStage", RpcTarget.All, thing);
    }

    [PunRPC]
    public void ChangeSelectedStage(int newStage)
    {
        SelectedStage = newStage;
        GlobalData.SelectedStage = newStage;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        pv.RPC("ChangeSelectedStage", RpcTarget.All, GlobalData.SelectedStage);
        Discord_Controller.instance.UpdateStatusInfo("Enjoying the online experience", $"In a lobby ({PhotonNetwork.PlayerList.Length}/{GlobalData.PlayerCount})", "maric_rast", "Image made by AI", GlobalData.charPngNames[GlobalData.SelectedCharacter], $"Currently playing as {GlobalData.charPngNames[GlobalData.SelectedCharacter]}");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Discord_Controller.instance.UpdateStatusInfo("Enjoying the online experience", $"In a lobby ({PhotonNetwork.PlayerList.Length}/{GlobalData.PlayerCount})", "maric_rast", "Image made by AI", GlobalData.charPngNames[GlobalData.SelectedCharacter], $"Currently playing as {GlobalData.charPngNames[GlobalData.SelectedCharacter]}");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (scene.buildIndex == 1)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.CurrentRoom.IsOpen = false;
                PhotonNetwork.CurrentRoom.IsVisible = false;
            }
            PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "PlayerManager"), Vector3.zero, Quaternion.identity);
        }
        else if (scene.buildIndex == 0)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.CurrentRoom.IsOpen = true;
                PhotonNetwork.CurrentRoom.IsVisible = true;
            }
        }
    }
}
