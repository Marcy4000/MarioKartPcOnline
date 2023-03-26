using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager instance;

    public AudioSource source;
    public AudioClip musicClip;

    private void Start()
    {
        instance = this;
    }

    public void Play()
    {
        source.Play();
    }

    public void PlayDelayed(float delay)
    {
        StartCoroutine(Delay(delay));
    }

    public void ChangeSpeed(float newSpeed)
    {
        source.pitch = newSpeed;
    }

    public void SetAudioClip(AudioClip clip)
    {
        source.clip = clip;
    }

    public void ResetAudioClip()
    {
        source.clip = musicClip;
    }

    public void Stop()
    {
        source.Stop();
    }

    IEnumerator Delay(float delay)
    {
        yield return new WaitForSeconds(delay);
        source.Play();
    }
}
