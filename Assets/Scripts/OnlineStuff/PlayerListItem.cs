using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class PlayerListItem : MonoBehaviourPunCallbacks
{
    [SerializeField] TMP_Text text;

    Player player;

    public void SetUp(Player _player)
    {
        player = _player;
        text.text = $"{_player.NickName} ({(int)_player.CustomProperties["score"]} points)";
        if (_player.IsMasterClient)
        {
            text.color = Color.yellow;
        }
        if (string.Equals(_player.NickName, "JustMarcy", System.StringComparison.OrdinalIgnoreCase))
        {
            text.color = Color.green;
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (otherPlayer == player)
        {
            Destroy(gameObject);
        }
    }

    public override void OnLeftRoom()
    {
        Destroy(gameObject);
    }

    public void ShowProfile()
    {
        ProfileMenu.instance.OpenMenu(player);
    }
}
