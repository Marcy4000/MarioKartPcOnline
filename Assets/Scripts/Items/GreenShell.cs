using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class GreenShell : NetworkBehaviour
{
    private Rigidbody rb;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private float groundRayLenght;
    [SerializeField] private Transform groundRayPoint;
    [SerializeField] bool grounded;
    [SerializeField] float maxSpeed = 200f;
    [SerializeField] private float rotationSpeed;
    bool floorIsAlsoWall, hasHitWall;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!IsOwner)
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
        NetworkObject.Despawn(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall") && !floorIsAlsoWall && IsOwner)
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
            if (!player.IsOwner)
            {
                return;
            }
            player.GetHit(false);
            AskToDestroyRPC();
            return;
        }
        else if (collision.gameObject.TryGetComponent(out KartLap kart))
        {
            if (!kart.carController.IsOwner)
            {
                return;
            }
            kart.carController.GetHit();
            AskToDestroyRPC();
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!hasHitWall && collision.gameObject.CompareTag("Wall") && !floorIsAlsoWall && IsOwner)
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
        if (collision.gameObject.CompareTag("Wall") && !floorIsAlsoWall && IsOwner)
        {
            hasHitWall = false;
        }
    }

    [Rpc(SendTo.Server)]
    private void AskToDestroyRPC()
    {
        NetworkObject.Despawn(true);
        //SkinManager.instance.SetCharacterHitAnimation();
    }

    private void FixedUpdate()
    {
        if (!IsOwner)
        {
            return;
        }

        grounded = false;
        RaycastHit hit;

        Debug.DrawRay(groundRayPoint.position, -transform.up * groundRayLenght);
        if (Physics.Raycast(groundRayPoint.position, -transform.up, out hit, groundRayLenght, whatIsGround))
        {
            grounded = true;
            Quaternion newRotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, newRotation, rotationSpeed);
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
}
