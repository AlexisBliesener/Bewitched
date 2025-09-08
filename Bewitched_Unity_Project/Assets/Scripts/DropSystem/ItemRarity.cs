using UnityEngine;
/// <summary>
/// ItemRarity is a class the holds the rarity information for a drop.
/// It has a display name and a drop chance.
/// </summary>
[System.Serializable]
public class ItemRarity 
{
    [Header("Rarity Info")]
    [Tooltip("The display name of the rarity")]
    public string displayName;
    [Tooltip("The drop chance of the rarity")]
    [Range(1, 100)]
    public int dropChance;
}
