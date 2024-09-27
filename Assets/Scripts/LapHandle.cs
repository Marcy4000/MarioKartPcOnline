using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class LapHandle : NetworkBehaviour
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
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer)
        {
            return;
        }

        KartLap tempKart = other.GetComponent<KartLap>();

        if (tempKart == null)
        {
            return;
        }

        if (tempKart.CheckpointIndex == CheckpointAmt)
        {
            tempKart.IncreaseLapCount();
            if (tempKart.LapNumber < nLaps + 1)
            {
                if (tempKart.carController.IsOwner && tempKart.carController.isPlayer)
                {
                    if (tempKart.LapNumber == nLaps)
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
                    lapCounter.text = $"Lap {tempKart.LapNumber}/{nLaps}";
                }
                tempKart.UpdatePlace(PlaceCounter.instance.GetCurrentPlace(tempKart));
            }
            else
            {
                if (tempKart.carController.IsOwner && tempKart.carController.isPlayer && !tempKart.HasFinished)
                {
                    lapSource.clip = lastThing;
                    lapSource.Play();
                    MusicManager.instance.Stop();
                    if (tempKart.RacePlace == RacePlace.first)
                    {
                        MusicManager.instance.SetAudioClip(finishThemes[0]);
                    }
                    else if ((int)tempKart.RacePlace > 0 && (int)tempKart.RacePlace < 6)
                    {
                        MusicManager.instance.SetAudioClip(finishThemes[1]);
                    }
                    else
                    {
                        MusicManager.instance.SetAudioClip(finishThemes[2]);
                    }
                    MusicManager.instance.ChangeSpeed(1f);
                    MusicManager.instance.Play();
                    tempKart.SetLapNumber(nLaps);
                    //tempKart.carController.botDrive = true;
                    tempKart.SetHasFinished(true);
                    lapCounter.text = $"Lap {tempKart.LapNumber}/{nLaps}";

                    tempKart.SetLapNumber(1000 - (int)tempKart.RacePlace);
                    GlobalData.Score += 7 - (int)tempKart.RacePlace;
                    //PhotonNetwork.LocalPlayer.CustomProperties["score"] = GlobalData.Score;
                    PlayerPrefs.SetInt("score", GlobalData.Score);
                    StartCoroutine(EndGame());
                }
                else
                {
                    //tempKart.lapNumber = nLaps;
                    tempKart.SetLapNumber(1000 - (int)tempKart.RacePlace);
                    tempKart.carController.botDrive = true;
                    tempKart.SetHasFinished(true);
                }

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
        bool everyoneEnded = true;

        foreach (var kart in PlaceCounter.instance.karts)
        {
            if (!kart.HasFinished && kart.carController.isPlayer)
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
                    if ((int)orderedKarts[i].RacePlace + 1 > (int)orderedKarts[j].RacePlace + 1)
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
                if (!orderedKarts[i].carController.botDrive || orderedKarts[i].carController.isPlayer)
                {
                    VictoryScreen.instance.resultText.text += $"{i + 1}) {orderedKarts[i].NetworkObjectId} (N/A)\n";
                }
                else
                {
                    VictoryScreen.instance.resultText.text += $"{i + 1}) {GlobalData.CharPngNames[orderedKarts[i].carController.selectedCharacter]} (Bot {i+1})\n";
                }
            }

        }
    }
}
