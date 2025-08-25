using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeStop : StatusEffect
{
    private float timeStopDuration = 0;
    public override void ApplyEffect(Character user, Character character, DefaultHitbox hitbox)
    {
        Time.timeScale = 0;
        character.StartTime(timeStopDuration);
    }
}
