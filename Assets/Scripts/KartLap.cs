using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class KartLap : NetworkBehaviour
{
    private NetworkVariable<int> lapNumber = new NetworkVariable<int>();
    private NetworkVariable<int> checkpointIndex = new NetworkVariable<int>();
    private NetworkVariable<bool> hasFinished = new NetworkVariable<bool>(false);
    private NetworkVariable<RacePlace> racePlace = new NetworkVariable<RacePlace>();

    public int LapNumber => lapNumber.Value;
    public int CheckpointIndex => checkpointIndex.Value;
    public bool HasFinished => hasFinished.Value;
    public RacePlace RacePlace => racePlace.Value;

    public CarController carController;
    private bool ready = false;
    public Transform shellBackSPos, frontPosition;
    public static KartLap mainKart;

    void Awake()
    {
        lapNumber.Value = 1;
        checkpointIndex.Value = 0;

        StartCoroutine(SetMainKart());
    }

    private void LateUpdate()
    {
        if (!ready || hasFinished.Value || !carController.IsOwner)
        {
            return;
        }
        UpdatePlace(PlaceCounter.instance.GetCurrentPlace(this));
    }

    private IEnumerator SetMainKart()
    {
        yield return new WaitUntil(() => GlobalData.AllPlayersLoaded);

        if (carController.IsOwner && carController.isPlayer)
        {
            mainKart = this;
        }
        ready = true;
    }

    public void UpdatePlace(RacePlace place)
    {
        if (PlaceCounter.instance.karts == null)
        {
            return;
        }

        racePlace.Value = place;
        if (carController.IsOwner && carController.isPlayer)
        {
            PlaceCounter.instance.ChangePosition(racePlace.Value);
        }
    }

    public void IncreaseLapCount()
    {
        lapNumber.Value++;
        checkpointIndex.Value = 0;
    }

    public void SetLapNumber(int lap)
    {
        lapNumber.Value = lap;
    }

    public void SetCheckpointIndex(int index)
    {
        checkpointIndex.Value = index;
    }

    public void SetHasFinished(bool finished)
    {
        hasFinished.Value = finished;
    }
}
