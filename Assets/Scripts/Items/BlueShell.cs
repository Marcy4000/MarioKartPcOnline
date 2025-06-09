using System.Collections;
using UnityEngine;
using Photon.Pun;
using PathCreation;
using System.IO;

public class BlueShell : MonoBehaviourPun
{
    private KartLap targetKart;
    private PathCreator path;
    private float distanceAlongPath;
    private float speed = 200f;
    private bool targetMode = false;
    private bool animationEnded = false;
    private float t = 0;
    public float SHELL_HEIGHT = 3f;
    private const float radius = 10f;
    private bool notFirst = false;
    public Animator animator;
    public bool PlayingAnimation = false;
    private bool MustLookAt = false;
    public GameObject child;
    public LayerMask THISMASK;
    private bool once = false;

    private void Awake()
    {
        if (!photonView.IsMine)
        {
            return;
        }

        foreach (PathCreator p in FindObjectsOfType<PathCreator>())
        {
            if (p.gameObject.tag == "BlueShellPath")
            {
                path = p;
                break;
            }
        }

        distanceAlongPath = path.path.GetClosestDistanceAlongPath(transform.position) + 30f;
        transform.position = path.path.GetPointAtDistance(distanceAlongPath);
    }

    public void SetCurrentKartLap(KartLap _kart)
    {
        foreach (var kart in PlaceCounter.instance.karts)
        {
            if (kart.racePlace == 1)
            {
                targetKart = kart;
                StartCoroutine(SafeFrames());

                Debug.Log("Blue Shell found target");
                return;
            }
        }
        AskToDestroy();
    }

    private IEnumerator SafeFrames()
    {
        GetComponent<SphereCollider>().enabled = false;
        yield return new WaitForSeconds(0.2f);
        GetComponent<SphereCollider>().enabled = true;
    }

    private void Update()
    {
        if (!photonView.IsMine)
        {
            return;
        }

        if (targetMode)
        { 
            TargetMode();
            return;
        }
        targetMode = (Distance2dVector3xz(transform.position, targetKart.transform.position) < 40 && CheckLineOfSight()) ;
        distanceAlongPath += speed * Time.deltaTime;
        transform.position = path.path.GetPointAtDistance(distanceAlongPath);

        if (targetKart.racePlace != 1 || !targetKart)
        {
            foreach (var kart in PlaceCounter.instance.karts)
            {
                if (kart.racePlace == 1)
                {
                    targetKart = kart;
                    Debug.Log("blue Shell found target");
                }
            }
        }

    }

    bool CheckLineOfSight()
    {
        return !Physics.Linecast(transform.position, targetKart.transform.position + Vector3.up * SHELL_HEIGHT,THISMASK);
    }

    private void TargetMode()
    {
        if (animationEnded)
        {
            AnimationEnd();
            return;
        }

        
        float dist = Distance2dVector3xz(transform.position, targetKart.transform.position);
        if (dist < radius + 5f && notFirst)
        {
            t += speed / 20 * Time.deltaTime;
            if (t >= Mathf.PI * 4 + transform.rotation.eulerAngles.y * Mathf.Deg2Rad || MustLookAt)
            {
                t = transform.rotation.eulerAngles.y * Mathf.Deg2Rad;
                var temp = targetKart.transform.position + targetKart.transform.forward * radius;
                transform.position = temp + Vector3.up * SHELL_HEIGHT;
                transform.LookAt(targetKart.transform.position + Vector3.up * SHELL_HEIGHT);
                MustLookAt = true;
                if (!PlayingAnimation)
                {
                    PlayingAnimation = true;
                    animator.Play("rotate");
                    return;
                }
                return;
            }
            transform.position =  targetKart.transform.position + Vector3.right * Mathf.Cos(t) * radius + Vector3.forward *Mathf.Sin(t)* radius + Vector3.up * SHELL_HEIGHT; 
        }

        else
        {
            notFirst = true;
            float p = transform.position.z / dist;
            Debug.Log(p);
            float f = 1;
            Start:
            if (p > 1)
            {
                p -= 1;
                f*= -1 ;
                goto Start;    
            }
            else if( p < -1 ) 
            {
                p += 1;
                f*= -1 ;
                goto Start;
            }    
            t = Mathf.Asin(p) * f ;
            transform.LookAt(targetKart.transform.position + Vector3.up * SHELL_HEIGHT);
            transform.position += transform.forward * speed * Time.deltaTime;
        }
    }

    private void AnimationEnd()
    {
        if (!once)
        {
            child.transform.rotation = Quaternion.Euler(child.transform.rotation.eulerAngles.x, child.transform.rotation.eulerAngles.y + 90, child.transform.rotation.eulerAngles.z);
            once = true;
        }
        transform.LookAt(targetKart.transform.position);
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    public void HitAnimationEvent()
    {
        animationEnded = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponent<KartLap>().racePlace == targetKart.racePlace)
        {
            PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "Explosion"), transform.position, Quaternion.identity);
            photonView.RPC("AskToDestroy", RpcTarget.All);
        }

        if (collision.gameObject.GetComponent<PlayerScript>())
        {
            PlayerScript player = collision.gameObject.GetComponent<PlayerScript>();
            if (!player.photonView.IsMine)
            {
                return;
            }
            player.GetHit(true);

            return;
        }
       
        if (collision.gameObject.GetComponent<KartLap>())
        {
            KartLap kart = collision.gameObject.GetComponent<KartLap>();
            if (!kart.kartController.PhotonView.IsMine)
            {
                return;
            }
            kart.kartController.Transform.GetComponent<PlayerScript>().GetHit(false);
            photonView.RPC("AskToDestroy", RpcTarget.All);
        }
    }

    [PunRPC]
    private void AskToDestroy()
    {
        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
            SkinManager.instance.SetCharacterHitAnimation();
        }
    }

    float Distance2dVector3xz(Vector3 a, Vector3 b)
    {
        return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
    }
}
