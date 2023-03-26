using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour
{
    [SerializeField] private TMP_InputField nameField;
    [SerializeField] private Toggle fullscreen, useController;
    [SerializeField] private DummyKart dummyKart;
    public TMP_Dropdown resolutionsDropdown, characterDropdown, regionDropdown;
    private Resolution[] resolutions;
    private ExitGames.Client.Photon.Hashtable _myCustomProprieties = new ExitGames.Client.Photon.Hashtable();

    private void Start()
    {
        resolutions = Screen.resolutions;
        fullscreen.isOn = Screen.fullScreen;
        useController.isOn = GlobalData.UseController;

        _myCustomProprieties["score"] = GlobalData.score;
        resolutionsDropdown.ClearOptions();
        List<string> options = new List<string>();
        int currentResolutionIndex = 0;
        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);
            if (resolutions[i].width == Screen.width && resolutions[i].height == Screen.height)
            {
                currentResolutionIndex = i;
            }
        }
        resolutionsDropdown.AddOptions(options);
        resolutionsDropdown.value = currentResolutionIndex;
        resolutionsDropdown.RefreshShownValue();
    }

    public void SetUseController(bool value)
    {
        GlobalData.UseController = value;
        PlayerPrefs.SetInt("controller", boolToInt(value));
    }

    int boolToInt(bool val)
    {
        if (val)
            return 1;
        else
            return 0;
    }

    private void OnEnable()
    {
        nameField.text = PhotonNetwork.NickName;
        characterDropdown.value = GlobalData.SelectedCharacter;
        regionDropdown.value = GlobalData.selectedRegion;
        useController.isOn = GlobalData.UseController;
        dummyKart.SetCharacter(GlobalData.SelectedCharacter);
        _myCustomProprieties = PhotonNetwork.LocalPlayer.CustomProperties;
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }

    public void SetRegion(int newReg)
    {
        GlobalData.selectedRegion = newReg;
        PlayerPrefs.SetInt("region", newReg);
    }

    public void ChangeCharacter(int newChar)
    {
        GlobalData.SelectedCharacter = newChar;
        PlayerPrefs.SetInt("character", newChar);
        _myCustomProprieties["character"] = newChar;
    }

    public void Save()
    {
        Discord_Controller.instance.UpdateStatusInfo("Enjoying the online experience", "Browsing the menus...", "maric_rast", "Image made by AI", GlobalData.charPngNames[GlobalData.SelectedCharacter], $"Currently playing as {GlobalData.charPngNames[GlobalData.SelectedCharacter]}");
        PlayerPrefs.Save();
        PhotonNetwork.LocalPlayer.CustomProperties = _myCustomProprieties;
    }

    public void SetName(string nickname)
    {
        if (string.IsNullOrWhiteSpace(nickname))
        {
            return;
        }
        PhotonNetwork.NickName = nickname;
        PlayerPrefs.SetString("nickname", nickname);
    }

    public void SetFullscreen(bool value)
    {
        Screen.fullScreen = value;
    }
}
