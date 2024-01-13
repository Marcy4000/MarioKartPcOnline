using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class SkinManager : MonoBehaviourPun
{
    public static SkinManager instance { get; private set; }
    public KartCharacter[] characters;
    public GameObject itemThing;
    public int selectedCharacter;
    public SkinnedMeshRenderer skinnedMeshRenderer, emblemMesh;
    public SpriteRenderer emblemSprite;
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private Transform characterSpawnParent;

    private void Start()
    {
        if (!photonView.IsMine)
        {
            SetCharacter((int)photonView.Owner.CustomProperties["character"]);
            emblemMesh.material.SetTexture("_DetailAlbedoMap", IMG2Sprite.LoadTextureFromBytes((byte[])photonView.Owner.CustomProperties["emblem"]));
            emblemSprite.sprite = IMG2Sprite.ConvertTextureToSprite(IMG2Sprite.LoadTextureFromBytes((byte[])photonView.Owner.CustomProperties["emblem"]), 32, SpriteMeshType.FullRect, true);
            return;
        }
        instance = this;
        emblemMesh.material.SetTexture("_DetailAlbedoMap", IMG2Sprite.LoadTextureFromBytes((byte[])photonView.Owner.CustomProperties["emblem"]));
        emblemSprite.sprite = IMG2Sprite.ConvertTextureToSprite(IMG2Sprite.LoadTextureFromBytes((byte[])photonView.Owner.CustomProperties["emblem"]), 32, SpriteMeshType.FullRect, true);
        SetCharacter(GlobalData.SelectedCharacter);
    }

    public void SetCharacter(int character)
    {
        selectedCharacter = character;
        skinnedMeshRenderer.material = characters[character].KartMaterial;
        GameObject newCharacter = Instantiate(characters[character].CharacterModel, characterSpawnParent);
        characterAnimator = newCharacter.GetComponentInChildren<Animator>();
        GameObject itemPlace = new List<GameObject>(GameObject.FindGameObjectsWithTag("CharacterItemPlace")).Find(g => g.transform.IsChildOf(newCharacter.transform));
        itemThing.transform.SetParent(itemPlace.transform);
        itemThing.transform.position = itemPlace.transform.position;
        itemThing.transform.localRotation = Quaternion.Euler(Vector3.zero);
    }

    public void SetCharacterHitAnimation()
    {
        characterAnimator.SetTrigger("Hit");
    }

    public void SetCharacterBoolValue(string boolName, bool value)
    {
        characterAnimator.SetBool(boolName, value);
    }

    private void Update()
    {
        if (!photonView.IsMine)
        {
            return;
        }

        characterAnimator.SetFloat("Direction", Input.GetAxis("Horizontal"));
    }
}
