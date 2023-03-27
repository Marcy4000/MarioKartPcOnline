using Mono.Cecil.Cil;
using NUnit.Framework.Constraints;
using PathCreation;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
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
    public LayerMask mask;

    // Start is called before the first frame update

    void Start()
    {
        BulletBillGameObject = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "BulletBill"), new Vector3(transform.position.x +0.11f, transform.position.y+2.91f , transform.position.z-1.08f), Quaternion.Euler(90, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z));
        ps = gameObject.GetComponent<PlayerScript>();
        CartDisplayGameObject = gameObject.transform.Find("Holder").gameObject;
        BulletBillGameObject.transform.SetParent(gameObject.transform,true);
        CartDisplayGameObject.SetActive(false);
        pathCreator = FindObjectsOfType<PathCreator>()[0];
        if (pathCreator == null)
        {
            Destroy(this);
            return;
        }
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

        RaycastHit hit1, hit2 ;

        distanceTravelled += speed * Time.deltaTime;
        
        Vector3 provPos = pathCreator.path.GetPointAtDistance(distanceTravelled);
        float offset = 0.5f;
        int iteration = 0;
        mask = LayerMask.GetMask("Ground","OffRoad");
    start:
        //casting a ray to determine the height of the road
        //Debug.DrawRay(new Vector3(provPos.x, transform.position.y + offset, provPos.z), Vector3.down,Color.red,20,false);
        if (!Physics.Raycast(new Vector3(provPos.x, transform.position.y+offset, provPos.z), Vector3.down, out hit1, 1000f,mask))
        { 
            iteration++;
            offset += 0.1f;
            if (iteration > 100)
            {
                
                Debug.LogError("didn't hit anything\n");
                return;
            }
            goto start;
        }    
        
        Quaternion provRot = pathCreator.path.GetRotationAtDistance(distanceTravelled);
        
        float y = provRot.eulerAngles.y;

        //rotation calculation
        if (!Physics.Raycast(transform.position, -transform.up, out hit2, 0.75f, mask))
        {

        }
            Quaternion newRotation = Quaternion.FromToRotation(transform.up, hit2.normal) * transform.rotation;

        //applying
        transform.rotation = Quaternion.Euler(newRotation.eulerAngles.x, y, transform.rotation.eulerAngles.z);
        transform.position = new Vector3(provPos.x, hit1.point.y+0.25f, provPos.z);
        SyncBulletBillPosition();
         
    }
            
    private void SyncBulletBillPosition()
    {
        BulletBillGameObject.transform.position = new Vector3(transform.position.x + 0.11f, transform.position.y + 2.91f, transform.position.z - 1.08f);
        BulletBillGameObject.transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x + 90, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);

    }

    private void OnDestroy()
    {
        ps.BulletBill = false;
        PhotonNetwork.Destroy(BulletBillGameObject);
        CartDisplayGameObject.SetActive(true);

    }
}
