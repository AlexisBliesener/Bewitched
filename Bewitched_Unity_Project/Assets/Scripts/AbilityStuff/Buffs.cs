using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Buffs : MonoBehaviour
{
    [Header("Possession Orb Buffs")]
    [Tooltip("Number of Possession Orbs That Can be Fired Outside of Hag")]
    public int numExtraPossessionOrbs;

    [Header("Player Buffs")]

    [Tooltip("Player Speed Buff")]
    public float speedScalar;

    [Tooltip("Primary Attack Cooldown Percentage")]
    public float primaryCooldownPercent;

    [Tooltip("Secondary Attack Cooldown Percentage")]
    public float secondaryCooldownPercent;
}
