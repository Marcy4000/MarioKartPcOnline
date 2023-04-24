using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;
using UnityEngine.Rendering;

public class RedShell : MonoBehaviourPun, IPunObservable
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

    //Values that will be synced over network
    Vector3 latestPos;
    Quaternion latestRot;
    //Lag compensation
    float currentTime = 0;
    double currentPacketTime = 0;
    double lastPacketTime = 0;
    Vector3 positionAtLastPacket = Vector3.zero;
    Quaternion rotationAtLastPacket = Quaternion.identity;

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
        if (!photonView.IsMine)
        {
            //Lag compensation
            double timeToReachGoal = currentPacketTime - lastPacketTime;
            currentTime += Time.deltaTime;

            //Update remote player
            transform.position = Vector3.Lerp(positionAtLastPacket, latestPos, (float)(currentTime / timeToReachGoal));
            transform.rotation = Quaternion.Lerp(rotationAtLastPacket, latestRot, (float)(currentTime / timeToReachGoal));
        }

        if (!photonView.IsMine || (!agent.isOnNavMesh && ! agent.isOnOffMeshLink))
        {
            return;
        }

        if (!agent.pathPending)
        {
            if (Mathf.RoundToInt(Time.time) % 1 == 0)
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
        yield return new WaitForSeconds(1f);
        GetComponent<SphereCollider>().enabled = true;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            //We own this player: send the others our data
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            //Network player, receive data
            latestPos = (Vector3)stream.ReceiveNext();
            latestRot = (Quaternion)stream.ReceiveNext();

            //Lag compensation
            currentTime = 0.0f;
            lastPacketTime = currentPacketTime;
            currentPacketTime = info.SentServerTime;
            positionAtLastPacket = transform.position;
            rotationAtLastPacket = transform.rotation;
        }
    }
}
