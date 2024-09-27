using TMPro;
using UnityEngine;

public class CharacterSelectScreen : MonoBehaviour
{
    public DummyKart dummyKart;
    public int selectedChar;
    public TMP_Text[] names;

    private void OnEnable()
    {
        selectedChar = GlobalData.SelectedCharacter;
        //_myCustomProprieties = PhotonNetwork.LocalPlayer.CustomProperties;
        dummyKart.gameObject.SetActive(true);
        dummyKart.SetCharacter(GlobalData.SelectedCharacter);
        foreach (var name in names)
        {
            name.text = GlobalData.CharPngNames[GlobalData.SelectedCharacter];
        }
    }

    private void OnDisable()
    {
        dummyKart.gameObject.SetActive(false);
    }

    public void SetSelectedChar(int chara)
    {
        selectedChar = chara;
        foreach (var name in names)
        {
            name.text = GlobalData.CharPngNames[chara];
        }
    }

    public void ChangeCharacter()
    {
        GlobalData.SelectedCharacter = selectedChar;
        PlayerPrefs.SetInt("character", selectedChar);
        //_myCustomProprieties["character"] = selectedChar;
    }
}
