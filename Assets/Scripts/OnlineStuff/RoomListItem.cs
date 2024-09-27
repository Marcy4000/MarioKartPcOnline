using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class RoomListItem : MonoBehaviour
{
    [SerializeField] TMP_Text text;

    public Lobby info;

    public void SetUp(Lobby _info)
    {
        info = _info;
        text.text = $"{_info.Name} ({_info.Players.Count}/{_info.MaxPlayers})";
    }

    public void OnClick()
    {
        LobbyController.Instance.TryLobbyJoin(info.LobbyCode);
    }
}
