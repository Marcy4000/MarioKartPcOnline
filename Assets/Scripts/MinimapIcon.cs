using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimapIcon : MonoBehaviour
{
    [SerializeField] Image icon;
    public KartLap ConnectedKart { get; private set; }
    private Vector3 courseOffset;

    public void Initialize(Sprite iconSprite, KartLap currKart, Vector3 cameraOffset)
    {
        icon.sprite = iconSprite;
        ConnectedKart = currKart;
        courseOffset = cameraOffset;
    }

    private void Update()
    {
        Vector3 racerPosition = ConnectedKart.transform.position;

        // Subtract the course offset from the player position
        Vector3 adjustedPosition = racerPosition - courseOffset;

        // Convert the adjusted position to minimap coordinates
        Vector3 minimapPosition = new Vector3(adjustedPosition.x * 0.3f, adjustedPosition.z * 0.3f, 0);

        // Update the minimap icon position
        transform.localPosition = minimapPosition;
    }
}
