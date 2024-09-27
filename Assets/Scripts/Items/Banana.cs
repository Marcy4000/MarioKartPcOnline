using Unity.Netcode;
using UnityEngine;

public class Banana : NetworkBehaviour
{
    [SerializeField] LayerMask whatIsGround;

    private void Start()
    {
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
            if (!player.IsOwner)
            {
                return;
            }
            player.GetHit(true);
            AskToDestroyRPC();
            return;
        }

        if (collision.gameObject.TryGetComponent(out KartLap kart))
        {
            if (!kart.carController.IsOwner)
            {
                return;
            }
            kart.carController.GetHit();
            AskToDestroyRPC();
        }
    }

    [Rpc(SendTo.Server)]
    private void AskToDestroyRPC()
    {
        NetworkObject.Despawn(gameObject);
        //SkinManager.instance.SetCharacterHitAnimation();
    }
}
