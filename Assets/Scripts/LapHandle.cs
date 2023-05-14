using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Diagnostics;
using Photon.Pun;

public class LapHandle : MonoBehaviour
{
    public int CheckpointAmt, nLaps;
    public AudioSource lapSource;
    public AudioClip lastThing, finalLapClip;
    public AudioClip[] finishThemes;
    public TMP_Text lapCounter;
    public KartLap[] orderedKarts;

    private void Start()
    {
        lapCounter = GameObject.Find("LapCounter").GetComponent<TMP_Text>();
        lapCounter.text = $"Lap 1/{nLaps}";
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<KartLap>())
        {
            KartLap tempKart = other.GetComponent<KartLap>();

            if (tempKart.CheckpointIndex == CheckpointAmt)
            {
                tempKart.CheckpointIndex = 0;
                tempKart.lapNumber++;
                if (tempKart.lapNumber < nLaps + 1)
                {
                    if (tempKart.carController.pv.IsMine && !tempKart.carController.botDrive || tempKart.carController.pv.IsMine && tempKart.carController.botDrive && tempKart.carController.bulletBil)
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
                    if (tempKart.carController.pv.IsMine && !tempKart.carController.botDrive && !tempKart.hasFinished || tempKart.carController.pv.IsMine && tempKart.carController.botDrive && tempKart.carController.bulletBil && !tempKart.hasFinished)
                    {
                        lapSource.clip = lastThing;
                        lapSource.Play();
                        MusicManager.instance.Stop();
                        if (tempKart.racePlace == RacePlace.first)
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
                        //tempKart.carController.botDrive = true;
                        tempKart.hasFinished = true;
                        lapCounter.text = $"Lap {tempKart.lapNumber}/{nLaps}";
                        tempKart.lapNumber = 1000 - (int)tempKart.racePlace;
                        GlobalData.score += 7 - (int)tempKart.racePlace;
                        PhotonNetwork.LocalPlayer.CustomProperties["score"] = GlobalData.score;
                        PlayerPrefs.SetInt("score", GlobalData.score);
                        StartCoroutine(EndGame());
                    }
                    else
                    {
                        //tempKart.lapNumber = nLaps;
                        tempKart.lapNumber = 1000 - (int)tempKart.racePlace;
                        tempKart.carController.botDrive = true;
                        tempKart.hasFinished = true;
                    }
                    
                }
                
            }
            CheckIfGameEnded();
        }
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
            if (!kart.hasFinished && kart.carController.isPlayer)
            {
                everyoneEnded = false;
            }
        }

        if (everyoneEnded)
        {
            orderedKarts = PlaceCounter.instance.karts;
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
                if (!orderedKarts[i].carController.botDrive || orderedKarts[i].carController.isPlayer)
                {
                    VictoryScreen.instance.resultText.text += $"{i + 1}) {orderedKarts[i].photonView.Owner.NickName} ({(int)orderedKarts[i].photonView.Owner.CustomProperties["score"]})\n";
                }
                else
                {
                    VictoryScreen.instance.resultText.text += $"{i + 1}) {GlobalData.charPngNames[orderedKarts[i].carController.selectedCharacter]} (Bot {i+1})\n";
                }
            }

        }
    }
}
