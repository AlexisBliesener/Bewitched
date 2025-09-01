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
        base.SubHealth(amt);
        // To show mini health bar when damaged by player if the player wasn't the enemy itself
        Enemy enemy = GetCharacter() as Enemy;
        if (enemy != null && !enemy.IsPlayerControlling())
        {
            ShowMiniHealthBar(true, GetCharacter());
        }
    }

}