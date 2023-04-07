using Photon.Pun;
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
    public GameObject FrontSphere;
    public GameObject BackSphere;
    Renderer renderer;
    public Light light1;
    public Light light2;
    Renderer rendererFront;
    Renderer rendererBack;
    bool dissolve = false;
    bool lightdown = false;
    public PhotonView pv;
    // Start is called before the first frame update

    private void Start()
    {
        renderer = GetComponent<Renderer>();
        rendererFront = FrontSphere.GetComponent<Renderer>();
        rendererBack = BackSphere.GetComponent<Renderer>();
        StartCoroutine(DeleteFront());
        CheckCollision();

    }
    // Update is called once per frame
    void FixedUpdate()
    {

        if (FrontSphere  && FrontSphere.transform.localScale.x > 0.99f)
        {
            FrontSphere.transform.localScale -= Vector3.one * explosionSpeed / 10;
        }
        else if (FrontSphere)
        {
            FrontSphere.transform.localScale = Vector3.one * 0.99f;
        }
        if (BackSphere && BackSphere.transform.localScale.x > 0.99f)
        {
            BackSphere.transform.localScale -= Vector3.one * explosionSpeed / 10;
        }
        else if (BackSphere)
        {
            BackSphere.transform.localScale = Vector3.one * 0.99f;
        }
        if (transform.localScale.x < ExplosionRange)
        {
            transform.localScale += Vector3.one * explosionSpeed;
        }
        else if (!alreadyExec)
        {
            
            
            StartCoroutine(WaitToDestoy());
            alreadyExec = true;
        }
        if (lightdown)
        {
            light1.intensity -= 1.0f;
            light2.intensity -= 1.0f;
            if (light1.intensity < 20)
            {
                Destroy(FrontSphere.gameObject);
            }

        }
        if (dissolve) 
        {
            
            float currentProgress = renderer.material.GetFloat("_Progress");
            if (currentProgress > 1)
            {
                Destroy(this.gameObject);
                return;
            }
            renderer.material.SetFloat("_Progress", currentProgress + 0.02f);    
        }

        
    }
    IEnumerator DeleteFront()
    {
        yield return new WaitForSeconds(0.2f);
        Destroy(FrontSphere.gameObject);
    }
    void CheckCollision()
    {
        if (!pv.IsMine) return;   
        Collider[] colliders = Physics.OverlapSphere(transform.position,ExplosionRange,playerMask);
        foreach (Collider c in colliders)
        {
            var pv = c.gameObject.GetComponent<PhotonView>();
            var ps = c.gameObject.GetComponent<PlayerScript>();
            if (pv) {
                if (ps)
                {
                    pv.RPC("PlayerGetHitRPC", RpcTarget.All, false);
                }
                else
                {
                    pv.RPC("BotGetHitRPC", RpcTarget.All, false);
                }
                
            }
        }
    }
    IEnumerator WaitToDestoy()
    {
        lightdown = true;
        yield return new WaitForSeconds(0.2f);
        Destroy(BackSphere);
        dissolve = true;
    }
}
