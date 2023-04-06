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
    // Start is called before the first frame update

    private void Start()
    {
        renderer = GetComponent<Renderer>();
        rendererFront = FrontSphere.GetComponent<Renderer>();
        rendererBack = BackSphere.GetComponent<Renderer>();
    }
    // Update is called once per frame
    void FixedUpdate()
    {
       
        if (FrontSphere.transform.localScale.x > 0.99f)
        {
            FrontSphere.transform.localScale -= Vector3.one * explosionSpeed/10;
        }
        else
        {
            FrontSphere.transform.localScale = Vector3.one * 0.99f;
        }

        if (transform.localScale.x < ExplosionRange)
        {
            transform.localScale += Vector3.one * explosionSpeed;
        }
        else if (!alreadyExec)
        {
            
            CheckCollision();
            StartCoroutine(WaitToDestoy());
        }
        if (lightdown)
        {
            rendererFront.material.SetColor("_BaseColor", new Color(1.0f, 1.0f, 1.0f,rendererFront.material.GetColor("_BaseColor").a - 0.01f));
            rendererBack.material.SetColor("_BaseColor", new Color(1.0f, 1.0f, 1.0f, rendererBack.material.GetColor("_BaseColor").a - 0.01f));

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
    
    void CheckCollision()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position,ExplosionRange,playerMask);
        foreach (Collider c in colliders)
        {
            
        }
    }
    IEnumerator WaitToDestoy()
    {
        lightdown = true;
        yield return new WaitForSeconds(0.2f);
        dissolve = true;
    }
}
