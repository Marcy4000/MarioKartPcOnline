using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HolderSetDrifting : MonoBehaviour
{
    [SerializeField] private SkinManager skinManager;
    private PlayerScript player;

    private void Start()
    {
        player = transform.parent.GetComponent<PlayerScript>();
    }

    public void SetDrifting()
    {
        player.isSliding = true;
    }
}
