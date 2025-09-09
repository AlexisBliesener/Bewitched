using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Pity system - increases drop chances for rarities that are NOT received,
/// resets when they are received as options.
/// </summary>
public class PitySystem : MonoBehaviour
{
    [Header("Pity Settings")]
    [Tooltip("How much to increase drop chance per failed attempt")]
    [SerializeField, Range(0, 100)] private float pityIncrement = 5f;
    [Tooltip("Maximum pity bonus that can be accumulated")]
    [SerializeField, Range(0, 100)] private float maxPityBonus = 50f;

    [Tooltip("To track the rarities and change the drop chance if not received.")]
    private Dictionary<ItemRarity, float> pityCounters = new Dictionary<ItemRarity, float>();

    /// <summary> Get the pity counters </summary>
    public Dictionary<ItemRarity, float> GetPityCounters() => pityCounters;
    /// <summary>
    /// To transfer the rarities from the drop system to the pity system.
    /// This is called when the drop system is initialized.
    /// </summary>
    public void Initialize(List<ItemRarity> rarities)
    {
        pityCounters.Clear();
        foreach (ItemRarity rarity in rarities.Distinct())
        {
            if (rarity != null)
                pityCounters[rarity] = 0f;
        }
    }

    /// <summary>
    /// Get drop chance with pity bonus applied
    /// </summary>
    public int GetModifiedDropChance(ItemRarity rarity)
    {
        if (rarity == null || !pityCounters.ContainsKey(rarity))
            return rarity?.dropChance ?? 0;

        float modifiedChance = rarity.dropChance + pityCounters[rarity];
        return Mathf.RoundToInt(Mathf.Min(modifiedChance, 100f));
    }

    /// <summary>
    /// Called after upgrades are shown - increases pity for rarities NOT received,
    /// resets pity for rarities that WERE received
    /// </summary>
    public void OnUpgradesOffered(ItemRarity offeredRarities)
    {
        foreach (ItemRarity rarity in pityCounters.Keys.ToList())
        {
            if (rarity == offeredRarities)
            {
                // This rarity was offered - reset its pity
                pityCounters[rarity] = 0f;
            }
            else
            {
                // This rarity was NOT offered - increase its pity
                pityCounters[rarity] = Mathf.Min(pityCounters[rarity] + pityIncrement, maxPityBonus);
            }
        }
    }
}