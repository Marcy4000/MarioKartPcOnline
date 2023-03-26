using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HolderSetDrifting : MonoBehaviour
{
    [SerializeField] private SkinManager skinManager;

    public void SetDrifting()
    {
        transform.parent.GetComponent<PlayerScript>().isSliding = true;
    }
}
