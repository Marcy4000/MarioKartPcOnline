using NUnit.Framework.Constraints;
using PathCreation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BulletBill : MonoBehaviour
{
    public PathCreator pathCreator;
    public float speed = 100;
    float distanceTravelled;
    PlayerScript ps;
    // Start is called before the first frame update
    void Start()
    {
        ps = gameObject.GetComponent<PlayerScript>();
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
}
