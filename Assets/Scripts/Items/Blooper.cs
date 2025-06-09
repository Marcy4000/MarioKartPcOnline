using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Blooper : MonoBehaviour
{
    public static Blooper insance { get; private set; }
    [SerializeField] private Animator blooperAnimator, splatAnimator;

    private void Awake()
    {
        insance = this;
    }

    public void Splat(int racePlace)
    {
        /*if (KartLap.mainKart.racePlace > racePlace)
        {
            blooperAnimator.Play("BlooperMoving");
        }*/
        blooperAnimator.Play("BlooperMoving");
    }

    public void CoverScreen()
    {
        StartCoroutine(WaitThing());
    }

    IEnumerator WaitThing()
    {
        splatAnimator.Play("BlooperSplat");
        yield return new WaitForSeconds(8f);
        splatAnimator.Play("BlooperEnd");
    }
}
