using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class BlueShell : MonoBehaviourPun
{
    private Rigidbody rb;
    public Transform target;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (!photonView.IsMine)
        {
            rb.constraints = RigidbodyConstraints.FreezeAll;
            return;
        }
        rb.AddForce(transform.forward * 1200f, ForceMode.Impulse);
    }

    private void Update()
    {
        if (!photonView.IsMine)
        {
            return;
        }

        if (target != null)
        {
            transform.LookAt(target, Vector3.up);
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

    private void FixedUpdate()
    {
        if (!photonView.IsMine)
        {
            return;
        }

        rb.AddForce(2200f * transform.forward);
    }
}
