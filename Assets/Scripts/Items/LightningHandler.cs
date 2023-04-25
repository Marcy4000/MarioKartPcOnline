using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightningHandler : MonoBehaviour
{
    public static LightningHandler instance;
    public bool sender = false;

    // Start is called before the first frame update
    void Awake()
    {
        instance = this;
    }

    public void Strike()
    {
        foreach (var kart in PlaceCounter.instance.karts)
        {
            if (!kart.carController.isPlayer)
            {
                if (!kart.carController.pv.IsMine)
                {
                    continue;
                }

                kart.carController.GetHit();
            }
            else
            {
                if (!kart.photonView.IsMine)
                {
                    continue;
                }

                if (!sender)
                {
                    kart.GetComponent<PlayerScript>().GetHit(true);
                }
            }
        }

        sender = false;
    }
}
