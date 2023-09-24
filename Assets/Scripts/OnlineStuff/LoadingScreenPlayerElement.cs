using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class LoadingScreenPlayerElement : MonoBehaviour
{
    public Image emblemImage;
    public TMP_Text playerName;

    public void SetUp(Player _player)
    {
        playerName.text = _player.NickName;

        emblemImage.sprite = IMG2Sprite.ConvertTextureToSprite(IMG2Sprite.LoadTextureFromBytes((byte[])_player.CustomProperties["emblem"]));
    }
}
