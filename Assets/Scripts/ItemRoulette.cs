using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class ItemSpin
{
    public Item[] items;
}

public class ItemRoulette : MonoBehaviour
{
    public Animator animator;
    public ItemSpin[] itemIcons;
    public Item selectedItem, nothing;
    public Image firstImage, secondImage;
    public AudioSource roll, decide;
    public static ItemRoulette instance;
    public bool spinning { get; private set; }

    private void Start()
    {
        instance = this;
    }

    public void UpdateItem(Item item)
    {
        selectedItem = item;
        firstImage.sprite = item.activeSprite;
    }

    public void ChangeToRandomItem()
    {
        firstImage.sprite = secondImage.sprite;
        int racePlaceIndex = Mathf.Max(0, KartLap.mainKart.racePlace - 1);
        int random = Random.Range(0, itemIcons[racePlaceIndex].items.Length);
        secondImage.sprite = itemIcons[racePlaceIndex].items[random].activeSprite;
    }

    public Item GetRandomItem(KartLap lap)
    {
        int racePlaceIndex = Mathf.Max(0, lap.racePlace - 1);
        int random = Random.Range(0, itemIcons[racePlaceIndex].items.Length);
        return itemIcons[racePlaceIndex].items[random];
    }

    public void Spin()
    {
        if (spinning || selectedItem != nothing) { return; }
        int racePlaceIndex = Mathf.Max(0, KartLap.mainKart.racePlace - 1);
        int random = Random.Range(0, itemIcons[racePlaceIndex].items.Length);
        selectedItem = itemIcons[racePlaceIndex].items[random];
        StartCoroutine(DoTheSpinning());
    }

    IEnumerator DoTheSpinning()
    {
        spinning = true;
        animator.Play("Spin");
        roll.Play();
        animator.speed = 3;
        yield return new WaitForSeconds(0.5f);
        animator.speed = 2.5f;
        yield return new WaitForSeconds(0.5f);
        animator.speed = 2;
        yield return new WaitForSeconds(0.5f);
        animator.speed = 1.5f;
        yield return new WaitForSeconds(0.5f);
        animator.speed = 1;
        yield return new WaitForSeconds(0.8f);
        animator.speed = 0.5f;
        yield return new WaitForSeconds(0.65f);
        animator.Play("Static");
        firstImage.sprite = selectedItem.activeSprite;
        roll.Stop();
        decide.Play();
        animator.speed = 1;
        spinning = false;
    }
}
