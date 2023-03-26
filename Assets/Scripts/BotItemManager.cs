using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.IO;

public class BotItemManager : MonoBehaviourPun
{
    private Item selectedItem;
    public bool canUseItem = true;
    [SerializeField] private Item nothing;
    [SerializeField] KartLap kart;
    [SerializeField] private Transform fireballSpawnPos, bananaSpawnPos;
    int amount = 0;

    private void Start()
    {
        selectedItem = nothing;
        if (!photonView.IsMine)
        {
            enabled = false;
        }
    }

    public void SelectItem()
    {
        if (selectedItem != nothing) { return; }

        int random = Random.Range(0, ItemRoulette.instance.itemIcons[(int)KartLap.mainKart.racePlace].items.Length);
        selectedItem = ItemRoulette.instance.itemIcons[(int)KartLap.mainKart.racePlace].items[random];
        amount = selectedItem.amount;
        StartCoroutine(ThinkUseItem());
    }

    IEnumerator ThinkUseItem()
    {
        while (selectedItem != nothing)
        {
            yield return new WaitForSeconds(Random.Range(6f, 14f));
            UseItem();
        }
    }

    private void UseItem()
    {
        switch (selectedItem.itemType)
        {
            case Items.mushroom:
                if (kart.carController.grounded)
                {
                    kart.carController.theRB.AddForce(kart.carController.transform.forward * 4000f, ForceMode.Impulse);
                }
                break;
            case Items.greenShell:
                PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "GreenShell"), fireballSpawnPos.position, transform.rotation);
                break;
            case Items.redShell:
                GameObject redShell = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "NewRedShell"), fireballSpawnPos.position, transform.rotation);
                RedShell script = redShell.GetComponent<RedShell>();
                script.SetCurrentKartLap(kart);
                break;
            case Items.blueShell:
                KartLap targetKart = PlaceCounter.instance.karts[0];
                foreach (var _kart in PlaceCounter.instance.karts)
                {
                    if (_kart.racePlace == RacePlace.first)
                    {
                        targetKart = _kart;
                        break;
                    }
                }
                GameObject blueShell = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "BlueShell"), targetKart.shellBackSPos.position, targetKart.transform.rotation * new Quaternion(0f, 1f, 0f, 0f));
                BlueShell blueScript = blueShell.GetComponent<BlueShell>();
                blueScript.target = targetKart.transform;
                break;
            case Items.banana:
                PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "Banana"), bananaSpawnPos.position, transform.rotation);
                break;
            case Items.star:
                redShell = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "NewRedShell"), fireballSpawnPos.position, transform.rotation);
                script = redShell.GetComponent<RedShell>();
                script.SetCurrentKartLap(kart);
                break;
            case Items.fireFlower:
                PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "Fireball"), fireballSpawnPos.position, transform.rotation);
                break;
            case Items.bulletBill:
                targetKart = PlaceCounter.instance.karts[0];
                foreach (var _kart in PlaceCounter.instance.karts)
                {
                    if (_kart.racePlace == RacePlace.first)
                    {
                        targetKart = _kart;
                        break;
                    }
                }
                blueShell = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "BlueShell"), targetKart.shellBackSPos.position, targetKart.transform.rotation * new Quaternion(0f, 1f, 0f, 0f));
                blueScript = blueShell.GetComponent<BlueShell>();
                blueScript.target = targetKart.transform;
                break;
            case Items.blooper:
                photonView.RPC("UseBlooper", RpcTarget.All, PhotonNetwork.LocalPlayer, kart.racePlace);
                break;
        }
        amount--;
        if (amount <= 0)
        {
            selectedItem = nothing;
        }
    }

    [PunRPC]
    public void UseBlooper(Photon.Realtime.Player sender, RacePlace racePlace)
    {
        Blooper.insance.Splat(racePlace);
    }
}
