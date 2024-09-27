using TMPro;
using UnityEngine;

public class VictoryScreen : MonoBehaviour
{
    public static VictoryScreen instance { get; private set; }
    public TMP_Text resultText;

    private void Start()
    {
        instance = this;
        gameObject.SetActive(false);
    }

    public void ReturnToMainMenu()
    {
        GlobalData.ReturnToLobby = true;
        LobbyController.Instance.ReturnToLobby(false);
    }
}
