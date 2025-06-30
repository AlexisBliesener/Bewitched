using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShieldBashHitBox : MonoBehaviour
{
    public Guard character;

    private float damage;

    private float knockback;

    private bool hitWall;

    private List<Character> hitCharacters; // For ensuring we hit characters only once

    public void Init(Guard c, float dmg, float knock)
    {
        character = c;
        damage = dmg;
        hitWall = false;
        hitCharacters = new List<Character>();
        knockback = knock;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Character hitChar) && hitChar.teamID != character.teamID)
        {
            if (!hitCharacters.Contains(hitChar))
            {
                hitChar.SubHealth(damage);
                hitCharacters.Add(hitChar);

                Vector3 knockbackDirection = character.GetCurrentSpeedVector() + (hitChar.transform.position - transform.position).normalized;

                hitChar.GetComponent<KnockbackControl>().AddImpact(knockbackDirection, knockback);

                Time.timeScale = 0;
                StartCoroutine(character.StartTime(0.15f));
            }
        }
        else if (other.gameObject.layer == 8)
        {
            Debug.Log("wall");
            hitWall = true;
        }
    }

    public bool HitWall()
    {
        return hitWall;
    }
}
