using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScreenPlayerElement : MonoBehaviour
{
    public Image emblemImage;
    public TMP_Text playerName;

    public void SetUp(Player _player)
    {
        playerName.text = _player.Data["PlayerName"].Value;

        //emblemImage.sprite = IMG2Sprite.ConvertTextureToSprite(IMG2Sprite.LoadTextureFromBytes((byte[])_player.CustomProperties["emblem"]));
    }
}
