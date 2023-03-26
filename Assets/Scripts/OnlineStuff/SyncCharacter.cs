using UnityEngine;
using Photon.Pun;
using System.IO;

public class SyncCharacter : MonoBehaviourPun, IPunObservable
{
    private Vector3 latestPos;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (photonView.IsMine)
        {
            // Update position on the local player
            latestPos = transform.position;
        }
        else
        {
            // Update position on other players using predicted movement and interpolation
            transform.position = Vector3.Lerp(transform.position, latestPos, Time.deltaTime * 10);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Write position to the network
            stream.SendNext(rb.position);
            stream.SendNext(rb.velocity);
        }
        else
        {
            // Read position from the network
            Vector3 receivedPosition = (Vector3)stream.ReceiveNext();
            Vector3 receivedVelocity = (Vector3)stream.ReceiveNext();

            float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
            receivedPosition += receivedVelocity * lag;

            // Update latest position with the received position
            latestPos = receivedPosition;
        }
    }
}
