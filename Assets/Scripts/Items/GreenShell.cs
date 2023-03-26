using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class GreenShell : MonoBehaviour
{
    private Rigidbody rb;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private float groundRayLenght;
    [SerializeField] private Transform groundRayPoint;
    [SerializeField] bool grounded;
    [SerializeField] float maxSpeed = 200f;
    PhotonView pv;
    bool bitch, thing;

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
        yield return new WaitForSeconds(50);
        PhotonNetwork.Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall") && !bitch && pv.IsMine)
        {
            Vector3 v = Vector3.Reflect(transform.forward, collision.GetContact(0).normal);
            transform.rotation = Quaternion.FromToRotation(Vector3.forward, v);
            transform.rotation = new Quaternion(0, transform.rotation.y, 0, transform.rotation.w);
            rb.AddForce(transform.forward * 2500f, ForceMode.Impulse);
            thing = true;
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
        if (!thing && collision.gameObject.CompareTag("Wall") && !bitch && pv.IsMine)
        {
            Vector3 v = Vector3.Reflect(transform.forward, collision.GetContact(0).normal);
            transform.rotation = Quaternion.FromToRotation(Vector3.forward, v);
            transform.rotation = new Quaternion(0, transform.rotation.y, 0, transform.rotation.w);
            rb.AddForce(transform.forward * 2500f, ForceMode.Impulse);
            thing = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall") && !bitch && pv.IsMine)
        {
            thing = false;
        }
    }

    [PunRPC]
    private void AskToDestroy()
    {
        if (pv.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
            SkinManager.instance.characters[SkinManager.instance.selectedCharacter].modelAnimator.SetTrigger("Hit");
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
            bitch = true;
        }
        else
        {
            bitch = false;
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
}
