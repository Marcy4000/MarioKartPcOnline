using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class RaceStateManager : MonoBehaviourPunCallbacks, IPunObservable
{
    public static RaceStateManager instance { get; private set; }
    
    [Header("Race Settings")]
    public int totalLaps = 3;
    public int totalCheckpoints = 3;
    
    [Header("Audio")]
    public AudioSource lapSource;
    public AudioClip lastThing, finalLapClip;
    public AudioClip[] finishThemes;
    
    private Dictionary<int, RaceData> raceStates = new Dictionary<int, RaceData>();
    private List<int> finishedOrder = new List<int>();
    private bool raceEnded = false;
    private TMP_Text lapCounter;

    private Coroutine positionUpdateCoroutine;
    private bool raceStarted = false;

    [System.Serializable]
    public class RaceData
    {
        public int photonViewID;
        public int currentLap;
        public int currentCheckpoint;
        public int position;
        public bool hasFinished;
        public string playerName;
        public bool isBot;

        public RaceData(int viewID, string name, bool bot = false)
        {
            photonViewID = viewID;
            currentLap = 1;
            currentCheckpoint = 0;
            position = 1; // 1st place by default
            hasFinished = false;
            playerName = name;
            isBot = bot;
        }
    }

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        lapCounter = GameObject.Find("LapCounter")?.GetComponent<TMP_Text>();
        if (lapCounter != null)
        {
            lapCounter.text = $"Lap 1/{totalLaps}";
        }
    }

    public void RegisterKart(int photonViewID, string playerName, bool isBot = false)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            raceStates[photonViewID] = new RaceData(photonViewID, playerName, isBot);
            photonView.RPC("SyncKartRegistration", RpcTarget.Others, photonViewID, playerName, isBot);

            // Avvia la coroutine di aggiornamento posizioni se non già partita
            if (!raceStarted)
            {
                raceStarted = true;
                positionUpdateCoroutine = StartCoroutine(PositionUpdateLoop());
            }
        }
    }

    [PunRPC]
    public void SyncKartRegistration(int photonViewID, string playerName, bool isBot)
    {
        raceStates[photonViewID] = new RaceData(photonViewID, playerName, isBot);
    }

    public void RequestLapComplete(int kartPhotonViewID, Vector3 kartPosition)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            ProcessLapCompletion(kartPhotonViewID, kartPosition);
        }
        else
        {
            photonView.RPC("RequestLapCompleteRPC", RpcTarget.MasterClient, kartPhotonViewID, kartPosition.x, kartPosition.y, kartPosition.z);
        }
    }

    [PunRPC]
    public void RequestLapCompleteRPC(int kartPhotonViewID, float posX, float posY, float posZ)
    {
        Vector3 kartPosition = new Vector3(posX, posY, posZ);
        ProcessLapCompletion(kartPhotonViewID, kartPosition);
    }

    private void ProcessLapCompletion(int kartPhotonViewID, Vector3 kartPosition)
    {
        if (!PhotonNetwork.IsMasterClient || !raceStates.ContainsKey(kartPhotonViewID))
            return;

        RaceData raceData = raceStates[kartPhotonViewID];
        
        // Validate lap completion (check if all checkpoints were hit)
        if (raceData.currentCheckpoint != totalCheckpoints)
            return;

        // Process lap completion
        raceData.currentCheckpoint = 0;
        raceData.currentLap++;

        // Check if race is finished for this kart
        if (raceData.currentLap > totalLaps)
        {
            if (!raceData.hasFinished)
            {
                raceData.hasFinished = true;
                finishedOrder.Add(kartPhotonViewID);
                
                // Set final position based on finish order (1st place = 1, 2nd place = 2, etc.)
                raceData.position = finishedOrder.Count;
                
                // Broadcast lap completion
                photonView.RPC("OnLapCompleted", RpcTarget.All, kartPhotonViewID, raceData.currentLap - 1, true, raceData.position);
                
                CheckRaceEnd();
            }
        }
        else
        {
            // Normal lap completion
            photonView.RPC("OnLapCompleted", RpcTarget.All, kartPhotonViewID, raceData.currentLap, false, -1);
        }

        // Recalculate all positions
        RecalculatePositions();
    }

    public void RequestCheckpointHit(int kartPhotonViewID, int checkpointIndex, Vector3 kartPosition)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            ProcessCheckpointHit(kartPhotonViewID, checkpointIndex, kartPosition);
        }
        else
        {
            photonView.RPC("RequestCheckpointHitRPC", RpcTarget.MasterClient, kartPhotonViewID, checkpointIndex, kartPosition.x, kartPosition.y, kartPosition.z);
        }
    }

    [PunRPC]
    public void RequestCheckpointHitRPC(int kartPhotonViewID, int checkpointIndex, float posX, float posY, float posZ)
    {
        Vector3 kartPosition = new Vector3(posX, posY, posZ);
        ProcessCheckpointHit(kartPhotonViewID, checkpointIndex, kartPosition);
    }

    private void ProcessCheckpointHit(int kartPhotonViewID, int checkpointIndex, Vector3 kartPosition)
    {
        if (!PhotonNetwork.IsMasterClient || !raceStates.ContainsKey(kartPhotonViewID))
            return;

        RaceData raceData = raceStates[kartPhotonViewID];
        
        // Validate checkpoint sequence (must hit checkpoints in order)
        if (checkpointIndex == raceData.currentCheckpoint + 1 || 
            (raceData.currentCheckpoint == totalCheckpoints && checkpointIndex == 1))
        {
            raceData.currentCheckpoint = checkpointIndex;
            
            photonView.RPC("OnCheckpointHit", RpcTarget.All, kartPhotonViewID, checkpointIndex);
            
            // Recalculate positions after checkpoint update
            RecalculatePositions();
        }
    }

    private void RecalculatePositions()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        var allKarts = raceStates.Values.ToList();

        foreach (var targetKart in allKarts)
        {
            int currPlace = 1;

            foreach (var kart in allKarts)
            {
                if (kart.photonViewID == targetKart.photonViewID)
                    continue;

                bool isAhead = false;

                if (kart.hasFinished && !targetKart.hasFinished)
                {
                    // Chi ha finito è sempre davanti a chi non ha finito
                    isAhead = true;
                }
                else if (kart.hasFinished && targetKart.hasFinished)
                {
                    // Entrambi hanno finito: confronta l'ordine di arrivo
                    int kartFinishOrder = finishedOrder.IndexOf(kart.photonViewID);
                    int targetFinishOrder = finishedOrder.IndexOf(targetKart.photonViewID);
                    if (kartFinishOrder < targetFinishOrder)
                        isAhead = true;
                }
                else if (!kart.hasFinished && !targetKart.hasFinished)
                {
                    // Nessuno dei due ha finito: logica classica
                    if (kart.currentLap > targetKart.currentLap)
                    {
                        isAhead = true;
                    }
                    else if (kart.currentLap == targetKart.currentLap)
                    {
                        if (kart.currentCheckpoint > targetKart.currentCheckpoint)
                        {
                            isAhead = true;
                        }
                        else if (kart.currentCheckpoint == targetKart.currentCheckpoint)
                        {
                            float targetDistance = GetDistanceToNextCheckpoint(targetKart);
                            float kartDistance = GetDistanceToNextCheckpoint(kart);

                            if (kartDistance < targetDistance)
                            {
                                isAhead = true;
                            }
                        }
                    }
                }

                if (isAhead)
                {
                    currPlace++;
                }
            }

            if (targetKart.position != currPlace)
            {
                targetKart.position = currPlace;
                photonView.RPC("UpdateRacePosition", RpcTarget.All, targetKart.photonViewID, currPlace);
            }
        }
    }

    // Calcola la distanza dal prossimo checkpoint per RaceData
    private float GetDistanceToNextCheckpoint(RaceData raceData)
    {
        if (PlaceCounter.instance == null)
            return float.MaxValue;

        // Ottieni la posizione attuale dal GameObject associato al PhotonViewID
        PhotonView kartPV = PhotonView.Find(raceData.photonViewID);
        if (kartPV == null)
            return float.MaxValue;
        Vector3 kartPosition = kartPV.transform.position;

        Transform nextCheckpoint = PlaceCounter.instance.GetCheckpointTransformByIndex(raceData.currentCheckpoint + 1);
        if (nextCheckpoint == null)
            return float.MaxValue;

        return Vector3.Distance(kartPosition, nextCheckpoint.position);
    }

    [PunRPC]
    public void OnLapCompleted(int kartPhotonViewID, int newLap, bool hasFinished, int finalPosition)
    {
        // Handle lap completion on all clients
        PhotonView kartPV = PhotonView.Find(kartPhotonViewID);
        if (kartPV == null) return;

        KartLap kartLap = kartPV.GetComponent<KartLap>();
        if (kartLap == null) return;

        kartLap.SetLapData(newLap, 0, hasFinished);

        // Handle audio and UI for local player
        if (kartPV.IsMine && !kartLap.kartController.IsBot)
        {
            if (hasFinished)
            {
                HandleRaceFinish(finalPosition);
            }
            else if (newLap == totalLaps)
            {
                HandleFinalLap();
            }
            else
            {
                lapSource.Play();
            }
            
            if (lapCounter != null)
            {
                lapCounter.text = $"Lap {newLap}/{totalLaps}";
            }
        }
    }

    private void HandleFinalLap()
    {
        lapSource.clip = finalLapClip;
        lapSource.Play();
        MusicManager.instance.Stop();
        MusicManager.instance.ChangeSpeed(1.15f);
        MusicManager.instance.PlayDelayed(finalLapClip.length);
    }

    private void HandleRaceFinish(int position)
    {
        lapSource.clip = lastThing;
        lapSource.Play();
        MusicManager.instance.Stop();
        
        if (position == 1) // First place
        {
            MusicManager.instance.SetAudioClip(finishThemes[0]);
        }
        else if (position >= 2 && position <= 6)
        {
            MusicManager.instance.SetAudioClip(finishThemes[1]);
        }
        else
        {
            MusicManager.instance.SetAudioClip(finishThemes[2]);
        }
        
        MusicManager.instance.ChangeSpeed(1f);
        MusicManager.instance.Play();
        
        // Update score (adjust for 1-based positions)
        GlobalData.Score += 8 - position; // 1st = +7, 2nd = +6, etc.
        PhotonNetwork.LocalPlayer.CustomProperties["score"] = GlobalData.Score;
        PlayerPrefs.SetInt("score", GlobalData.Score);
        
        StartCoroutine(ShowVictoryScreen());
    }

    private IEnumerator ShowVictoryScreen()
    {
        yield return new WaitForSeconds(4.5f);
        if (VictoryScreen.instance != null)
        {
            VictoryScreen.instance.gameObject.SetActive(true);
        }
    }

    [PunRPC]
    public void OnCheckpointHit(int kartPhotonViewID, int checkpointIndex)
    {
        PhotonView kartPV = PhotonView.Find(kartPhotonViewID);
        if (kartPV != null)
        {
            KartLap kartLap = kartPV.GetComponent<KartLap>();
            if (kartLap != null)
            {
                kartLap.SetCheckpointIndex(checkpointIndex);
            }
        }
    }

    [PunRPC]
    public void UpdateRacePosition(int kartPhotonViewID, int position)
    {
        PhotonView kartPV = PhotonView.Find(kartPhotonViewID);
        if (kartPV != null)
        {
            KartLap kartLap = kartPV.GetComponent<KartLap>();
            if (kartLap != null)
            {
                kartLap.UpdateNetworkPosition(position);
            }
        }
    }

    private IEnumerator PositionUpdateLoop()
    {
        yield return new WaitUntil(() => GlobalData.AllPlayersLoaded);

        yield return new WaitForSeconds(0.25f);

        var wait = new WaitForSeconds(1f / 30f); // 30 volte al secondo
        while (!raceEnded)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                RecalculatePositions();
            }
            yield return wait;
        }
    }

    private void CheckRaceEnd()
    {
        if (raceEnded) return;

        // Check if all players have finished
        bool allFinished = true;
        foreach (var raceData in raceStates.Values)
        {
            if (!raceData.isBot && !raceData.hasFinished)
            {
                allFinished = false;
                break;
            }
        }

        if (allFinished)
        {
            raceEnded = true;
            // Ferma la coroutine di aggiornamento posizioni
            if (positionUpdateCoroutine != null)
            {
                StopCoroutine(positionUpdateCoroutine);
                positionUpdateCoroutine = null;
            }
            photonView.RPC("ShowFinalResults", RpcTarget.All);
        }
    }

    [PunRPC]
    public void ShowFinalResults()
    {
        if (VictoryScreen.instance != null && !VictoryScreen.instance.gameObject.activeInHierarchy)
        {
            VictoryScreen.instance.gameObject.SetActive(true);
            
            // Build results string
            string results = "Results:\n";
            var sortedResults = raceStates.Values.OrderBy(r => finishedOrder.Contains(r.photonViewID) ? finishedOrder.IndexOf(r.photonViewID) : int.MaxValue).ToList();
            
            for (int i = 0; i < sortedResults.Count; i++)
            {
                var raceData = sortedResults[i];
                if (!raceData.isBot)
                {
                    PhotonView kartPV = PhotonView.Find(raceData.photonViewID);
                    if (kartPV != null && kartPV.Owner != null)
                    {
                        results += $"{i + 1}) {kartPV.Owner.NickName} ({(int)kartPV.Owner.CustomProperties["score"]})\n";
                    }
                }
                else
                {
                    results += $"{i + 1}) {raceData.playerName} (Bot)\n";
                }
            }
            
            VictoryScreen.instance.resultText.text = results;
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        // This can be used for additional synchronization if needed
        // Currently, we're using RPCs for most communication
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        // Handle master client change
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Became new master client, taking over race state management");
            // The new master client can request current state from other clients if needed
        }
    }
}
