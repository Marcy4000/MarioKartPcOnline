using NUnit.Framework.Constraints;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class moveCrane : MonoBehaviour
{
    Vector3 startingPosition;
    float speed = 0.3f;
    bool canGo = true;
    PhotonView pv;
    List<GameObject > Updatable = new List< GameObject>();
    // Start is called before the first frame update
    void Start()
    {
        pv = GetComponent<PhotonView>();    
        startingPosition = transform.position;
    }

    // Update is called once per frame
    
    void FixedUpdate()
    {
        
        if (!canGo ) { return; }
        if (pv.IsMine)
        {
            transform.position += Vector3.right * speed;
        }
        foreach (GameObject updateObj in Updatable) {
            if (updateObj == null) continue;
            Debug.Log(updateObj.name);
            PhotonView pvo = updateObj.GetComponent<PhotonView>();
            if (pvo != null && pvo.IsMine)
            {               
                updateObj.transform.position += Vector3.right * speed;
            }
        }
        
        if (Mathf.Abs (transform.position.x - startingPosition.x ) > 31)
        {
            
            speed *= -1;
            
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
        canGo = false;
        pv.RPC("syncVariable", RpcTarget.All, speed,canGo);
        yield return new WaitForSeconds(5f);
        canGo = true;
        pv.RPC("syncVariable", RpcTarget.All, speed, canGo);
    }
    [PunRPC]
     public void syncVariable(float speed, bool canGo )
    {
        this.canGo = canGo; 
        this.speed = speed;
    }
    
}
