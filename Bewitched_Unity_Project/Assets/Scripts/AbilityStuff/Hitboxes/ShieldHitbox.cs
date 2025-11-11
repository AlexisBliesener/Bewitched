using FMOD.Studio;
using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShieldHitbox : DefaultHitbox
{
    [Tooltip("Hitboxes that have been shielded")]
    List<DefaultHitbox> blockedBoxes = new List<DefaultHitbox>();

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
            if (other.TryGetComponent(out DefaultHitbox hitbox))
            {
                if (hitbox && !blockedBoxes.Contains(hitbox) && hitbox.GetUser() != user)
                {
                    blockedBoxes.Add(hitbox);

                    hitbox.AddToHit(user); // Adds character to hit so it does not hit again (effectively invulnerable to the attack)

                    // Hit VFX
                    if (hitVFX != null)
                    {
                        Instantiate(hitVFX, new Vector3(hitbox.transform.position.x,
                            hitbox.transform.position.y + hitbox.GetComponent<CharacterController>().height / 2,
                            hitbox.transform.position.z), hitbox.transform.rotation);
                    }
                    else
                    {
                        Debug.LogWarning("HitVFX is not assigned!");
                    }

                    //Hit sound effect implementation. Implement unique hit type later
                    string soundEffectKey = "Hit";
                    if (AudioManager.TryGetReference(soundEffectKey, out EventReference evRef))
                    {
                        EventInstance inst = RuntimeManager.CreateInstance(evRef);
                        inst.setParameterByName("Type", (float)damageType);
                        inst.start();
                        inst.release();
                    }
                    else Debug.LogError("Could not find a valid hit/death event. Is it assigned in the refSheet?");

                }
            }
        }
    }
}
