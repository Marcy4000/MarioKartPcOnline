using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class SkinManager : MonoBehaviourPun
{
    public static SkinManager instance { get; private set; }
    public Character[] characters;
    public int selectedCharacter;
    public SkinnedMeshRenderer skinnedMeshRenderer, emblemMesh;
    [SerializeField] private Animator characterAnimator;

    private void Start()
    {
        if (!photonView.IsMine)
        {
            SetCharacter((int)photonView.Owner.CustomProperties["character"]);
            emblemMesh.material.SetTexture("_DetailAlbedoMap", IMG2Sprite.LoadTextureFromBytes((byte[])photonView.Owner.CustomProperties["emblem"]));
            return;
        }
        instance = this;
        emblemMesh.material.SetTexture("_DetailAlbedoMap", IMG2Sprite.LoadTextureFromBytes((byte[])photonView.Owner.CustomProperties["emblem"]));
        SetCharacter(GlobalData.SelectedCharacter);
    }

    public void SetCharacter(int character)
    {
        selectedCharacter = character;
        skinnedMeshRenderer.material = characters[character].kartMaterial;
        foreach (var model in characters)
        {
            model.model.SetActive(false);
        }
        characters[character].model.SetActive(true);
        characterAnimator = characters[character].modelAnimator;
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
