using System.Collections;
using UnityEngine;

public class Minimap : MonoBehaviour
{
    [SerializeField] GameObject iconPrefab;
    [SerializeField] Sprite[] iconSprites;

    private bool ready;

    private void Start()
    {
        ready = false;
        StartCoroutine(InitializeMinimap());
    }

    IEnumerator InitializeMinimap()
    {
        yield return new WaitUntil(() => GlobalData.AllPlayersLoaded);

        yield return new WaitForSeconds(0.25f);

        Transform minimapCam = GameObject.FindGameObjectWithTag("MinimapCam").transform;

        foreach (var kart in PlaceCounter.instance.karts)
        {
            if (kart == KartLap.mainKart)
            {
                continue;
            }

            MinimapIcon icon = Instantiate(iconPrefab, transform).GetComponent<MinimapIcon>();

            if (kart.carController.isPlayer)
            {
                SkinManager skinManager = kart.GetComponent<SkinManager>();
                icon.Initialize(iconSprites[skinManager.selectedCharacter], kart, minimapCam.transform.position);
            }
            else
            {
                icon.Initialize(iconSprites[kart.carController.selectedCharacter], kart, minimapCam.transform.position);
            }
        }

        MinimapIcon iconMainPlayer = Instantiate(iconPrefab, transform).GetComponent<MinimapIcon>();
        SkinManager skinManagerMainPlayer = KartLap.mainKart.GetComponent<SkinManager>();
        iconMainPlayer.Initialize(iconSprites[skinManagerMainPlayer.selectedCharacter], KartLap.mainKart, minimapCam.transform.position);

        ready = true;
    }

    private void Update()
    {
        if (!ready)
        {
            return;
        }


    }
}
