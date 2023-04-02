using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;
using UnityEngine.Rendering;

public class RedShell : MonoBehaviourPun
{
    private Rigidbody rb;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private float groundRayLenght;
    [SerializeField] private Transform groundRayPoint;
    private bool grounded;
    [SerializeField] private KartLap targetKart, currKart;
    public int currentCheckpoint;
    [SerializeField] private Transform nextCheckPoint;
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
    }

    public void SetCurrentKartLap(KartLap _kart)
    {
        currKart = _kart;
        currentCheckpoint = _kart.CheckpointIndex;
        foreach (var kart in PlaceCounter.instance.karts)
        {
            if (kart.racePlace == currKart.racePlace - 1)
            {
                    targetKart = kart;
                    agent.destination = targetKart.frontPosition.position;
                    StartCoroutine(SafeFrames());
                
                Debug.Log("Red Shell found target");
                return;
            }
        }
        AskToDestroy();
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
        if (!photonView.IsMine || (!agent.isOnNavMesh && ! agent.isOnOffMeshLink))
        {
            return;
        }

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

  

    private IEnumerator SafeFrames()
    {
        GetComponent<SphereCollider>().enabled = false;
        yield return new WaitForSeconds(1);
        GetComponent<SphereCollider>().enabled = true;
    }

}
