using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class KartLap : MonoBehaviourPun, IPunObservable
{
    public int lapNumber;
    public int CheckpointIndex;
    public int racePlace = 1; // 1st place by default
    public IKartController kartController;
    public bool hasFinished = false;
    public Transform shellBackSPos, frontPosition;
    public static KartLap mainKart;

    void Awake()
    {
        lapNumber = 1;
        CheckpointIndex = 0;

        StartCoroutine(SetMainKart());
    }

    private void Start()
    {
        // Get the kart controller component
        kartController = GetComponent<IKartController>();
        StartCoroutine(RegisterWithRaceManager());
    }

    private IEnumerator RegisterWithRaceManager()
    {
        // Wait for RaceStateManager to be available
        yield return new WaitUntil(() => RaceStateManager.instance != null);
        
        // Register this kart with the centralized race manager
        string playerName = kartController.IsBot ? $"Bot {photonView.ViewID}" : photonView.Owner?.NickName ?? "Unknown";
        RaceStateManager.instance.RegisterKart(photonView.ViewID, playerName, kartController.IsBot);
    }

    private IEnumerator SetMainKart()
    {
        yield return new WaitUntil(() => GlobalData.AllPlayersLoaded);

        if (kartController.PhotonView.IsMine && !kartController.IsBot)
        {
            mainKart = this;
        }
    }

    public void UpdatePlace(int place)
    {
        racePlace = place;
        if (kartController.PhotonView.IsMine && !kartController.IsBot)
        {
            PlaceCounter.instance.ChangePosition(racePlace);
        }
    }

    // New methods for centralized race management
    public void SetLapData(int newLap, int newCheckpoint, bool finished)
    {
        lapNumber = newLap;
        CheckpointIndex = newCheckpoint;
        hasFinished = finished;
    }

    public void SetCheckpointIndex(int newCheckpoint)
    {
        CheckpointIndex = newCheckpoint;
    }

    public void UpdateNetworkPosition(int newPosition)
    {
        racePlace = newPosition;
        if (kartController.PhotonView.IsMine && !kartController.IsBot)
        {
            PlaceCounter.instance.ChangePosition(racePlace);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Send basic data for fallback synchronization
            stream.SendNext(CheckpointIndex);
            stream.SendNext(lapNumber);
            stream.SendNext(hasFinished);
            stream.SendNext(racePlace);
        }
        else if (stream.IsReading)
        {
            // Receive basic data (centralized system takes priority)
            CheckpointIndex = (int)stream.ReceiveNext();
            lapNumber = (int)stream.ReceiveNext();
            hasFinished = (bool)stream.ReceiveNext();
            racePlace = (int)stream.ReceiveNext();
        }
    }
}
