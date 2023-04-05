using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.AI;
using NUnit.Framework.Internal.Execution;
using PathCreation;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine.UIElements;
using Unity.Collections.LowLevel.Unsafe;
using Unity.VisualScripting;

public class BlueShell : MonoBehaviourPun
{
    private Rigidbody rb;
    private KartLap targetKart, currKart;
    private PathCreator path;
    private float distanceAlongPath;
    private float ExplodeDistance = 20f;
    private float speed = 100f;
    private bool targetMode = false;
    private bool animationEnded = false;
    private float t = 0;
    private const float radius = 10f;
    private bool notFirst = false;
    public Animator animator;
    public bool PlayingAnimation = false;
    private bool MustLookAt = false;
    public GameObject child;
    private bool once = false;
    private void Awake()
    {
        if (!photonView.IsMine)
        {
            rb.constraints = RigidbodyConstraints.FreezeAll;
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
        currKart = _kart;
        foreach (var kart in PlaceCounter.instance.karts)
        {
            if (kart.racePlace == RacePlace.first)
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
        yield return new WaitForSeconds(1);
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
        distanceAlongPath += speed * Time.deltaTime;
        transform.position = path.path.GetPointAtDistance(distanceAlongPath);

        if (targetKart.racePlace != RacePlace.first || !targetKart)
        {
            foreach (var kart in PlaceCounter.instance.karts)
            {
                if (kart.racePlace == RacePlace.first)
                {
                    targetKart = kart;
                    Debug.Log("blue Shell found target");
                }
            }
        }
        if (Distance2dVector3xz(transform.position, targetKart.transform.position) < 80) targetMode = true;

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
                transform.position =  new Vector3 (temp.x , transform.position.y, temp.z);
                transform.LookAt(new Vector3 (targetKart.transform.position.x , transform.position.y, targetKart.transform.position.z));
                MustLookAt = true;
                if (!PlayingAnimation)
                {
                    PlayingAnimation = true;
                    animator.Play("rotate");
                    return;
                }
                return;
            }
            transform.position = new Vector3(targetKart.transform.position.x + Mathf.Cos(t) * radius, transform.position.y, targetKart.transform.position.z + Mathf.Sin(t) * radius);
        }

        else
        {
            notFirst = true;
            float p = transform.position.z / dist;
            Debug.Log(p);
            if (p > 1)
            {
                t = -Mathf.Asin(p -1  );
            }
            else if( p < -1 ) 
            {
                t = -Mathf.Asin(p + 1);
            }
            else
            {
                t = Mathf.Asin(p);
            }
            Debug.Log(t);
            transform.position = new Vector3(Mathf.Lerp(transform.position.x, targetKart.transform.position.x + Mathf.Cos(t) *radius, speed * Time.deltaTime / 5), transform.position.y, Mathf.Lerp(transform.position.z, targetKart.transform.position.z + Mathf.Sin(t) * radius, speed * Time.deltaTime / 5 ));
        }
    }
    private void AnimationEnd()
    {if (!once)
        {
            child.transform.rotation = Quaternion.Euler(child.transform.rotation.eulerAngles.x, child.transform.rotation.eulerAngles.y + 90, child.transform.rotation.eulerAngles.z);
            once = true;
        }
        transform.LookAt(targetKart.transform.position);
        transform.position += transform.forward * speed * Time.deltaTime;
    }
    public void HitAnimationEvent()
    {
        this.animationEnded = true;
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponent<PlayerScript>())
        {
            PlayerScript player = collision.gameObject.GetComponent<PlayerScript>();
            if (!player.photonView.IsMine)
            {
                return;
            }
            player.GetHit(true);
            if (collision.gameObject.GetComponent<KartLap>().racePlace == targetKart.racePlace)
            {
                photonView.RPC("AskToDestroy", RpcTarget.All);
            }

            return;
        }

        if (collision.gameObject.GetComponent<KartLap>())
        {
            KartLap kart = collision.gameObject.GetComponent<KartLap>();
            if (!kart.carController.pv.IsMine)
            {
                return;
            }
            kart.carController.GetHit();
            if (kart.racePlace == targetKart.racePlace)
            {
                photonView.RPC("AskToDestroy", RpcTarget.All);
            }
        }
    }

    [PunRPC]
    private void AskToDestroy()
    {
        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
            SkinManager.instance.characters[SkinManager.instance.selectedCharacter].modelAnimator.SetTrigger("Hit");
        }
    }
    float Distance2dVector3xz(Vector3 a, Vector3 b)
    {
        return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
    }

}
