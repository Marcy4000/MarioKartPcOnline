using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.AI;
using NUnit.Framework.Internal.Execution;

public class BlueShell : MonoBehaviourPun
{
    private Rigidbody rb;
    private KartLap targetKart, currKart;
    private NavMeshAgent agent;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        if (!photonView.IsMine)
        {
            rb.constraints = RigidbodyConstraints.FreezeAll;
            return;
        }
        rb.AddForce(transform.forward * 1200f, ForceMode.Impulse);
    }
    public void SetCurrentKartLap(KartLap _kart)
    {
        currKart = _kart;
        foreach (var kart in PlaceCounter.instance.karts)
        {
            if (kart.racePlace == RacePlace.first)
            {
                targetKart = kart;
                agent.destination = targetKart.frontPosition.position;
                StartCoroutine(SafeFrames());

                Debug.Log("Blue Shell found target");
                return;
            }
        }
        AskToDestroy();
    }
    private IEnumerator SafeFrames()
    {
        GetComponent<SphereCollider>().enabled = false;
        yield return new WaitForSeconds(1);
        GetComponent<SphereCollider>().enabled = true;
    }
    private void Update()
    {
        if (!photonView.IsMine || (!agent.isOnNavMesh && !agent.isOnOffMeshLink))
        {
            return;
        }

        if (targetKart.racePlace != RacePlace.first)
        {
            foreach (var kart in PlaceCounter.instance.karts)
            {
                if (kart.racePlace == RacePlace.first)
                {
                    targetKart = kart;
                    agent.destination = targetKart.frontPosition.position;
                    Debug.Log("blue Shell found target");
                }
            }
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
            if (collision.gameObject.GetComponent<KartLap>().racePlace == RacePlace.first)
            {
                    photonView.RPC("AskToDestroy", RpcTarget.All);
            }

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
            if (kart.racePlace == RacePlace.first)
            {
                photonView.RPC("AskToDestroy", RpcTarget.All);
            }
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
    }
}
