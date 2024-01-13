using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class GreenShell : MonoBehaviour, IPunObservable
{
    private Rigidbody rb;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private float groundRayLenght;
    [SerializeField] private Transform groundRayPoint;
    [SerializeField] bool grounded;
    [SerializeField] float maxSpeed = 200f;
    PhotonView pv;
    bool floorIsAlsoWall, hasHitWall;

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
        pv = GetComponent<PhotonView>();
        if (!pv.IsMine)
        {
            rb.constraints = RigidbodyConstraints.FreezeAll;
            return;
        }
        rb.AddForce(transform.forward * 3000f, ForceMode.Impulse);
        StartCoroutine(Timer());
    }

    IEnumerator Timer()
    {
        yield return new WaitForSeconds(50f);
        PhotonNetwork.Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall") && !floorIsAlsoWall && pv.IsMine)
        {
            Vector3 v = Vector3.Reflect(transform.forward, collision.GetContact(0).normal);
            transform.rotation = Quaternion.FromToRotation(Vector3.forward, v);
            transform.rotation = new Quaternion(0, transform.rotation.y, 0, transform.rotation.w);
            rb.AddForce(transform.forward * 2500f, ForceMode.Impulse);
            hasHitWall = true;
        }
        else if (collision.gameObject.GetComponent<PlayerScript>())
        {
            PlayerScript player = collision.gameObject.GetComponent<PlayerScript>();
            if (!player.photonView.IsMine)
            {
                return;
            }
            player.GetHit(false);
            pv.RPC("AskToDestroy", RpcTarget.All);
            return;
        }
        else if (collision.gameObject.GetComponent<KartLap>())
        {
            KartLap kart = collision.gameObject.GetComponent<KartLap>();
            if (!kart.carController.pv.IsMine)
            {
                return;
            }
            kart.carController.GetHit();
            pv.RPC("AskToDestroy", RpcTarget.All);
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!hasHitWall && collision.gameObject.CompareTag("Wall") && !floorIsAlsoWall && pv.IsMine)
        {
            Vector3 v = Vector3.Reflect(transform.forward, collision.GetContact(0).normal);
            transform.rotation = Quaternion.FromToRotation(Vector3.forward, v);
            transform.rotation = new Quaternion(0, transform.rotation.y, 0, transform.rotation.w);
            rb.AddForce(transform.forward * 2500f, ForceMode.Impulse);
            hasHitWall = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall") && !floorIsAlsoWall && pv.IsMine)
        {
            hasHitWall = false;
        }
    }

    [PunRPC]
    private void AskToDestroy()
    {
        if (pv.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
            SkinManager.instance.SetCharacterHitAnimation();
        }
    }

    private void Update()
    {
        if (!pv.IsMine)
        {
            //Lag compensation
            double timeToReachGoal = currentPacketTime - lastPacketTime;
            currentTime += Time.deltaTime;

            //Update remote player
            rotationAtLastPacket = GlobalData.FixQuaternion(rotationAtLastPacket);
            latestRot = GlobalData.FixQuaternion(latestRot);

            var time = (float)(currentTime / timeToReachGoal);
            //time = Mathf.Clamp01(time);
            time = Mathf.Clamp01((float)(time + Time.deltaTime / timeToReachGoal));
            transform.position = Vector3.Lerp(positionAtLastPacket, latestPos, time);
            transform.rotation = Quaternion.Lerp(rotationAtLastPacket, latestRot, time);
            rb.velocity = Vector3.Lerp(velocityAtLastPacket, latestVel, time);
        }
    }

    private void FixedUpdate()
    {
        if (!pv.IsMine)
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

        if (hit.collider != null && hit.collider.CompareTag("Wall"))
        {
            floorIsAlsoWall = true;
        }
        else
        {
            floorIsAlsoWall = false;
        }

        if (!grounded)
        {
            rb.AddForce(-5500f * Vector3.up);
        }
        rb.AddForce(2500f * transform.forward);

        if (rb.velocity.magnitude > maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed;
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            //We own this player: send the others our data
            stream.SendNext(SerializationHelper.SerializeVector3(transform.position));
            stream.SendNext(SerializationHelper.SerializeQuaternion(transform.rotation));
            stream.SendNext(SerializationHelper.SerializeVector3(rb.velocity));
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
            velocityAtLastPacket = rb.velocity;
        }
    }
}
