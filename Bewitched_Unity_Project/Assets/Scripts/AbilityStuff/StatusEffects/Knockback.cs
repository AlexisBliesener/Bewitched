using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Knockback : StatusEffect
{
    private enum KnockbackType // For handling knockback
    {
        BasicForward,
        Impact,
        Swing,
        Bash
    };

    // For when knockback is constant
    private float knockbackAmount = 0;

    // For when knockback is in a range
    private float knockbackMinimum = 0;
    private float knockbackMaximum = 0;
    private float knockbackRange = 0;

    // The knockback type this is
    private KnockbackType knockbackType = KnockbackType.BasicForward;

    public override void ApplyEffect(Character user, Character character, DefaultHitbox hitbox)
    {
        if (knockbackType == KnockbackType.Bash)
        {
            Vector3 knockbackDirection = user.GetCurrentSpeedVector() + (character.transform.position - hitbox.transform.position).normalized;

            character.GetComponent<KnockbackControl>().AddImpact(knockbackDirection, knockbackAmount);
        }
        else if (knockbackType == KnockbackType.Swing)
        {
            float knockbackAngle = hitbox.transform.parent.rotation.eulerAngles.y - 90;
            Vector3 knockbackDirection = new Vector3(Mathf.Sin(knockbackAngle * Mathf.Deg2Rad), 0, Mathf.Cos(knockbackAngle * Mathf.Deg2Rad));

            character.GetComponent<KnockbackControl>().AddImpact(knockbackDirection, knockbackAmount);
        }
        else if (knockbackType == KnockbackType.Impact)
        {
            Vector3 knockbackDirection = character.transform.position - hitbox.transform.position;
            float distance = knockbackDirection.magnitude;

            float knockbackAmt = knockbackMaximum - Mathf.Lerp(knockbackMinimum, knockbackMaximum, distance / knockbackRange);


            character.GetComponent<KnockbackControl>().AddImpact(knockbackDirection.normalized, knockbackAmt);
        }
        else
        {
            character.GetComponent<KnockbackControl>().AddImpact(hitbox.transform.forward.normalized, knockbackAmount);
        }
    }
}
