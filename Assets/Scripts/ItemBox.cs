using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemBox : MonoBehaviour
{
    public GameObject thing;
    public AudioSource breakSound;
    [SerializeField] private BoxCollider boxCollider;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<KartLap>())
        {
            KartLap kart = other.GetComponent<KartLap>();
            breakSound.Play();
            thing.SetActive(false);
            boxCollider.enabled = false;
            if (kart.carController.pv.IsMine && !kart.carController.botDrive)
            {
                ItemRoulette.instance.Spin();
            }
            else if (kart.carController.pv.IsMine && kart.carController.botDrive && !kart.carController.bulletBil && !kart.hasFinished)
            {
                kart.carController.gameObject.GetComponent<BotItemManager>().SelectItem();
            }
            StartCoroutine(WaitThing());
        }
    }

    IEnumerator WaitThing()
    {
        yield return new WaitForSeconds(1.7f);
        thing.SetActive(true);
        boxCollider.enabled = true;
    }
}
