using Mono.Cecil.Cil;
using NUnit.Framework.Constraints;
using PathCreation;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BulletBill : MonoBehaviourPun
{
    public PathCreator pathCreator;
    public float speed = 100;
    float distanceTravelled;
    PlayerScript ps;
    private bool firstcicle = true;
    GameObject BulletBillGameObject;
    GameObject CartDisplayGameObject;
    // Start is called before the first frame update
    
    void Start()
    {
        Debug.Log("START Function");
        BulletBillGameObject = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "BulletBill"), new Vector3(transform.position.x +0.11f, transform.position.y+2.91f , transform.position.z-1.08f), Quaternion.Euler(90, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z));
        ps = gameObject.GetComponent<PlayerScript>();
        CartDisplayGameObject = gameObject.transform.Find("Holder").gameObject;
        //BulletBillGameObject.transform.SetParent(gameObject.transform,true);
       // CartDisplayGameObject.SetActive(false);
        CartDisplayGameObject.transform.position = new Vector3(CartDisplayGameObject.transform.position.x, CartDisplayGameObject.transform.position.y-20f ,CartDisplayGameObject.transform.position.z);
        pathCreator = FindObjectsOfType<PathCreator>()[0];
        if (pathCreator == null)
        {
            Destroy(this);
            return;
        }
        firstcicle = false;
        distanceTravelled = pathCreator.path.GetClosestDistanceAlongPath(transform.position);
    }

    // Update is called once per frame
    void Update()
    {
        if (!ps.BulletBill || pathCreator == null)
        {
           
            Destroy(this);
            return;
        }
        if (firstcicle)
        {
            CartDisplayGameObject.transform.position = new Vector3(CartDisplayGameObject.transform.position.x, CartDisplayGameObject.transform.position.y - 20f, CartDisplayGameObject.transform.position.z);
            firstcicle = false;
        }

        distanceTravelled += speed * Time.deltaTime;
        Vector3 provPos = pathCreator.path.GetPointAtDistance(distanceTravelled);
        transform.position = new Vector3(provPos.x, transform.position.y, provPos.z);

        Quaternion provRot = pathCreator.path.GetRotationAtDistance(distanceTravelled);
        float y = provRot.eulerAngles.y;
        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, y, transform.rotation.eulerAngles.z);
        BulletBillGameObject.transform.position = new Vector3(transform.position.x + 0.11f, transform.position.y + 2.91f, transform.position.z - 1.08f);
        BulletBillGameObject.transform.rotation = Quaternion.Euler(90, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);
        
    }
    private void OnDestroy()
    {
        ps.BulletBill = false;
        CartDisplayGameObject.transform.position = new Vector3(CartDisplayGameObject.transform.position.x, CartDisplayGameObject.transform.position.y + 20f, CartDisplayGameObject.transform.position.z);
        PhotonNetwork.Destroy(BulletBillGameObject);
        CartDisplayGameObject.SetActive(true);

    }
}
