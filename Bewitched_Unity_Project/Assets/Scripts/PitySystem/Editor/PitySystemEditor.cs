using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(PitySystem))]
public class PitySystemEditor : Editor
{
    [Tooltip("The reference of the pity system")]
    private PitySystem pitySystem;
    /// <summary>
    /// This is going to store the pity system refrence and the last tool used in the editor
    /// </summary>
    private void OnEnable()
    {
        pitySystem = (PitySystem)target;
    }
    /// <summary>
    /// Draw the default inspector first
    /// Then draw the pity counters
    /// This is going to show the rarities and their drop chances with the pity bonus
    /// </summary>
    public override void OnInspectorGUI()
    {

        // Draw the default inspector first
        DrawDefaultInspector();

        Dictionary<ItemRarity, float> pityCounters = pitySystem.GetPityCounters();

        if (pityCounters != null && pityCounters.Count > 0)
        {
            foreach (KeyValuePair<ItemRarity, float> keyValuePair in pityCounters)
            {
                ItemRarity rarity = keyValuePair.Key;
                float pityBonus = keyValuePair.Value;

                if (rarity != null)
                {
                    int totalChance = pitySystem.GetModifiedDropChance(rarity);
                    EditorGUILayout.LabelField($"{rarity.displayName}: {rarity.dropChance}% + {pityBonus}% = {totalChance}%");
                }
            }
        }

        Repaint();

    }
}