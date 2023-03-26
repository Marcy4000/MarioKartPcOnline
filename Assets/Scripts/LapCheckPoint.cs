using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LapCheckPoint : MonoBehaviour
{
    public int Index;

    public Transform next, previus;

    private void Start()
    {
        LapCheckPoint[] checkPoints = FindObjectsOfType<LapCheckPoint>();
        bool foundThing1 = false, foundThing2 = false;

        foreach (LapCheckPoint cPoint in checkPoints)
        {
            if (!foundThing1 && cPoint.Index == Index - 1)
            {
                if (previus == null)
                {
                    previus = cPoint.transform;
                }
                foundThing1 = true;
            }

            if (!foundThing2 && cPoint.Index == Index + 1)
            {
                if (next == null)
                {
                    next = cPoint.transform;
                }
                foundThing2 = true;
            }
        }

        if (!foundThing1)
        {
            previus = FindObjectOfType<LapHandle>().transform;
        }

        if (!foundThing2)
        {
            next = FindObjectOfType<LapHandle>().transform;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<KartLap>())
        {
            KartLap Kart = other.GetComponent<KartLap>();

            if (Kart.CheckpointIndex == Index + 1 || Kart.CheckpointIndex == Index - 1)
            {
                Kart.CheckpointIndex = Index;
                Kart.UpdatePlace(PlaceCounter.instance.GetCurrentPlace(Kart));
            }
        }
        else if (other.GetComponent<RedShell>())
        {
            RedShell shell = other.GetComponent<RedShell>();

            if (!shell.photonView.IsMine)
            {
                return;
            }

            if (shell.currentCheckpoint == Index + 1 || shell.currentCheckpoint == Index - 1)
            {
                shell.currentCheckpoint = Index;
            }
        }
    }
}
