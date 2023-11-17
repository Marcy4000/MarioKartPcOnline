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
    public static bool AllPlayersLoaded = false;

    public static int SelectedRegion = 0;

    public static int Score = 0;

    public static bool ShowName = true;

    public static string[] Regions =
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

    public static string[] Stages =
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
        "WiiRainbowRoad",
        "8MarioCircuit",
        "MushroomGorge"
    };

    public static string[] CharPngNames =
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


    public static Quaternion FixQuaternion(Quaternion original)
    {
        Quaternion result = original;

        if (original.x + original.y + original.z + original.w == 0)
        {
            result = Quaternion.identity;
        }

        return result;
    }
}
