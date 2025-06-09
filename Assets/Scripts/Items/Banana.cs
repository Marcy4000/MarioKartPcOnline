using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.ProBuilder;

public class Banana : MonoBehaviour
{
    PhotonView pv;
    [SerializeField] LayerMask whatIsGround;

    private void Start()
    {
        pv = GetComponent<PhotonView>();
        RaycastHit hit;
        if (Physics.Raycast(transform.position, -transform.up, out hit, 1000f, whatIsGround))
        {
            transform.position = hit.point;
            transform.rotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
        }
        else
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.None;
        }
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
            player.GetHit(true);
            pv.RPC("AskToDestroy", RpcTarget.All);
            return;
        }

        if (collision.gameObject.GetComponent<KartLap>())
        {
            KartLap kart = collision.gameObject.GetComponent<KartLap>();
            if (!kart.kartController.PhotonView.IsMine)
            {
                return;
            }
            kart.kartController.Transform.GetComponent<PlayerScript>().GetHit(true);
            pv.RPC("AskToDestroy", RpcTarget.All);
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
}
