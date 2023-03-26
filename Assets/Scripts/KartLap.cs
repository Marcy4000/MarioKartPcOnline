using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class KartLap : MonoBehaviourPun, IPunObservable
{
    public int lapNumber;
    public int CheckpointIndex;
    public RacePlace racePlace;
    public CarController carController;
    public bool hasFinished = false;
    private bool ready = false;
    public Transform shellBackSPos, frontPosition;
    public static KartLap mainKart;

    void Awake()
    {
        lapNumber = 1;
        CheckpointIndex = 0;

        StartCoroutine(DoThing());
    }

    private void LateUpdate()
    {
        if (!ready || hasFinished)
        {
            return;
        }
        UpdatePlace(PlaceCounter.instance.GetCurrentPlace(this));
    }

    private IEnumerator DoThing()
    {
        while (!GlobalData.HasSceneLoaded)
        {
            yield return null;
        }

        yield return null;

        if (carController.pv.IsMine && !carController.botDrive)
        {
            mainKart = this;
            ready = true;
        }
    }

    public void UpdatePlace(RacePlace place)
    {
        if (PlaceCounter.instance.karts == null)
        {
            return;
        }

        racePlace = place;
        if (carController.pv.IsMine && !carController.botDrive || carController.pv.IsMine && carController.botDrive && carController.bulletBil)
        {
            PlaceCounter.instance.ChangePosition(racePlace);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Send checkpoint index and lap number to other clients
            stream.SendNext(CheckpointIndex);
            stream.SendNext(lapNumber);
            stream.SendNext(hasFinished);
        }
        else
        {
            // Receive checkpoint index and lap number from network
            CheckpointIndex = (int)stream.ReceiveNext();
            lapNumber = (int)stream.ReceiveNext();
            hasFinished = (bool)stream.ReceiveNext();
        }
    }
}