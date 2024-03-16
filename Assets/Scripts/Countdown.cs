using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Countdown : MonoBehaviour
{
    public static Countdown Instance { get; private set; }
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private AudioSource numbers, go;
    public Animator lakitu;
    public bool canDoRocketBoost = false;
    public delegate void CountdownEnded();
    public event CountdownEnded OnCountdownEnded;

    private void Start()
    {
        Instance = this;
    }

    public void StartCountdown()
    {
        StartCoroutine(CountdownThing());
    }

    IEnumerator CountdownThing()
    {
        lakitu.gameObject.SetActive(true);
        yield return new WaitForSeconds(1.6f);
        lakitu.SetTrigger("doCountdown");
        yield return new WaitForSeconds(0.4f);
        countdownText.gameObject.SetActive(true);
        countdownText.text = "3";
        numbers.Play();
        yield return new WaitForSeconds(1f);
        countdownText.text = "2";
        numbers.Play();
        canDoRocketBoost = true;
        yield return new WaitForSeconds(0.5f);
        canDoRocketBoost = false;
        yield return new WaitForSeconds(0.5f);
        countdownText.text = "1";
        numbers.Play();
        yield return new WaitForSeconds(1f);
        countdownText.text = "GO!";
        go.Play();
        OnCountdownEnded?.Invoke();
        yield return new WaitForSeconds(0.4f);
        MusicManager.instance.Play();
        countdownText.gameObject.SetActive(false);
        lakitu.gameObject.SetActive(false);
    }
}
