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
        direction.Normalize();
        impact += new Vector3(direction.x, 0, direction.z) * force / mass;
        //PlayerController.instance.SetAllowMovement(false);
        gettingKnockback = true;
    }

    void FixedUpdate()
    {
        // apply the impact force:
        if (impact.magnitude > 0.2) character.Move(impact * Time.deltaTime);
        else if (gettingKnockback)
        {
            StartCoroutine(GetComponent<Character>().EnableMovement());
            gettingKnockback = false;
        }
        // consumes the impact energy each cycle:
        impact = Vector3.Lerp(impact, Vector3.zero, 5 * Time.deltaTime);
    }
}
