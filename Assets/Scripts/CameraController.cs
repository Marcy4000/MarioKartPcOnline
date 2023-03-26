using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraController : MonoBehaviour
{
    [SerializeField] CinemachineVirtualCamera virtualCamera;
    CinemachineTransposer trp;
    [SerializeField] float zOffset;

    private IEnumerator Start()
    {
        trp = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
        while (!GlobalData.HasSceneLoaded)
        {
            yield return null;
        }
        PlayerScript[] players = FindObjectsOfType<PlayerScript>();
        foreach (PlayerScript player in players)
        {
            if (player.photonView.IsMine)
            {
                virtualCamera.Follow = player.transform;
                virtualCamera.LookAt = player.transform;
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetButtonDown("Jump"))
        {
            trp.m_FollowOffset = new Vector3(trp.m_FollowOffset.x, trp.m_FollowOffset.y, zOffset);
            trp.m_XDamping = 0;
            trp.m_YDamping = 0;
            trp.m_ZDamping = 0;
        }

        if (Input.GetKeyUp(KeyCode.Tab) || Input.GetButtonUp("Jump"))
        {
            trp.m_FollowOffset = new Vector3(trp.m_FollowOffset.x, trp.m_FollowOffset.y, -zOffset);
            trp.m_XDamping = 0.5f;
            trp.m_YDamping = 0.5f;
            trp.m_ZDamping = 0.5f;
        }
    }
}
