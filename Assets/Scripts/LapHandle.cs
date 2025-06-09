using System.Collections;
using UnityEngine;
using TMPro;
using Photon.Pun;

public class LapHandle : MonoBehaviour
{
    public int CheckpointAmt, nLaps;
    public AudioSource lapSource;
    public AudioClip lastThing, finalLapClip;
    public AudioClip[] finishThemes;
    [SerializeField] private TMP_Text lapCounter;

    private void Start()
    {
        lapCounter = GameObject.Find("LapCounter").GetComponent<TMP_Text>();
        lapCounter.text = $"Lap 1/{nLaps}";
        
        // Configure the RaceStateManager with track settings
        if (RaceStateManager.instance != null)
        {
            RaceStateManager.instance.totalLaps = nLaps;
            RaceStateManager.instance.totalCheckpoints = CheckpointAmt;
            RaceStateManager.instance.lapSource = lapSource;
            RaceStateManager.instance.lastThing = lastThing;
            RaceStateManager.instance.finalLapClip = finalLapClip;
            RaceStateManager.instance.finishThemes = finishThemes;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        KartLap tempKart = other.GetComponent<KartLap>();

        if (tempKart == null)
        {
            return;
        }

        // Only validate that this kart has hit all checkpoints, then request lap completion from RaceStateManager
        if (tempKart.CheckpointIndex == CheckpointAmt)
        {
            // Request lap completion through the centralized system
            if (RaceStateManager.instance != null)
            {
                RaceStateManager.instance.RequestLapComplete(tempKart.photonView.ViewID, tempKart.transform.position);
            }
            else
            {
                // Fallback to old system if RaceStateManager is not available
                FallbackLapCompletion(tempKart);
            }
        }
    }

    // Fallback method in case RaceStateManager is not available
    private void FallbackLapCompletion(KartLap tempKart)
    {
        tempKart.CheckpointIndex = 0;
        tempKart.lapNumber++;
        
        if (tempKart.lapNumber < nLaps + 1)
        {
            if (tempKart.kartController.PhotonView.IsMine && !tempKart.kartController.IsBot)
            {
                if (tempKart.lapNumber == nLaps)
                {
                    lapSource.clip = finalLapClip;
                    lapSource.Play();
                    MusicManager.instance.Stop();
                    MusicManager.instance.ChangeSpeed(1.15f);
                    MusicManager.instance.PlayDelayed(finalLapClip.length);
                }
                else
                {
                    lapSource.Play();
                }
                lapCounter.text = $"Lap {tempKart.lapNumber}/{nLaps}";
            }
            tempKart.UpdatePlace(PlaceCounter.instance.GetCurrentPlace(tempKart));
        }
        else
        {
            if (tempKart.kartController.PhotonView.IsMine && !tempKart.kartController.IsBot && !tempKart.hasFinished)
            {
                lapSource.clip = lastThing;
                lapSource.Play();
                MusicManager.instance.Stop();
                if (tempKart.racePlace == 1)
                {
                    MusicManager.instance.SetAudioClip(finishThemes[0]);
                }
                else if ((int)tempKart.racePlace > 0 && (int)tempKart.racePlace < 6)
                {
                    MusicManager.instance.SetAudioClip(finishThemes[1]);
                }
                else
                {
                    MusicManager.instance.SetAudioClip(finishThemes[2]);
                }
                MusicManager.instance.ChangeSpeed(1f);
                MusicManager.instance.Play();
                tempKart.lapNumber = nLaps;
                tempKart.hasFinished = true;
                lapCounter.text = $"Lap {tempKart.lapNumber}/{nLaps}";
                tempKart.lapNumber = 1000 - (int)tempKart.racePlace;
                GlobalData.Score += 7 - (int)tempKart.racePlace;
                PhotonNetwork.LocalPlayer.CustomProperties["score"] = GlobalData.Score;
                PlayerPrefs.SetInt("score", GlobalData.Score);
                StartCoroutine(EndGame());
            }
            else
            {
                tempKart.lapNumber = 1000 - (int)tempKart.racePlace;
                tempKart.hasFinished = true;
            }
        }
        
        CheckIfGameEnded();
    }

    private IEnumerator EndGame()
    {
        yield return new WaitForSeconds(4.5f);
        VictoryScreen.instance.gameObject.SetActive(true);
    }

    private void CheckIfGameEnded()
    {
        // This method is now mainly handled by RaceStateManager
        // Keep it for fallback compatibility
        bool everyoneEnded = true;

        foreach (var kart in PlaceCounter.instance.karts)
        {
            if (!kart.hasFinished && !kart.kartController.IsBot)
            {
                everyoneEnded = false;
            }
        }

        if (everyoneEnded)
        {
            KartLap[] orderedKarts = PlaceCounter.instance.karts;
            KartLap temp;

            for (int i = 0; i < orderedKarts.Length - 1; i++)
            {
                for (int j = i + 1; j < orderedKarts.Length; j++)
                {
                    if ((int)orderedKarts[i].racePlace + 1 > (int)orderedKarts[j].racePlace + 1)
                    {
                        temp = orderedKarts[i];
                        orderedKarts[i] = orderedKarts[j];
                        orderedKarts[j] = temp;
                    }
                }
            }

            if (!VictoryScreen.instance.gameObject.activeInHierarchy)
            {
                VictoryScreen.instance.gameObject.SetActive(true);
            }

            VictoryScreen.instance.resultText.text = $"Results:\n";
            for (int i = 0; i < orderedKarts.Length; i++)
            {
                if (!orderedKarts[i].kartController.IsBot)
                {
                    VictoryScreen.instance.resultText.text += $"{i + 1}) {orderedKarts[i].photonView.Owner.NickName} ({(int)orderedKarts[i].photonView.Owner.CustomProperties["score"]})\n";
                }
                else
                {
                    VictoryScreen.instance.resultText.text += $"{i + 1}) Bot {i+1}\n";
                }
            }
        }
    }
}
