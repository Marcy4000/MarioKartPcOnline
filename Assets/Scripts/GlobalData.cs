using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public static class GlobalData
{
    public static int SelectedStage = 0;
    public static int SelectedCharacter = 0;
    public static bool UseController, HasIntroPlayed = false;

    public const int PlayerCount = 12;

    public static bool HasSceneLoaded = false;

    public static int selectedRegion = 0;

    public static int score = 0;

    public static string[] regions =
    {
        "eu",
        "us",
        "jp",
        "ru",
        "asia",
        "usw",
        "sa",
        "za",
        "cae"
    };

    public static string[] stages =
    {
        "LuigiCircuit",
        "Figure8Circuit",
        "WaluigiStadium",
        "KoopaCave",
        "CoconutMall",
        "YoshiFalls",
        "WaluigiPinball",
        "YoshiCircuit",
        "ToadsFactory",
        "GokuStage",
        "LuigisMansion",
        "BabyPark",
        "WiiRainbowRoad"
    };

    public static string[] charPngNames =
    {
        "mario",
        "luigi",
        "yoshi",
        "waluigi",
        "ermes",
        "toad",
        "vegito",
        "luffy",
        "marcy",
        "donkeykong",
        "raboot",
        "sonic",
        "peach",
        "rosalina",
        "eggman",
        "peppebrescia",
        "peppino"
    };
}
