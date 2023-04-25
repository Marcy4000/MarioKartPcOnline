using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Items { mushroom, greenShell, redShell, blueShell, banana, star, fireFlower, goldenMushroom, bulletBill, blooper, lightning };

[CreateAssetMenu]
public class Item : ScriptableObject
{
    public Items itemType;
    public Sprite activeSprite;
    public int amount;
}
