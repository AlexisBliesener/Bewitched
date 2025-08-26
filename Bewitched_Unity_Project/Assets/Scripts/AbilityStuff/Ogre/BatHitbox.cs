using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BatHitbox : MonoBehaviour
{

    Character character;
    float damage;
    float knockback;

    List<Character> hitCharacters;

    public void Init(Character user, float dmg, float knock)
    {
        character = user;
        damage = dmg;
        knockback = knock;
        hitCharacters = new List<Character>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Character hitChar) && hitChar.teamID != character.teamID)
        {
            if (!hitCharacters.Contains(hitChar))
            {
                hitChar.SubHealth(damage);
                hitCharacters.Add(hitChar);

                float knockbackAngle = transform.parent.rotation.eulerAngles.y - 90;
                Vector3 knockbackDirection = new Vector3(Mathf.Sin(knockbackAngle * Mathf.Deg2Rad), 0, Mathf.Cos(knockbackAngle * Mathf.Deg2Rad));

                hitChar.GetComponent<KnockbackControl>().AddImpact(knockbackDirection, knockback);

                Time.timeScale = 0;
                StartCoroutine(character.StartTime(0.15f));
            }
        }
    }
}
