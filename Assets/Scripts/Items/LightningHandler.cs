using System.Collections;
using UnityEngine;

public class LightningHandler : MonoBehaviour
{
    public static LightningHandler instance;
    public bool sender = false;
    public AudioSource[] soundEffects;

    // Start is called before the first frame update
    void Awake()
    {
        instance = this;
    }

    public void Strike()
    {
        soundEffects[0].Play();
        foreach (var kart in PlaceCounter.instance.karts)
        {
            if (!kart.carController.isPlayer)
            {
                if (!kart.carController.IsOwner)
                {
                    continue;
                }

                kart.carController.GetHit();
            }
            else
            {
                if (!kart.IsOwner)
                {
                    continue;
                }

                if (!sender)
                {
                    soundEffects[1].Play();
                    kart.GetComponent<PlayerScript>().GetHit(true);
                    StartCoroutine("ThunderAnimation", kart);
                }
            }
        }

        sender = false;
    }

    private IEnumerator ThunderAnimation(KartLap _kart)
    {
        GameObject thunder = _kart.transform.Find("Thunder").gameObject;
        thunder.SetActive(true);
        yield return new WaitForSeconds(1.4f);
        thunder.SetActive(false);
    }
}
