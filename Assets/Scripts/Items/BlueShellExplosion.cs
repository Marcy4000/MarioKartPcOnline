using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class BlueShellExplosion : MonoBehaviour
{
    public float ExplosionRange = 10f;
    [Range(0.001f , 1.0f )] public float explosionSpeed = 0.25f;
    public bool alreadyExec = false;
    [SerializeField] LayerMask playerMask;
    // Start is called before the first frame update
   

    // Update is called once per frame
    void FixedUpdate()
    {
        
        if (transform.localScale.x < ExplosionRange)
        transform.localScale += Vector3.one * explosionSpeed;
        else if(!alreadyExec)
        {
            CheckCollision();
        }
    }
    void CheckCollision()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position,ExplosionRange,playerMask);
        foreach (Collider c in colliders)
        {
            
        }
    }
}
