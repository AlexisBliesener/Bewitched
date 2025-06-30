using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashHitBox : MonoBehaviour
{
    public Character character;

    private float damage;

    private bool hitWall;

    private List<Character> hitCharacters; // For ensuring we hit characters only once

    public void Init(Character c, float dmg)
    {
        character = c;
        damage = dmg;
        hitWall = false;
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
