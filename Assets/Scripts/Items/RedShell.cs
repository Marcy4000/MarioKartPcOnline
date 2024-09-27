using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class RedShell : NetworkBehaviour
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
        if (!IsOwner)
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
            if (kart.RacePlace == currKart.RacePlace - 1)
            {
                targetKart = kart;
                agent.destination = targetKart.frontPosition.position;
                //agent.SetDestination(targetKart.frontPosition.position);
                StartCoroutine(SafeFrames());
                
                Debug.Log("Red Shell found target");
                return;
            }
        }
        AskToDestroyRPC();
    }


    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponent<PlayerScript>())
        {
            PlayerScript player = collision.gameObject.GetComponent<PlayerScript>();
            if (!player.IsOwner)
            {
                return;
            }
            player.GetHit(false);
            //photonView.RPC("AskToDestroy", RpcTarget.All);
            return;
        }

        if (collision.gameObject.TryGetComponent(out KartLap kart))
        {
            if (!kart.carController.IsOwner)
            {
                return;
            }
            kart.carController.GetHit();
            //photonView.RPC("AskToDestroy", RpcTarget.All);
        }
    }

    [Rpc(SendTo.Server)]
    private void AskToDestroyRPC()
    {
        NetworkObject.Despawn(true);
        //SkinManager.instance.SetCharacterHitAnimation(); This one hurts
    }

    private void Update()
    {
        if (!IsOwner)
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

        if (!IsOwner || (!agent.isOnNavMesh && ! agent.isOnOffMeshLink))
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
}
