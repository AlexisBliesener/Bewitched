using FMOD.Studio;
using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A derived class for a hitbox that handles deflection capabilities
/// </summary>
public class DeflectingHitbox : DefaultHitbox
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (user == null)
        {
            Debug.Log("No User");
            Destroy(gameObject);
            foreach (DefaultHitbox child in children)
            {
                Destroy(child.gameObject);
            }
            return;
        }

        if (Time.time - timeAlive > duration)
        {
            user.EndAttacks();
            Destroy(gameObject);
        }

        currentSpeed = Mathf.Lerp(currentSpeed, thrustSpeed, 1);
        currentRotationalSpeed = Mathf.Lerp(currentRotationalSpeed, rotationalSpeed, 1);

        velocity = user.transform.forward.normalized * currentSpeed;
        rotationalVelocity = new Quaternion(0, 0, 0, 0);

        transform.position = transform.position + velocity * Time.deltaTime;
        transform.rotation = transform.rotation;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (active)
        {
            if (other.TryGetComponent(out Character character))
            {
                if (character && character.teamID != user.teamID && !hitChars.Contains(character))
                {
                    character.health.SubHealth(damage);
                    AddStatusEffects(character);
                    AddToHit(character);
                    //Hit sound effect implementation. Implement unique hit type later
                    string soundEffectKey = character.health.IsDead ? "Death" : "Hit";
                    if (AudioManager.TryGetReference(soundEffectKey, out EventReference evRef))
                    {
                        EventInstance inst = RuntimeManager.CreateInstance(evRef);
                        inst.setParameterByName("Type", (float)damageType);
                        inst.start();
                        inst.release();
                    }
                    else Debug.LogError("Could not find a valid hit/death event. Is it assigned in the refSheet?");

                    foreach (DefaultHitbox hitbox in children)
                    {
                        hitbox.AddToHit(character);
                    }
                    if (parent)
                    {
                        parent.AddToHit(character);
                    }
                }
                else if (character == user) // If hit the user, return
                {
                    return;
                }
            }
            else if (other.gameObject.layer == 8)
            {
                hitWall = true;
            }

            // If not colliding with the floor
            if (other.gameObject.layer != 6)
            {
                // Check if other hit is on a hitbox
                if (other.TryGetComponent<DefaultHitbox>(out DefaultHitbox otherBox))
                {
                    Debug.Log("Hit other");
                    // If it is a projectile hitbox (future) only apply deflection to hitbox

                    if (otherBox.GetType() == typeof(DeflectingHitbox)) // If it is a deflecting hitbox deflect the enemy velocity too
                    {
                        otherBox.GetUser().DeflectVelocity(other.transform.position - user.transform.position);
                    }
                    else // Otherwise just apply knockback to enemy
                    {
                        statusEffects.ApplyKnockback(user, otherBox.GetUser(), this);
                    }
                }

                if (other.TryGetComponent<Character>(out Character charac))
                {
                    Debug.Log("Deflecting off of: " + charac);
                }
                // Now deflect user's velocity away from hit if the collision was not with the floor
                user.DeflectVelocity(user.transform.position - other.transform.position);
            }
        }
    }
}
