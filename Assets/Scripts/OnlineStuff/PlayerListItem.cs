using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class PlayerListItem : MonoBehaviour
{
    [SerializeField] TMP_Text text;
    [SerializeField] GameObject banButton;

    Player player;

    public void Initialize(Player _player)
    {
        player = _player;
        text.text = _player.Data["PlayerName"].Value;

        if (_player.Id == LobbyController.Instance.Lobby.HostId)
        {
            text.color = Color.yellow;
        }

        if (_player.Id == LobbyController.Instance.Lobby.HostId && LobbyController.Instance.Player != _player)
        {
            banButton.SetActive(true);
        }

        NetworkManager.Singleton.OnConnectionEvent += OnConnectionEvent;
    }

    private void OnConnectionEvent(NetworkManager manager, ConnectionEventData connectionEvent)
    {
        if (connectionEvent.EventType == ConnectionEvent.PeerDisconnected && connectionEvent.ClientId == ulong.Parse(player.Data["OwnerID"].Value))
        {
            Destroy(gameObject);
        }
    }

    public void ShowProfile()
    {
        ProfileMenu.instance.OpenMenu(player);
    }

    public void BanPlayer()
    {
        LobbyController.Instance.KickPlayer(player.Id);
    }
}
