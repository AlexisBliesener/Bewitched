using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This is a class for storing status effects for attacks
/// It will hold instructions for how to apply certain status effects (such as knockback)
/// and carry out applying the effects
/// </summary>
public class AttackStatusEffects
{
    private List<StatusEffect> statusEffects = new List<StatusEffect>();

    public void ApplyStatusEffects(Character user, Character character, DefaultHitbox hitbox)
    {
        foreach (StatusEffect effect in statusEffects)
        {
            effect.ApplyEffect(user, character, hitbox);
        }
    }
}
