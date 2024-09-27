using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<KartLap>())
        {
            KartLap kartLap = other.GetComponent<KartLap>();

            if (!kartLap.carController.IsOwner)
            {
                return;
            }

            Transform targetCheckpoint = PlaceCounter.instance.GetCheckpointTransformByIndex(kartLap.CheckpointIndex);

            other.transform.position = targetCheckpoint.position;
            other.transform.rotation = Quaternion.FromToRotation(other.transform.forward, targetCheckpoint.forward) * other.transform.rotation;
        }
    }
}
