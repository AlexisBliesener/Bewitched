using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlamHitbox : MonoBehaviour
{
    Character character;
    float hitDamage;
    float slamDamage;
    float knockbackMin;
    float knockbackMax;
    float knockbackRange;

    List<Character> hitCharacters;

    public LayerMask characters;

    public void Init(Character user, float hitDmg, float slamDmg, float knockMin, float knockMax, float knockRange)
    {
        character = user;
        hitDamage = hitDmg;
        slamDamage = slamDmg;
        knockbackMin = knockMin;
        knockbackMax = knockMax;
        knockbackRange = knockRange;
        hitCharacters = new List<Character>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Character hitChar) && hitChar.teamID != character.teamID)
        {
            if (!hitCharacters.Contains(hitChar))
            {
                hitChar.SubHealth(hitDamage);
                hitCharacters.Add(hitChar);
            }
        }
    }

    public void SlamImpact()
    {
        Collider[] impacts = Physics.OverlapSphere(transform.position, knockbackRange, characters);

        for (int i = 0; i < impacts.Length; i++)
        {
            if (impacts[i].TryGetComponent(out Character hitChar) && hitChar.teamID != character.teamID)
            {
                hitChar.SubHealth(slamDamage);

                Vector3 knockbackDirection = hitChar.transform.position - transform.position;
                float distance = knockbackDirection.magnitude;

                float knockbackAmt = knockbackMax - Mathf.Lerp(knockbackMin, knockbackMax, distance / knockbackRange);

                hitChar.GetComponent<KnockbackControl>().AddImpact(knockbackDirection.normalized, knockbackAmt);
            }
        }

        Destroy(gameObject);
    }
}
