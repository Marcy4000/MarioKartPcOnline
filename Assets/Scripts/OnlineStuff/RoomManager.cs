using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using System.IO;
using Photon.Realtime;

public class RoomManager : MonoBehaviourPunCallbacks
{
    public static RoomManager Instance;

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
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
    
    public void CallCommand(int selectedStage)
    {
        photonView.RPC("ChangeSelectedStage", RpcTarget.All, selectedStage);
    }

    [PunRPC]
    public void ChangeSelectedStage(int newStage)
    {
        GlobalData.SelectedStage = newStage;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        photonView.RPC("ChangeSelectedStage", RpcTarget.All, GlobalData.SelectedStage);
        DiscordController.instance.UpdateStatusInfo("Enjoying the online experience", $"In a lobby ({PhotonNetwork.PlayerList.Length}/{GlobalData.PlayerCount})", "maric_rast", "Image made by AI", GlobalData.CharPngNames[GlobalData.SelectedCharacter], $"Currently playing as {GlobalData.CharPngNames[GlobalData.SelectedCharacter]}");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        DiscordController.instance.UpdateStatusInfo("Enjoying the online experience", $"In a lobby ({PhotonNetwork.PlayerList.Length}/{GlobalData.PlayerCount})", "maric_rast", "Image made by AI", GlobalData.CharPngNames[GlobalData.SelectedCharacter], $"Currently playing as {GlobalData.CharPngNames[GlobalData.SelectedCharacter]}");
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

    public void LeaveGame()
    {
        PlaceCounter.instance.karts = new KartLap[0];
        GlobalData.AllPlayersLoaded = false;
        GlobalData.HasSceneLoaded = false;
        SceneManager.LoadSceneAsync(0);
        Destroy(gameObject);
    }
}
