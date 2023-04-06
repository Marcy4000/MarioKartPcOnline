using PathCreation;
using Photon.Pun;
using System.IO;
using UnityEngine;

public class BulletBill : MonoBehaviourPun
{
    public PathCreator GlobalPathCreator;
    public PathCreator UsablePath;
    public PathCreator BulletBillPathCreator;
    public float speed = 100;
    public const float MAXSPEED = 100;
    float distanceTravelled;
    PlayerScript ps;
    bool UsingLocalPath = false;
    GameObject BulletBillGameObject;
    GameObject PathKeeper; 
    GameObject CartDisplayGameObject;
    public LayerMask mask;
    public float turnRate= 7.5f;
    public float MaxDistSPawnBulletBill = 2.0f;
    private Vector3 aimPoint;
    private float gdist;
    void Start()
    {
        mask = LayerMask.GetMask("Ground", "OffRoad");
        BulletBillGameObject = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "BulletBill"), new Vector3(transform.position.x + 0.11f, transform.position.y + 2.91f, transform.position.z - 1.08f), Quaternion.Euler(90, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z));
        PathKeeper = Instantiate(new GameObject("BulletPathKeeper"));
        BulletBillPathCreator = PathKeeper.AddComponent<PathCreator>();
        ps = gameObject.GetComponent<PlayerScript>();

        CartDisplayGameObject = gameObject.transform.Find("Holder").gameObject;
        CartDisplayGameObject.SetActive(false);

        foreach (PathCreator path in FindObjectsOfType<PathCreator>())
        {
            if (path.gameObject.tag == "BulletBillPath")
            {
                GlobalPathCreator = path;
                break;
            }
        }

        if (GlobalPathCreator == null)
        {
            Destroy(this);
            return;
        }

        distanceTravelled = GlobalPathCreator.path.GetClosestDistanceAlongPath(transform.position);
        gdist = distanceTravelled + 60f;
        aimPoint = GlobalPathCreator.path.GetPointAtDistance(gdist);
        Vector3 v = aimPoint - transform.position;
        float dist = Mathf.Sqrt(v.x * v.x + v.z * v.z);
        
            Vector3[] waypoints = { transform.position, aimPoint };
            BezierPath bezierPath = new BezierPath(waypoints, false, PathSpace.xz);
            bezierPath.ControlPointMode = BezierPath.ControlMode.Free;
            bezierPath.SetPoint(0, transform.position);
            bezierPath.SetPoint(3, aimPoint);
            bezierPath.SetPoint(1, bezierPath.GetPoint(0));
            bezierPath.SetPoint(2, bezierPath.GetPoint(3));

            speed = MAXSPEED;
            UsingLocalPath = true;
            distanceTravelled = 0;
            BulletBillPathCreator.bezierPath = bezierPath;
            UsablePath = BulletBillPathCreator;
    }

    void Update()
    {
        if (!ps.BulletBill || UsablePath == null)
        {
            Destroy(this);
            return;
        }

        RaycastHit hit1, hit2;
        
        distanceTravelled += speed * Time.deltaTime;
        if (UsingLocalPath && UsablePath.path.length < distanceTravelled)
        {
            speed = 100;
            UsingLocalPath = false;
            UsablePath = GlobalPathCreator;
            distanceTravelled = gdist;
        }
        Vector3 provPos = UsablePath.path.GetPointAtDistance(distanceTravelled);
        float offset = 0.8f;
        Quaternion provRot = UsablePath.path.GetRotationAtDistance(distanceTravelled);

        int iteration = 0;
    //casting a ray to determine the height of the road
    Start:
        if (!Physics.Raycast(new Vector3(provPos.x, transform.position.y + offset, provPos.z), Vector3.down, out hit1, 1000f, mask))
        {
            offset += 0.3f;
            iteration++;
            if (iteration > 30)
            {
                Debug.LogError("didn't hit");
                goto followpathcondition;
            }
            goto Start;
        }
        else
        {
            Debug.Log(hit1.collider.gameObject);
            if (Mathf.Abs(transform.position.y - hit1.point.y) > 3)
            {
                goto followpathcondition;
            }
            else
            {
                transform.position = new Vector3(provPos.x, hit1.point.y, provPos.z);
            }
        }
   
        float y = provRot.eulerAngles.y;
        
        //rotation calculation
        if (Physics.Raycast(transform.position, -transform.up, out hit2, 0.75f, mask))
        {
            Quaternion newRotation = Quaternion.FromToRotation(transform.up, hit2.normal) * transform.rotation;
            if (UsingLocalPath)
            {
                
                var localRot = GlobalPathCreator.path.GetRotationAtDistance(gdist);
                transform.rotation = Quaternion.Euler(newRotation.eulerAngles.x,Mathf.LerpAngle( y,localRot.eulerAngles.y, distanceTravelled / UsablePath.path.length), transform.rotation.eulerAngles.z);
                goto sync;
            }
            transform.rotation =Quaternion.Slerp( transform.rotation,Quaternion.Euler(newRotation.eulerAngles.x, y, transform.rotation.eulerAngles.z), turnRate * Time.deltaTime);
        }
        else
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(transform.rotation.eulerAngles.x, y, transform.rotation.eulerAngles.z), turnRate * Time.deltaTime);
        }

    //applying
    sync:
        SyncBulletBillPosition();
        return;

    followpathcondition:
        transform.position = provPos;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(provRot.eulerAngles.x, provRot.eulerAngles.y, transform.rotation.eulerAngles.z), turnRate* Time.deltaTime);
        goto sync;
    }
            
    private void SyncBulletBillPosition()
    {
        BulletBillGameObject.transform.position = new Vector3(transform.position.x, transform.position.y + 2.91f, transform.position.z - 1.08f);
        BulletBillGameObject.transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x + 90, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);

    }

    private void OnDestroy()
    {
        ps.BulletBill = false;
        PhotonNetwork.Destroy(BulletBillGameObject);
        CartDisplayGameObject.SetActive(true);

    }
}
