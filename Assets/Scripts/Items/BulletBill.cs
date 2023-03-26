using NUnit.Framework.Constraints;
using PathCreation;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BulletBill : MonoBehaviourPun
{
    public PathCreator pathCreator;
    public float speed = 100;
    float distanceTravelled;
    PlayerScript ps;
    GameObject BulletBillGameObject;
    GameObject CartDisplayGameObject;
    // Start is called before the first frame update
    void Start()
    {
        if (!photonView.IsMine)
        {
            return;
        }
        ps = gameObject.GetComponent<PlayerScript>();
        BulletBillGameObject = gameObject.transform.Find("BulletBill").gameObject;
        CartDisplayGameObject = gameObject.transform.Find("Holder").gameObject;
        BulletBillGameObject.SetActive(true);
        CartDisplayGameObject.SetActive(false);
        pathCreator = FindObjectsOfType<PathCreator>()[0];
        if (pathCreator == null)
        {
            ps.BulletBill = false;
            Destroy(this);
            return;
        }

        distanceTravelled = pathCreator.path.GetClosestDistanceAlongPath(transform.position);
    }

    // Update is called once per frame
    void Update()
    {
        if (!ps.BulletBill)
        {
            Destroy(this);
            return;
        }
        distanceTravelled += speed * Time.deltaTime;
        
            Vector3 provPos = pathCreator.path.GetPointAtDistance(distanceTravelled);
        transform.position = new Vector3(provPos.x, transform.position.y, provPos.z);
            
            Quaternion provRot = pathCreator.path.GetRotationAtDistance(distanceTravelled);
        float y = provRot.eulerAngles.y;
        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x,y,transform.rotation.eulerAngles.z);
        
    }
    private void OnDestroy()
    {
        BulletBillGameObject.SetActive(false);
        CartDisplayGameObject.SetActive(true);

    }
}
