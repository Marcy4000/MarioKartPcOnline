using Unity.Netcode;
using UnityEngine;

public class SpawnManager : NetworkBehaviour
{
    public static SpawnManager Instance;

    [SerializeField] private SpawnPoint[] spawnPoints;
    private NetworkVariable<int> currentSpawnPoint = new NetworkVariable<int>(-1);

    private void Start()
    {
        Instance = this;
        //spawnPoints = FindObjectsOfType<SpawnPoint>();
    }

    public Transform getRandomSpawnPoint()
    {
        return spawnPoints[Random.Range(0, spawnPoints.Length)].transform;
    }
    
    public Transform getNextSpawnPoint()
    {
        return spawnPoints[currentSpawnPoint.Value].transform;
    }

    public Transform getSpawnPoint(int index)
    {
        return spawnPoints[index].transform;
    }
}
