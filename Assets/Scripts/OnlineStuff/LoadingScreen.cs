using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LoadingScreen : MonoBehaviour
{
    public GameObject loadingScreenPlayerPrefab;
    public TMP_Text courseName;
    private GameObject loadingScreen;
    private Transform namesArea;

    void Start()
    {
        loadingScreen = GameObject.FindGameObjectWithTag("LoadingScreen");
        courseName.text = $"Next Track: {GlobalData.Stages[GlobalData.SelectedStage]}";
        namesArea = loadingScreen.transform.Find("NamesArea");

        foreach (Player p in LobbyController.Instance.Lobby.Players)
        {
            LoadingScreenPlayerElement element = Instantiate(loadingScreenPlayerPrefab, namesArea).GetComponent<LoadingScreenPlayerElement>();
            element.SetUp(p);
        }
    }

}
