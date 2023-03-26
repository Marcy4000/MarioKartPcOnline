/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class BotCar : NetworkBehaviour
{
    [SerializeField] private Rigidbody theRB;


    [SerializeField] private float forwardAccel, revesreAccel, maxSpeed, turnStrenght, gravityForce = 10f, dragOnGround = 3f;

    private bool grounded;

    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private float groundRayLenght;
    [SerializeField] private Transform groundRayPoint;
    [SerializeField] private float yourRotationSpeedVariable;
    [SerializeField] private float kartOffset = 1.14f;

    [SerializeField] private AudioSource idle, moving;
    [SerializeField] private BoxCollider onlineCollider;

    [SerializeField] private KartLap kartLap;

    [SyncVar] public int currLap, currCheckpoint;
    private Transform checkpoint;
    private int lastValue;

    void Start()
    {
        if (isServer)
        {
            theRB.transform.parent = null;
            onlineCollider.enabled = false;
            lastValue = kartLap.CheckpointIndex;
            checkpoint = GetNextCheckPoint();
        }
    }

    private void Update()
    {
        if (isServer)
        {
            DoUpdate();
            kartLap.lapNumber = currLap;
            kartLap.CheckpointIndex = currCheckpoint;
        }
    }

    [Server]
    private void DoUpdate()
    {
        if (!moving.isPlaying)
        {
            idle.Stop();
            moving.Play();
        }
        if (lastValue != kartLap.CheckpointIndex)
        {
            checkpoint = GetNextCheckPoint();
        }
        Quaternion newRotation = Quaternion.FromToRotation(transform.forward, checkpoint.forward) * transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, newRotation, 0.02f);

        lastValue = kartLap.CheckpointIndex;
        transform.position = new Vector3(theRB.transform.position.x, theRB.transform.position.y - kartOffset, theRB.transform.position.z);
    }

    private void FixedUpdate()
    {
        if (isServer)
        {
            DoFixedUpdate();
        }
    }

    [Server]
    private void DoFixedUpdate()
    {
        grounded = false;
        RaycastHit hit;

        if (Physics.Raycast(groundRayPoint.position, -transform.up, out hit, groundRayLenght, whatIsGround))
        {
            grounded = true;

            Quaternion newRotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, newRotation, yourRotationSpeedVariable);
            if (hit.collider.CompareTag("Boost Pad"))
            {
                theRB.AddForce(transform.forward * 16000f);
            }
        }

        if (grounded)
        {
            theRB.drag = dragOnGround;
            theRB.AddForce(1000f * forwardAccel * transform.forward);
        }
        else
        {
            theRB.drag = 0.1f;
            theRB.AddForce(100f * -gravityForce * Vector3.up);
        }

    }

    [Server]
    private Transform GetNextCheckPoint()
    {
        LapCheckPoint[] things = FindObjectsOfType<LapCheckPoint>();

        foreach (var item in things)
        {
            if (item.Index == kartLap.CheckpointIndex)
            {
                return item.next;
            }
        }

        foreach (var item in things)
        {
            if (item.Index == 1)
            {
                return item.transform;
            }
        }

        return things[0].transform;
    }

    [Command(requiresAuthority = false)]
    public void ChangeLapNumber(int num)
    {
        currLap = num;
    }

    [Command(requiresAuthority = false)]
    public void ChangeCurrCheckpoint(int num)
    {
        currCheckpoint = num;
    }
}
*/