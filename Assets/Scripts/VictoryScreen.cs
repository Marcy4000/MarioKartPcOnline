using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class VictoryScreen : MonoBehaviourPunCallbacks
{
    public static VictoryScreen instance { get; private set; }
    public TMP_Text resultText;

    private void Start()
    {
        instance = this;
        gameObject.SetActive(false);
    }

    public void ReturnToMainMenu()
    {
        PhotonNetwork.Disconnect();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Destroy(RoomManager.Instance.gameObject);
        RoomManager.Instance = null;
        SceneManager.LoadScene(0);
    }
}
