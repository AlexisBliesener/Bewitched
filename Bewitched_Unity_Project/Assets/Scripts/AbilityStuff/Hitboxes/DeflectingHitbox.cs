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
    [Tooltip("Bool determining if a collision deflecting has already been applied")]
    private bool canDeflect = false;

    [Tooltip("Time range before this hitbox can deflect")]
    private float timeBeforeDeflect = 0.25f;

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

        if (!canDeflect && Time.time - timeAlive > timeBeforeDeflect)
        {
            canDeflect = true;
        }

        currentSpeed = Mathf.Lerp(currentSpeed, thrustSpeed, 1);
        currentRotationalSpeed = Mathf.Lerp(currentRotationalSpeed, rotationalSpeed, 1);

        velocity = user.transform.forward.normalized * currentSpeed;
        rotationalVelocity = new Quaternion(0, 0, 0, 0);

        transform.position = transform.position + velocity * Time.deltaTime;
        transform.rotation = transform.rotation;
    }

    /// <summary>
    /// Sets the deflect time
    /// </summary>
    /// <param name="time"> Time before this can deflect </param>
    public void SetDeflectTime(float time)
    {
        timeBeforeDeflect = time;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (active)
        {
            if (other.TryGetComponent(out Character character))
            {
                if (character && !hitChars.Contains(character) && character != user && !character.Invulnerable())
                {
                    character.health.SubHealth(damage);
                    AddStatusEffects(character);
                    AddToHit(character);

                    // Hit VFX
                    if (hitVFX != null)
                    {
                        Instantiate(hitVFX, new Vector3(character.transform.position.x,
                            character.transform.position.y + character.GetComponent<CharacterController>().height / 2,
                            character.transform.position.z), character.transform.rotation);
                    }
                    else
                    {
                        Debug.LogWarning("HitVFX is not assigned!");
                    }

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

            // If not colliding with the floor or self
            if (other.gameObject.layer != 6 && other.gameObject != user.gameObject && other.gameObject.layer != 9 && canDeflect)
            {
                // Check if other hit is on a hitbox
                if (other.TryGetComponent(out DefaultHitbox otherBox))
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

                if (user.TryGetComponent(out Goblin gob))
                {
                    gob.DeflectVelocity(other, this);
                }
            }
        }
    }
}
