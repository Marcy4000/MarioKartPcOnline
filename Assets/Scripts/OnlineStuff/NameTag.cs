using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;

public class NameTag : MonoBehaviourPun
{
    [SerializeField] private TMP_Text nametagText;
    [SerializeField] private float Speed = 2f;
    private Transform mainCamera;

    private void Start()
    {
        SetName();
        if (photonView.IsMine && !GlobalData.showName)
        {
            gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (mainCamera == null)
        {
            if (Camera.main != null)
            {
                mainCamera = Camera.main.transform;
            }
            else
            {
                return;
            }
        }

        transform.rotation = Quaternion.Lerp(mainCamera.rotation, mainCamera.transform.rotation, Speed * Time.deltaTime);
    }

    private void SetName()
    {
        nametagText.text = photonView.Owner.NickName;
    }
}
