using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KnockbackControl : MonoBehaviour
{
    float mass = 3; // defines the character mass
    Vector3 impact = Vector3.zero;
    private CharacterController character;

    public bool gettingKnockback = false;

    void Start()
    {
        character = GetComponent<CharacterController>();
        mass = GetComponent<Character>().weight;
    }

    // call this function to add an impact force:
    public void AddImpact(Vector3 direction, float force)
    {
        float finalForce = force;
        if (GetOffOfMe.instance != null)
        {
            finalForce = GetOffOfMe.instance.GetModifiedKnockback(force); // if the GetOffOfMe upgrade is not active, it will return the base knockback (force)
        }
        direction.Normalize();
        impact += new Vector3(direction.x, 0, direction.z) * finalForce / mass;
        gettingKnockback = true;
    }

    void FixedUpdate()
    {
        // apply the impact force:
        if (impact.magnitude > 0.2f * mass)
        {
            character.Move(impact * Time.deltaTime);

            // consumes the impact energy each cycle:
            impact -= impact.normalized * mass * GetComponent<Character>().deceleration * Time.deltaTime;
        }
        else
        {
            if (impact != Vector3.zero)
            {
                impact = Vector3.zero;
                gettingKnockback = false;
            }
        }
    }

    /// <summary>
    /// Resets the impact
    /// </summary>
    public void ResetImpact()
    {
        impact = Vector3.zero;
    }
}
