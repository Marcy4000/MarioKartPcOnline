using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class PlayerListItem : MonoBehaviourPunCallbacks
{
    [SerializeField] TMP_Text text;
    [SerializeField] GameObject banButton;

    Player player;

    public void SetUp(Player _player)
    {
        player = _player;
        text.text = _player.NickName;

        if (_player.IsMasterClient)
        {
            text.color = Color.yellow;
        }

        if (PhotonNetwork.LocalPlayer.IsMasterClient && PhotonNetwork.LocalPlayer != _player)
        {
            banButton.SetActive(true);
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

    public void BanPlayer()
    {
        if (PhotonNetwork.LocalPlayer.IsMasterClient)
        {
            PhotonNetwork.CloseConnection(player);
        }
    }
}
