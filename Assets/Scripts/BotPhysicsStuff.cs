using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BotPhysicsStuff : MonoBehaviour
{
    public GameObject parent;
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Mushroom"))
        {
            rb.AddForce(Vector3.up * 3500f, ForceMode.Impulse);
            rb.AddForce(parent.transform.forward * 3000f, ForceMode.Impulse);
        }
    }
}
