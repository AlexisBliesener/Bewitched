using UnityEngine;
using System;

/// <summary>
/// This has to be attached to an enemy 
/// </summary>
public class EnemyHealth : HealthController
{   
    /// <summary>
    /// Override to show mini health bar when damaged by player
    /// </summary>
    public override void SubHealth(float amt)
    {
        // To show mini health bar when damaged by player if the player wasn't the enemy itself
        Enemy enemy = GetCharacter() as Enemy;
        if (enemy != null && !enemy.IsPlayerControlling())
        {
            float finalDamage = amt;
            // Apply Adrenaline buff if active
            if (Adrenaline.instance != null && Adrenaline.instance.IsBuffActive())
            {
                finalDamage = Adrenaline.instance.GetModifiedDamage(amt);
            }
            base.SubHealth(finalDamage);
            //Debug.Log("Damage: " + finalDamage + " Buff: " + Adrenaline.instance.IsBuffActive() + " Stack: " + Adrenaline.instance.stackNum + " Damage was: " + amt);
            ShowMiniHealthBar(true, GetCharacter());
        }
    }

}