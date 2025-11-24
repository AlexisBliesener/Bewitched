using UnityEngine;
using System;

/// <summary>
/// This has to be attached to an enemy 
/// </summary>
public class EnemyHealth : HealthController
{
    [Header("Debug")]
    [SerializeField, Tooltip("Immediately kill this enemy")]
    bool kill;

    /// <summary>
    /// Override to show mini health bar when damaged by player
    /// </summary>
    public override void SubHealth(float amt)
    {
        // To show mini health bar when damaged by player if the player wasn't the enemy itself
        float finalDamage = amt;
        Enemy enemy = GetCharacter() as Enemy;

        if (enemy != null && !enemy.IsPlayerControlling())
        {
            // Apply Adrenaline buff if active
            if (Adrenaline.instance != null && Adrenaline.instance.IsBuffActive())
            {
                finalDamage = Adrenaline.instance.GetModifiedDamage(amt);
            }
            // Apply OffGuard if active
            if (OffGuard.instance != null && enemy.InAttackStartup())
            {
                finalDamage = OffGuard.instance.GetModifiedDamage(amt); // if the offguard is inactive, it will return the base damage (amt)
            }
            ShowMiniHealthBar(true, GetCharacter());
            if (characterAnimator != null)
            {
                StartCoroutine(characterAnimator.SetHit());
            }
        }
        base.SubHealth(finalDamage);
    }
    //This is used to force an enemy to die when toggling the kill bool in the inspector
    void OnValidate()
    {
        if (kill == true)
        {
            kill = false;
            SetCurrentHealth(0);
        }
    }
}