using SimpleFileBrowser;
using System;
using System.Collections;
using UnityEngine;

public class EmblemEditor : MonoBehaviour
{
    public SkinnedMeshRenderer dummyKartEmblem;
    private Material dummyKartMat;
    private Texture2D emblem;
    [SerializeField] private GameObject dummyKart;

    private void Start()
    {
        dummyKartMat = dummyKartEmblem.material;
        if (!string.IsNullOrEmpty(PlayerPrefs.GetString("emblem")))
        {
            Texture2D tex = IMG2Sprite.LoadTextureFromBytes(Convert.FromBase64String(PlayerPrefs.GetString("emblem")));
            Debug.Log($"Set Texture {tex}");
            tex = IMG2Sprite.Resize(tex, 64, 64);
            emblem = tex;
            dummyKartMat.SetTexture("_DetailAlbedoMap", tex);
            //_myCustomProprieties["emblem"] = tex.EncodeToPNG();
        }
    }

    private void OnEnable()
    {
        //_myCustomProprieties = PhotonNetwork.LocalPlayer.CustomProperties;
        dummyKart.SetActive(true);
    }

    private void OnDisable()
    {
        dummyKart.SetActive(false);
    }

    public void LoadTexture()
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter("Images", ".jpg", ".png"));
        FileBrowser.SetDefaultFilter(".png");

        StartCoroutine(ShowLoadSpriteCoroutine());
    }

    IEnumerator ShowLoadSpriteCoroutine()
    {
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, null, null, "Select Image", "Load");

        Debug.Log(FileBrowser.Success);

        if (FileBrowser.Success)
        {
            for (int i = 0; i < FileBrowser.Result.Length; i++)
                Debug.Log(FileBrowser.Result[i]);

            string _path = FileBrowser.Result[0];
            Debug.Log(_path);
            Debug.Log("Loading Image");
            Texture2D tex = IMG2Sprite.LoadTexture(_path);
            Debug.Log($"Set Texture {tex}");
            tex = IMG2Sprite.Resize(tex, 64, 64);
            emblem = tex;
            dummyKartMat.SetTexture("_DetailAlbedoMap", tex);
            PlayerPrefs.SetString("emblem", Convert.ToBase64String(tex.EncodeToPNG()));
            //_myCustomProprieties["emblem"] = tex.EncodeToPNG();
        }
    }
}
