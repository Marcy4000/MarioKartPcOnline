using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class BootSceneController : MonoBehaviour
{
    private IEnumerator Start()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        NetworkManager.Singleton.LogLevel = LogLevel.Developer;
#else
        NetworkManager.Singleton.LogLevel = LogLevel.Error;
#endif

        yield return Addressables.InitializeAsync();

        LobbyController.Instance.StartGame();
    }
}