using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MainMenu : MonoBehaviour
{
    private static PointerEventData _eventDataCurrentPosition;
    private static List<RaycastResult> _results;
    private GameObject hoveredGameobject;
    public GameObject[] artworks;

    private bool IsOverUI()
    {
        _eventDataCurrentPosition = new PointerEventData(EventSystem.current) { position = Input.mousePosition};
        _results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(_eventDataCurrentPosition, _results);
        foreach (var result in _results)
        {
            if (result.gameObject.CompareTag("ArtworkButton"))
            {
                hoveredGameobject = result.gameObject;
                return true;
            }
        }
        return false;
    }

    private void LateUpdate()
    {
        if (IsOverUI())
        {
            if (hoveredGameobject.name == "Singleplayer")
            {
                foreach (var item in artworks)
                {
                    item.SetActive(false);
                }
                artworks[0].SetActive(true);
            }
            else if (hoveredGameobject.name == "Multiplayer")
            {
                foreach (var item in artworks)
                {
                    item.SetActive(false);
                }
                artworks[1].SetActive(true);
            }
            else if (hoveredGameobject.name == "EmblemEditor")
            {
                foreach (var item in artworks)
                {
                    item.SetActive(false);
                }
                artworks[2].SetActive(true);
            }
            else if (hoveredGameobject.name == "Options")
            {
                foreach (var item in artworks)
                {
                    item.SetActive(false);
                }
                artworks[3].SetActive(true);
            }
        }
        else
        {
            foreach (var item in artworks)
            {
                item.SetActive(false);
            }
        }
    }
}
