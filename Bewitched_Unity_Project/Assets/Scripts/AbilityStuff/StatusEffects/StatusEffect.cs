using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An abstract class describing a status effect carrying functions for how to apply effects
/// </summary>
public abstract class StatusEffect
{
    public abstract void ApplyEffect(Character user, Character character, DefaultHitbox hitbox);
}
