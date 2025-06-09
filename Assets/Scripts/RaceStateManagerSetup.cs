using UnityEngine;
using Photon.Pun;

public class RaceStateManagerSetup : MonoBehaviourPunCallbacks
{
    [Header("Race State Manager Prefab")]
    public GameObject raceStateManagerPrefab;
    
    [Header("Auto Setup")]
    public bool autoCreateRaceStateManager = true;

    private void Start()
    {
        // Only the master client should create the RaceStateManager
        if (PhotonNetwork.IsMasterClient && autoCreateRaceStateManager)
        {
            SetupRaceStateManager();
        }
    }

    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        // If we become the new master client and there's no RaceStateManager, create one
        if (PhotonNetwork.IsMasterClient && RaceStateManager.instance == null && autoCreateRaceStateManager)
        {
            SetupRaceStateManager();
        }
    }

    private void SetupRaceStateManager()
    {
        // Check if RaceStateManager already exists
        if (RaceStateManager.instance != null)
        {
            return;
        }

        GameObject raceManager;
        
        if (raceStateManagerPrefab != null)
        {
            // Instantiate from prefab if available
            raceManager = PhotonNetwork.Instantiate(raceStateManagerPrefab.name, Vector3.zero, Quaternion.identity);
        }
        else
        {
            // Create a new GameObject with RaceStateManager component
            raceManager = new GameObject("RaceStateManager");
            raceManager.AddComponent<PhotonView>();
            raceManager.AddComponent<RaceStateManager>();
            
            // Make it a networked object
            PhotonView pv = raceManager.GetComponent<PhotonView>();
            PhotonNetwork.AllocateViewID(pv); // Correzione: passare il PhotonView come parametro
        }

        Debug.Log("RaceStateManager created by master client");
    }

    [ContextMenu("Force Create Race State Manager")]
    public void ForceCreateRaceStateManager()
    {
        SetupRaceStateManager();
    }
}
