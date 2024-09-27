using System.IO;
using Unity.Netcode;
using UnityEngine;

public class CraneSpawner : NetworkBehaviour
{
    [SerializeField] Vector3[] cranePositions;

    void Start()
    {
        if (!LobbyController.Instance.IsLocalPlayerHost()) return;
        for (int i = 0; i < cranePositions.Length; i++)
        {
            //PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "Crane"), cranePositions[i], Quaternion.identity);
        }
    }
}
