using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ItemManager : MonoBehaviourPun
{
    private Item selectedItem;
    private PlayerScript player;
    public bool canUseItem = true;
    [SerializeField] private Item nothing;
    [SerializeField] KartLap kart;
    [SerializeField] private Transform fireballSpawnPos, bananaSpawnPos;
    int amount = 0;
    [SerializeField] private AudioClip starTheme;
    [SerializeField] private Material rainbow;
    private SkinManager skinManager;

    private void Start()
    {
        selectedItem = nothing;
        player = GetComponent<PlayerScript>();
        skinManager = GetComponent<SkinManager>();
        if (!photonView.IsMine)
        {
            enabled = false;
        }
    }

    void Update()
    {
        if (selectedItem != ItemRoulette.instance.selectedItem)
        {
            selectedItem = ItemRoulette.instance.selectedItem;
            amount = ItemRoulette.instance.selectedItem.amount;
        }


        if (!GlobalData.UseController)
        {
            if ((Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.RightControl)) && selectedItem != nothing && !ItemRoulette.instance.spinning && canUseItem )
            {
                UseItem();
            }
        }
        else
        {
            if (Input.GetButtonDown("UseItem1") && selectedItem != nothing && !ItemRoulette.instance.spinning && canUseItem)
            {
                UseItem();
            }
        }
    }

    private void UseItem()
    {
        switch (selectedItem.itemType)
        {
            case Items.mushroom:
                player.BoostTime = 2f;
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
                PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "Banana"), bananaSpawnPos.position, bananaSpawnPos.rotation);
                break;
            case Items.star:
                //CarController.Instance.EnterStarmode();
                StartCoroutine(Star());
                break;
            case Items.fireFlower:
                PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "Fireball"), fireballSpawnPos.position, transform.rotation);
                break;
            case Items.bulletBill:
                player.BulletBill = true;
                player.gameObject.AddComponent<BulletBill>().mask = kart.carController.whatIsGround;
                player.BoostTime = 10f;
                break;
            case Items.blooper:
                photonView.RPC("UseBlooper", RpcTarget.All, PhotonNetwork.LocalPlayer, KartLap.mainKart.racePlace);
                break;
        }
        amount--;
        if (amount <= 0)
        {
            selectedItem = nothing;
            ItemRoulette.instance.UpdateItem(nothing);
        }
    }

    [PunRPC]
    public void UseBlooper(Photon.Realtime.Player sender, RacePlace racePlace)
    {
        if (sender == PhotonNetwork.LocalPlayer)
        {
            return;
        }
        Blooper.insance.Splat(racePlace);
    }

    private IEnumerator Star()
    {
        player.star = true;
        player.kartAnimator.SetBool("Star", true);
        skinManager.skinnedMeshRenderer.material = rainbow;
        player.BoostTime = 8f;
        MusicManager.instance.Stop();
        MusicManager.instance.SetAudioClip(starTheme);
        MusicManager.instance.Play();
        yield return new WaitForSeconds(8f);
        player.star = false;
        player.kartAnimator.SetBool("Star", false);
        skinManager.skinnedMeshRenderer.material = skinManager.characters[skinManager.selectedCharacter].kartMaterial;
        MusicManager.instance.Stop();
        MusicManager.instance.ResetAudioClip();
        MusicManager.instance.Play();
    }

    private IEnumerator BulletBill()
    {
        canUseItem = false;
        CarController.Instance.botTurnSpeed = 0.095f;
        CarController.Instance.gravityForce *= 12;
        CarController.Instance.botDrive = true;
        CarController.Instance.bulletBil = true;
        CarController.Instance.forwardAccel *= 3.5f;
        yield return new WaitForSeconds(6f);
        CarController.Instance.botDrive = false;
        CarController.Instance.bulletBil = false;
        CarController.Instance.forwardAccel /= 3.5f;
        CarController.Instance.gravityForce /= 12;
        CarController.Instance.botTurnSpeed = Random.Range(0.014f, 0.027f); ;
        amount = 0;
        selectedItem = nothing;
        ItemRoulette.instance.UpdateItem(nothing);
        canUseItem = true;
    }
}
