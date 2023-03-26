using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance;
    PhotonView pv;

    [SerializeField] private SpawnPoint[] spawnPoints;
    int currentSpawnPoint = -1;

    private void Start()
    {
        pv = GetComponent<PhotonView>();
        Instance = this;
        //spawnPoints = FindObjectsOfType<SpawnPoint>();
    }

    public Transform getRandomSpawnPoint()
    {
        return spawnPoints[Random.Range(0, spawnPoints.Length)].transform;
    }
    
    public Transform getNextSpawnPoint()
    {
        pv.RPC("IncreaseCurrentSpawnPoint", RpcTarget.All);
        return spawnPoints[currentSpawnPoint].transform;
    }

    public Transform getSpawnPoint(int index)
    {
        return spawnPoints[index].transform;
    }

    [PunRPC]
    public void IncreaseCurrentSpawnPoint()
    {
        currentSpawnPoint++;
    }
}
