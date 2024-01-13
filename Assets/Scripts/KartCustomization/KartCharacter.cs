using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Character", menuName = "Kart/Create New Kart Character")]
public class KartCharacter : ScriptableObject
{
    [SerializeField] string characterName;

    [SerializeField] GameObject characterModel;

    [SerializeField] Material kartMaterial;

    public string CharacterName { get { return characterName; } }

    public GameObject CharacterModel { get { return characterModel; } }

    public Material KartMaterial { get {  return kartMaterial; } }
}
