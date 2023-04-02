using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;

public class SimpleBelt : MonoBehaviour
{
    [SerializeField] public float addSpeed = 1000.0f;
    List<GameObject> Colliders = new List<GameObject>();
    private void FixedUpdate()
    {
        foreach( GameObject obj in Colliders)
        {
            if (!obj)
            {
                Colliders.Remove(obj);
            }
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb) 
            {
                rb.AddForce(transform.right * addSpeed,ForceMode.Acceleration);    
            }
        }
    }
    private void OnTriggerEnter(Collider other)
    
    {
        Colliders.Add(other.gameObject);
    }
    private void OnTriggerExit(Collider other)
    
    {
       Colliders.Remove(other.gameObject); 
    }
}
