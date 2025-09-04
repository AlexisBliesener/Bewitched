using UnityEngine;

[CreateAssetMenu(fileName = "New Rarity", menuName = "UpgradeSystem/Rarity")]
public class RarityData : ScriptableObject
{
    [Header("Rarity Info")]
    public string rarityName;
    [Range(1, 100)]
    public int dropChance;
}
