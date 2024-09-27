using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class moveCrane : NetworkBehaviour
{
    Vector3 startingPosition;
    private NetworkVariable<float> speed = new NetworkVariable<float>(0.3f);
    private NetworkVariable<bool> canGo = new NetworkVariable<bool>(true);
    List<GameObject > Updatable = new List< GameObject>();

    void Start()
    {
        startingPosition = transform.position;
    }

    void FixedUpdate()
    {
        if (!canGo.Value ) { return; }

        if (IsOwner)
        {
            transform.position += Vector3.right * speed.Value;
        }

        foreach (GameObject updateObj in Updatable) {
            if (updateObj == null) continue;
            Debug.Log(updateObj.name);
            NetworkObject pvo = updateObj.GetComponent<NetworkObject>();
            if (pvo != null && pvo.IsOwner)
            {               
                updateObj.transform.position += Vector3.right * speed.Value;
            }
        }
        
        if (Mathf.Abs (transform.position.x - startingPosition.x ) > 31)
        {
            speed.Value *= -1;
            StartCoroutine(Wait5());
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Updatable.Add(collision.collider.gameObject);
    }

    private void OnCollisionExit(Collision collision)
    {
        Updatable.Remove(collision.collider.gameObject);
    }

    IEnumerator Wait5()
    {
        canGo.Value = false;
        yield return new WaitForSeconds(5f);
        canGo.Value = true;
    }
}
