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

        selectedItem = ItemRoulette.instance.GetRandomItem(this.kart);
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
                if (kart.kartController.IsGrounded)
                {
                    kart.kartController.Rigidbody.AddForce(kart.kartController.Transform.forward * 4000f, ForceMode.Impulse);
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

                GameObject blueShell = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "BlueShell"), fireballSpawnPos.position, transform.rotation);
                BlueShell blueScript = blueShell.GetComponent<BlueShell>();
                blueScript.SetCurrentKartLap(kart);
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
                PlayerScript botKart = kart.kartController as PlayerScript;

                botKart.BulletBill = true;
                botKart.gameObject.AddComponent<BulletBill>().mask = kart.kartController.GroundMask;
                botKart.BoostTime = 10f;
                break;

            case Items.blooper:
                photonView.RPC("UseBlooper", RpcTarget.All, PhotonNetwork.LocalPlayer, kart.racePlace);
                break;
            case Items.lightning:
                photonView.RPC("UseLightning", RpcTarget.All, PhotonNetwork.LocalPlayer);
                break;
        }
        amount--;
        if (amount <= 0)
        {
            selectedItem = nothing;
        }
    }

    [PunRPC]
    public void UseBlooper(Photon.Realtime.Player sender, int racePlace)
    {
        Blooper.insance.Splat(racePlace);
    }

    [PunRPC]
    public void UseLightning(Photon.Realtime.Player sender)
    {
        if (sender == PhotonNetwork.LocalPlayer && !PhotonNetwork.LocalPlayer.IsMasterClient)
        {
            return;
        }
        else if (sender == PhotonNetwork.LocalPlayer && PhotonNetwork.LocalPlayer.IsMasterClient)
        {
            LightningHandler.instance.Strike();
        }

        LightningHandler.instance.Strike();
    }
}
