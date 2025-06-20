using Photon.Pun;
using System.Collections;
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
    [SerializeField] private GameObject[] itemModels;
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

        if (!ItemRoulette.instance.spinning)
        {
            for (int i = 0; i < itemModels.Length; i++)
            {
                itemModels[i].SetActive(i == (int)selectedItem.itemType);
            }
            skinManager.SetCharacterBoolValue("HoldingItem", selectedItem != nothing);
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
        if (player.BulletBill)
        {
            return;
        }
        switch (selectedItem.itemType)
        {
            case Items.mushroom:
                player.BoostTime = 2f;
                break;
            case Items.greenShell:
                if (Input.GetKey(KeyCode.S) || Input.GetAxis("Vertical") < 0f)
                {
                    PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "GreenShell"), bananaSpawnPos.position, bananaSpawnPos.rotation);
                }
                else
                {
                    PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "GreenShell"), fireballSpawnPos.position, fireballSpawnPos.rotation);
                }
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
                player.gameObject.AddComponent<BulletBill>().mask = kart.kartController.GroundMask;
                player.BoostTime = 10f;
                break;
            case Items.blooper:
                photonView.RPC("UseBlooper", RpcTarget.AllViaServer, PhotonNetwork.LocalPlayer, KartLap.mainKart.racePlace);
                break;
            case Items.lightning:
                LightningHandler.instance.sender = true;
                photonView.RPC("UseLightning", RpcTarget.AllViaServer, PhotonNetwork.LocalPlayer);
                break;
        }
        amount--;
       
        if (amount <= 0)
        {
            selectedItem = nothing;
            ItemRoulette.instance.UpdateItem(nothing);
        }
        Debug.Log(selectedItem is MultipleObject);
        if (selectedItem is MultipleObject)
        {
            ItemRoulette.instance.firstImage.sprite = ((MultipleObject)selectedItem).SpriteCollection[amount - 1];   
        }
    }

    [PunRPC]
    public void UseBlooper(Photon.Realtime.Player sender, int racePlace)
    {
        if (sender == PhotonNetwork.LocalPlayer)
        {
            return;
        }
        Blooper.insance.Splat(racePlace);
    }

    [PunRPC]
    public void UseLightning(Photon.Realtime.Player sender)
    {
        LightningHandler.instance.Strike();
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
        skinManager.skinnedMeshRenderer.material = skinManager.characters[skinManager.selectedCharacter].KartMaterial;
        MusicManager.instance.Stop();
        MusicManager.instance.ResetAudioClip();
        MusicManager.instance.Play();
    }
}
