using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathZone : MonoBehaviour
{
    [SerializeField] private Transform[] checkpoints;
    
    void Start()
    {
        LapCheckPoint[] checkpointsScripts = FindObjectsOfType<LapCheckPoint>();
        checkpoints = new Transform[checkpointsScripts.Length];
        for (int i = 0; i < checkpointsScripts.Length; i++)
        {
            checkpoints[i] = checkpointsScripts[i].transform;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<KartLap>())
        {
            KartLap kartLap = other.GetComponent<KartLap>();

            if (!kartLap.carController.pv.IsMine)
            {
                return;
            }

            Transform otherThing = checkpoints[0];

            foreach (var item in checkpoints)
            {
                if (item.GetComponent<LapCheckPoint>().Index == kartLap.CheckpointIndex)
                {
                    otherThing = item;
                    break;
                }
            }

            other.transform.position = otherThing.position;
            other.transform.rotation = Quaternion.FromToRotation(other.transform.forward, otherThing.forward) * other.transform.rotation;
        }
    }
}
