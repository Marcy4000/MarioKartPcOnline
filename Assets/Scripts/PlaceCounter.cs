using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RacePlace { first, second, third, fourth, fifth, sixth, seventh, eighth, nineth, tenth, eleventh, twelveth }

public class PlaceCounter : MonoBehaviour
{
    public static PlaceCounter instance { get; private set; }
    [SerializeField] private Animator animator;
    public KartLap[] karts;
    private LapCheckPoint[] checkPoints;
    private Transform finishLine;

    private void Start()
    {
        instance = this;
        StartCoroutine(GetKartsAndCheckpoints());
    }

    IEnumerator GetKartsAndCheckpoints()
    {
        yield return new WaitUntil(() => GlobalData.AllPlayersLoaded);

        yield return new WaitForSeconds(0.2f);

        karts = FindObjectsOfType<KartLap>();
        checkPoints = FindObjectsOfType<LapCheckPoint>();
        finishLine = FindObjectOfType<LapHandle>().transform;
    }

    public RacePlace GetCurrentPlace(KartLap targetKart)
    {
        int currPlace = karts.Length - 1;
        RacePlace newPlace;

        foreach (var kart in karts)
        {
            if (kart == targetKart)
            {
                continue;
            }

            if (kart.lapNumber < targetKart.lapNumber)
            {
                currPlace--;
            }
            else if (kart.lapNumber == targetKart.lapNumber)
            {
                if (kart.CheckpointIndex < targetKart.CheckpointIndex)
                {
                    currPlace--;
                }
                else if (kart.CheckpointIndex == targetKart.CheckpointIndex)
                {
                    if (Vector3.Distance(targetKart.transform.position, GetCheckpointTransformByIndex(targetKart.CheckpointIndex + 1).position) < Vector3.Distance(kart.transform.position, GetCheckpointTransformByIndex(kart.CheckpointIndex + 1).position))
                    {
                        currPlace--;
                    }
                }
            }
        }

        newPlace = (RacePlace)currPlace;

        return newPlace;
    }

    public LapCheckPoint GetCheckpointByIndex(int index)
    {
        foreach (var checkpoint in checkPoints)
        {
            if (checkpoint.Index == index)
            {
                return checkpoint;
            }
        }
        return checkPoints[0];
    }

    public Transform GetCheckpointTransformByIndex(int index)
    {
        foreach (var checkpoint in checkPoints)
        {
            if (checkpoint.Index == index)
            {
                return checkpoint.transform;
            }
        }
        return finishLine;
    }

    public void ChangePosition(RacePlace place)
    {
        animator.Play(place.ToString());
    }
}
