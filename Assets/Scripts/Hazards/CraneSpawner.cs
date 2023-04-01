using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CraneSpawner : MonoBehaviour
{
    [SerializeField] Vector3[] cranePositions;
    PhotonView pv;
    // Start is called before the first frame update
    void Start()
    {
        pv = GetComponent<PhotonView>();
        if (!PhotonNetwork.IsMasterClient) return;
        for (int i = 0; i < cranePositions.Length; i++)
        {
            PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "Crane"), cranePositions[i], Quaternion.identity);
        }
    }
}
