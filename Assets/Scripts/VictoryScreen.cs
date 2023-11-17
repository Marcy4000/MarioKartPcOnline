using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class VictoryScreen : MonoBehaviour
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
        RoomManager.Instance.LeaveGame();
    }
}
