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
    [SerializeField] bool useNewRedshellCode;
    private float timer = 0f;

    //Values that will be synced over network
    Vector3 latestPos, latestVel;
    Quaternion latestRot;
    //Lag compensation
    float currentTime = 0;
    double currentPacketTime = 0;
    double lastPacketTime = 0;
    Vector3 positionAtLastPacket = Vector3.zero;
    Vector3 velocityAtLastPacket = Vector3.zero;
    Quaternion rotationAtLastPacket = Quaternion.identity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!photonView.IsMine)
        {
            rb.constraints = RigidbodyConstraints.FreezeAll;
            return;
        }

        /*RaycastHit hit;
        if (Physics.Raycast(transform.position, -transform.up, out hit, 20f, whatIsGround))
        {
            transform.position = hit.point + (transform.up * 1.4f);
        }*/

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
                //agent.SetDestination(targetKart.frontPosition.position);
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
            if (!kart.kartController.PhotonView.IsMine)
            {
                return;
            }
            kart.kartController.Transform.GetComponent<PlayerScript>().GetHit(false);
            photonView.RPC("AskToDestroy", RpcTarget.All);
        }
    }

    [PunRPC]
    private void AskToDestroy()
    {
        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
            SkinManager.instance.SetCharacterHitAnimation();
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
            rotationAtLastPacket = GlobalData.FixQuaternion(rotationAtLastPacket);
            latestRot = GlobalData.FixQuaternion(latestRot);

            var time = (float)(currentTime / timeToReachGoal);
            time = Mathf.Clamp(time, 0.0f, 1.0f);
            transform.position = Vector3.Lerp(positionAtLastPacket, latestPos, time);
            transform.rotation = Quaternion.Lerp(rotationAtLastPacket, latestRot, time);
            agent.velocity = Vector3.Lerp(velocityAtLastPacket, latestVel, time);
            
        }

        if (!photonView.IsMine || (!agent.isOnNavMesh && ! agent.isOnOffMeshLink))
        {
            return;
        }

        timer += Time.deltaTime;

        if (!agent.pathPending)
        {
            if (timer >= 0.1f)
            {
                agent.destination = targetKart.frontPosition.position;
                //agent.SetDestination(targetKart.frontPosition.position);
                timer = 0f;
                return;
            }
            
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                {
                    agent.destination = targetKart.frontPosition.position;
                    //agent.SetDestination(targetKart.frontPosition.position);
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
            stream.SendNext(SerializationHelper.SerializeVector3(transform.position));
            stream.SendNext(SerializationHelper.SerializeQuaternion(transform.rotation));
            stream.SendNext(SerializationHelper.SerializeVector3(agent.velocity));
        }
        else if (stream.IsReading)
        {
            //Network player, receive data
            latestPos = SerializationHelper.DeserializeVector3((byte[])stream.ReceiveNext());
            latestRot = SerializationHelper.DeserializeQuaternion((byte[])stream.ReceiveNext());
            latestVel = SerializationHelper.DeserializeVector3((byte[])stream.ReceiveNext());

            //Lag compensation
            currentTime = 0.0f;
            lastPacketTime = currentPacketTime;
            currentPacketTime = info.SentServerTime;
            positionAtLastPacket = transform.position;
            rotationAtLastPacket = transform.rotation;
            velocityAtLastPacket = agent.velocity;
        }
    }
}
