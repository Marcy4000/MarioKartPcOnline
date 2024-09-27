using TMPro;
using Unity.Netcode;
using UnityEngine;

public class NameTag : NetworkBehaviour
{
    [SerializeField] private TMP_Text nametagText;
    [SerializeField] private float Speed = 2f;
    private Transform mainCamera;

    private void Start()
    {
        SetName();
        if (IsOwner && !GlobalData.ShowName)
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
        //nametagText.text = photonView.Owner.NickName;
    }
}
