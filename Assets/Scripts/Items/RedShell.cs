using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;

public class RedShell : MonoBehaviourPun
{
    private Rigidbody rb;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private float groundRayLenght;
    [SerializeField] private Transform groundRayPoint;
    private bool grounded;
    [SerializeField] private KartLap targetKart, currKart;
    public int currentCheckpoint, targetCheckPoint;
    [SerializeField] private Transform nextCheckPoint;
    private int lastValue;
    [SerializeField] private bool move = false;
    private bool safeMode = false;
    [SerializeField] bool useNewRedshellCode;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!photonView.IsMine)
        {
            rb.constraints = RigidbodyConstraints.FreezeAll;
            return;
        }
        rb.AddForce(transform.forward * 1500f, ForceMode.Impulse);
        StartCoroutine(Timer());
    }

    public void SetCurrentKartLap(KartLap _kart)
    {
        currKart = _kart;
        currentCheckpoint = _kart.CheckpointIndex;
        foreach (var kart in PlaceCounter.instance.karts)
        {
            if (kart.racePlace == currKart.racePlace - 1)
            {
                if (useNewRedshellCode)
                {
                    targetKart = kart;
                    agent.destination = targetKart.frontPosition.position;
                    StartCoroutine(SafeFrames());
                }
                else
                {
                    targetKart = kart;
                    move = true;
                }
                Debug.Log("Found Thing");
                return;
            }
        }
        AskToDestroy();
    }

    IEnumerator Timer()
    {
        yield return new WaitForSeconds(25f);
        safeMode = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponent<PlayerScript>())
        {
            PlayerScript player = collision.gameObject.GetComponent<PlayerScript>();
            if (!player.photonView.IsMine)
            {
                return;
            }
            player.GetHit(false);
            photonView.RPC("AskToDestroy", RpcTarget.All);
            return;
        }

        if (collision.gameObject.GetComponent<KartLap>())
        {
            KartLap kart = collision.gameObject.GetComponent<KartLap>();
            if (!kart.carController.pv.IsMine)
            {
                return;
            }
            kart.carController.GetHit();
            photonView.RPC("AskToDestroy", RpcTarget.All);
        }
    }

    [PunRPC]
    private void AskToDestroy()
    {
        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
            SkinManager.instance.characters[SkinManager.instance.selectedCharacter].modelAnimator.SetTrigger("Hit");
        }
    }

    private void Update()
    {
        if (!photonView.IsMine)
        {
            return;
        }

        if (!useNewRedshellCode)
        {
            if (!move)
            {
                return;
            }

            if (lastValue != currentCheckpoint)
            {
                nextCheckPoint = GetNextCheckPoint();
            }

            if (!safeMode)
            {
                transform.LookAt(targetKart.transform, Vector3.up);
            }
            else
            {
                transform.position = targetKart.transform.position;
            }
        }
        else
        {
            if (!agent.pathPending)
            {
                if (Time.frameCount % 15 == 0)
                {
                    agent.destination = targetKart.frontPosition.position;
                    return;
                }

                if (agent.remainingDistance <= agent.stoppingDistance)
                {
                    if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                    {
                        agent.destination = targetKart.frontPosition.position;
                    }
                }
            }
        }
    }

    private Transform GetNextCheckPoint()
    {
        LapCheckPoint[] things = FindObjectsOfType<LapCheckPoint>();

        if (currentCheckpoint == targetKart.CheckpointIndex)
        {
            return targetKart.transform;
        }

        foreach (var item in things)
        {
            if (item.Index == currentCheckpoint)
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

    private IEnumerator SafeFrames()
    {
        GetComponent<SphereCollider>().enabled = false;
        yield return new WaitForSeconds(1);
        GetComponent<SphereCollider>().enabled = true;
    }

    private void FixedUpdate()
    {
        if (!photonView.IsMine)
        {
            return;
        }

        if (!useNewRedshellCode)
        {
            return;
        }
        grounded = false;
        RaycastHit hit;

        Debug.DrawRay(groundRayPoint.position, -transform.up * groundRayLenght);
        if (Physics.Raycast(groundRayPoint.position, -transform.up, out hit, groundRayLenght, whatIsGround))
        {
            grounded = true;
        }

        if (!move)
        {
            return;
        }

        if (!grounded)
        {
            rb.AddForce(-1200f * Vector3.up);
        }
        rb.AddForce(1000f * transform.forward);
    }
}
