using UnityEngine;

[CreateAssetMenu(fileName = "New Rarity Item", menuName = "DropSystem/Rarity")]
/// <summary>
/// ItemRarity is a scriptable object that holds the rarity information for a drop.
/// It has a display name and a drop chance.
/// </summary>
public class ItemRarity : ScriptableObject
{
    [Header("Rarity Info")]
    [Tooltip("The display name of the rarity")]
    public string displayName;
    [Tooltip("The drop chance of the rarity")]
    [Range(1, 100)]
    public int dropChance;
}
