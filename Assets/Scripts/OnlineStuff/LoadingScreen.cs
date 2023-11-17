using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LoadingScreen : MonoBehaviour
{
    public GameObject loadingScreenPlayerPrefab;
    public TMP_Text courseName;
    private GameObject loadingScreen;

    void Start()
    {
        loadingScreen = GameObject.FindGameObjectWithTag("LoadingScreen");
        courseName.text = $"Next Track: {GlobalData.Stages[GlobalData.SelectedStage]}";

        foreach (Photon.Realtime.Player p in PhotonNetwork.PlayerList)
        {
            LoadingScreenPlayerElement element = Instantiate(loadingScreenPlayerPrefab, loadingScreen.transform.Find("NamesArea")).GetComponent<LoadingScreenPlayerElement>();
            element.SetUp(p);
        }
    }

}
