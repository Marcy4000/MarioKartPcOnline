using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour
{
    [SerializeField] private TMP_InputField nameField, bioField;
    [SerializeField] private Toggle fullscreen, useController, showName;
    [SerializeField] private DummyKart dummyKart;
    public TMP_Dropdown resolutionsDropdown, characterDropdown, regionDropdown;
    private Resolution[] resolutions;

    private void Start()
    {
        resolutions = Screen.resolutions;
        fullscreen.isOn = Screen.fullScreen;
        useController.isOn = GlobalData.UseController;
        showName.isOn = GlobalData.ShowName;

        //_myCustomProprieties["score"] = GlobalData.Score;
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
        //nameField.text = PhotonNetwork.NickName;
        //bioField.text = (string)PhotonNetwork.LocalPlayer.CustomProperties["bio"];
        characterDropdown.value = GlobalData.SelectedCharacter;
        regionDropdown.value = GlobalData.SelectedRegion;
        useController.isOn = GlobalData.UseController;
        //_myCustomProprieties = PhotonNetwork.LocalPlayer.CustomProperties;
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }

    public void SetRegion(int newReg)
    {
        GlobalData.SelectedRegion = newReg;
        PlayerPrefs.SetInt("region", newReg);
    }

    public void ChangeCharacter(int newChar)
    {
        GlobalData.SelectedCharacter = newChar;
        PlayerPrefs.SetInt("character", newChar);
        //_myCustomProprieties["character"] = newChar;
    }

    public void Save()
    {
        DiscordController.instance.UpdateStatusInfo("Enjoying the online experience", "Browsing the menus...", "maric_rast", "Image made by AI", GlobalData.CharPngNames[GlobalData.SelectedCharacter], $"Currently playing as {GlobalData.CharPngNames[GlobalData.SelectedCharacter]}");
        PlayerPrefs.Save();
        //PhotonNetwork.LocalPlayer.CustomProperties = _myCustomProprieties;
    }

    public void SetName(string nickname)
    {
        if (string.IsNullOrWhiteSpace(nickname))
        {
            return;
        }
        //PhotonNetwork.NickName = nickname;
        PlayerPrefs.SetString("nickname", nickname);
    }

    public void SetBio(string bio)
    {
        if (string.IsNullOrWhiteSpace(bio))
        {
            return;
        }
        PlayerPrefs.SetString("bio", bio);
        //_myCustomProprieties["bio"] = bio;
    }

    public void SetFullscreen(bool value)
    {
        Screen.fullScreen = value;
    }

    public void SetViewName(bool value)
    {
        GlobalData.ShowName = value;
        PlayerPrefs.SetInt("showName", boolToInt(value));
    }
}
