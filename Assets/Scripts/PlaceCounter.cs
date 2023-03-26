using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RacePlace { first, second, third, fourth, fifth, sixth, seventh, eighth, nineth, tenth, eleventh, twelveth }

public class PlaceCounter : MonoBehaviour
{
    public static PlaceCounter instance { get; private set; }
    public Animator animator;
    public KartLap[] karts;
    LapCheckPoint[] checkPoints;
    Transform finishLine;

    private void Start()
    {
        instance = this;
        karts = new KartLap[0];
        StartCoroutine(GetKartsAndCheckpoints());
    }

    IEnumerator GetKartsAndCheckpoints()
    {
        while (!GlobalData.HasSceneLoaded)
        {
            yield return null;
        }
        yield return new WaitForSeconds(2.5f);
        karts = FindObjectsOfType<KartLap>();
        checkPoints = FindObjectsOfType<LapCheckPoint>();
        finishLine = FindObjectOfType<LapHandle>().transform;
    }

    public RacePlace GetCurrentPlace(KartLap kart)
    {
        int currPlace = 7;
        RacePlace newPlace;

        foreach (var thingKart in karts)
        {
            if (thingKart == kart)
            {
                continue;
            }

            if (thingKart.lapNumber < kart.lapNumber)
            {
                currPlace--;
            }
            else if (thingKart.lapNumber == kart.lapNumber)
            {
                if (thingKart.CheckpointIndex < kart.CheckpointIndex)
                {
                    currPlace--;
                }
                else if (thingKart.CheckpointIndex == kart.CheckpointIndex)
                {
                    if (Vector3.Distance(kart.transform.position, GetCheckpointTransformByIndex(kart.CheckpointIndex + 1).position) < Vector3.Distance(thingKart.transform.position, GetCheckpointTransformByIndex(thingKart.CheckpointIndex + 1).position))
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
