using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Fireball : NetworkBehaviour
{
    private Rigidbody rb;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent(out KartLap kart))
        {
            if (!kart.carController.IsOwner)
            {
                return;
            }
            kart.carController.theRB.velocity = Vector3.zero;
            kart.carController.theRB.angularVelocity = Vector3.zero;
            kart.carController.theRB.AddForce(transform.up * 1200f, ForceMode.Impulse);
            AskToDestroyRPC();
        }
        else if (collision.collider.CompareTag("Wall") && IsOwner)
        {
            AskToDestroyRPC();
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!IsOwner)
        {
            rb.constraints = RigidbodyConstraints.FreezeAll;
            return;
        }
        StartCoroutine(Timer());
    }

    [Rpc(SendTo.Server)]
    private void AskToDestroyRPC()
    {
        NetworkObject.Despawn(true);
    }

    IEnumerator Timer()
    {
        yield return new WaitForSeconds(4f);
        AskToDestroyRPC();
    }
    
    private void FixedUpdate()
    {
        if (IsOwner)
        {
            rb.AddForce(16000f * transform.forward);
        }
    }
}
