using System.Collections;
using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    public bool isOpen;
    [SerializeField] private GameObject ui;
    private Transform[] checkpoints;

    void Start()
    {
        StartCoroutine(DoThing());
    }

    IEnumerator DoThing()
    {
        while (!GlobalData.HasSceneLoaded)
        {
            yield return null;
        }
        yield return new WaitForSeconds(2.5f);
        LapCheckPoint[] checkpointsScripts = FindObjectsOfType<LapCheckPoint>();
        checkpoints = new Transform[checkpointsScripts.Length];
        for (int i = 0; i < checkpointsScripts.Length; i++)
        {
            checkpoints[i] = checkpointsScripts[i].transform;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isOpen)
            {
                isOpen = false;
                ui.SetActive(false);
            }
            else
            {
                isOpen = true;
                ui.SetActive(true);
            }
        }
    }

    public void Continue()
    {
        isOpen = false;
        ui.SetActive(false);
    }

    public void ImStuck()
    {
        KartLap kartLap = KartLap.mainKart;

        if (!kartLap.carController.IsOwner)
        {
            return;
        }

        Transform otherThing = checkpoints[0];

        foreach (var item in checkpoints)
        {
            if (item.GetComponent<LapCheckPoint>().Index == kartLap.CheckpointIndex)
            {
                otherThing = item;
                break;
            }
        }

        kartLap.transform.position = otherThing.position;
        kartLap.transform.rotation = Quaternion.FromToRotation(kartLap.transform.forward, otherThing.forward) * kartLap.transform.rotation;
    }

    public void ReturnToMainMenu()
    {
        GlobalData.ReturnToLobby = false;
        LobbyController.Instance.ReturnToLobby(true);
    }
}
