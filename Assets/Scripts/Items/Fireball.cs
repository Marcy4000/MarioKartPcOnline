using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Fireball : MonoBehaviour
{
    private Rigidbody rb;
    private PhotonView pv;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponent<KartLap>())
        {
            KartLap kart = collision.gameObject.GetComponent<KartLap>();
            if (!kart.kartController.PhotonView.IsMine)
            {
                return;
            }
            kart.kartController.Rigidbody.velocity = Vector3.zero;
            kart.kartController.Rigidbody.angularVelocity = Vector3.zero;
            kart.kartController.Rigidbody.AddForce(transform.up * 1200f, ForceMode.Impulse);
            pv.RPC("AskToDestroy", RpcTarget.All);
        }
        else if (collision.collider.CompareTag("Wall") && pv.IsMine)
        {
            pv.RPC("AskToDestroy", RpcTarget.All);
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        pv = GetComponent<PhotonView>();
        if (!pv.IsMine)
        {
            rb.constraints = RigidbodyConstraints.FreezeAll;
            return;
        }
        StartCoroutine(Timer());
    }

    [PunRPC]
    private void AskToDestroy()
    {
        if (pv.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    IEnumerator Timer()
    {
        yield return new WaitForSeconds(4f);
        pv.RPC("AskToDestroy", RpcTarget.All);
    }
    
    private void FixedUpdate()
    {
        if (pv.IsMine)
        {
            rb.AddForce(16000f * transform.forward);
        }
    }
}
