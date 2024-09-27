using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class ProfileMenu : MonoBehaviour
{
    public static ProfileMenu instance;

    public Image playerPFP;
    public TMP_Text playerName, playerInfo;
    public GameObject holder;

    private void Awake()
    {
        instance = this;
    }

    public void CloseMenu()
    {
        holder.SetActive(false);
    }

    public void OpenMenu(Player _player)
    {
        playerName.text = _player.Data["PlayerName"].Value;
        //playerPFP.sprite = IMG2Sprite.ConvertTextureToSprite(IMG2Sprite.LoadTextureFromBytes((byte[])_player.CustomProperties["emblem"]));

        //playerInfo.text = $"Points: {(int)_player.CustomProperties["score"]}\nBio: {(string)_player.CustomProperties["bio"]}";

        holder.SetActive(true);
    }
}
