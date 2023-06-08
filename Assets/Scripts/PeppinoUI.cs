using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PeppinoUI : MonoBehaviour
{
    public Animator UiAnimator;
    private PlayerScript kart;
    public int currAnim = 0;

    private void Start()
    {
        if (GlobalData.SelectedCharacter != 16)
        {
            Destroy(gameObject);
        }
        StartCoroutine(Setup());
    }

    IEnumerator Setup()
    {
        while (!GlobalData.HasSceneLoaded)
        {
            yield return null;
        }
        yield return new WaitForSeconds(2.5f);
        kart = KartLap.mainKart.GetComponent<PlayerScript>();
    }

    private void Update()
    {
        if (kart == null)
        {
            return;
        }

        if (kart.RealSpeed < 5f)
        {
            UiAnimator.Play("idle");
            currAnim = 0;
        }
        else if (kart.RealSpeed > 5f && kart.RealSpeed < 55f)
        {
            if (currAnim != 1)
            {
                UiAnimator.Play("idle1");
            }
            currAnim = 1;
        }
        else if (kart.RealSpeed > 55f && kart.RealSpeed < 76f )
        {
            UiAnimator.Play("run");
            currAnim = 2;
        }
        else if (kart.RealSpeed > 76f)
        {
            UiAnimator.Play("run2");
            currAnim = 3;
        }
        else
        {
            UiAnimator.Play("idle1");
            currAnim = 4;
        }
    }
}
