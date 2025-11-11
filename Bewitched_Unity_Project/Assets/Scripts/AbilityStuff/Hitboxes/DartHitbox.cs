using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Projectile hitbox for thrown darts
/// </summary>
public class DartHitbox : DefaultHitbox
{
    [Tooltip("Normalized direction of the dart")]
    private Vector3 moveDirection = Vector3.forward;

    /// <summary>
    /// initlalize the dart 
    /// </summary>
    public void InitDart(Character character, Vector3 direction, float dmg = 0, float slamDMG = 0, float forwardVelocity = 0, float rotationalVelocity = 0, AttackStatusEffects status = null, float attackDuration = 0)
    {
        moveDirection = direction.normalized;
        base.Init(character, dmg, slamDMG, forwardVelocity, rotationalVelocity, status, attackDuration);
    }

    void Update()
    {
        if (user == null)
        {
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
            return;
        }
        
        if (HasHitGroundOrEnvironment() ||
            ((user != PlayerController.instance.currentCharacter) && HasBeenHit(PlayerController.instance.currentCharacter)) // if player is not controlling and it hits the player, thne it will destory it (I did this so it will not be destroyed when it hit another enemy)
            || ((user == PlayerController.instance.currentCharacter) && hitChars.Count > 0)) { // if player is controlling and it hits another enemy, it will destory it since it hit the enemy
            Destroy(gameObject);
            return;
        }

        currentSpeed = Mathf.Lerp(currentSpeed, thrustSpeed, 1);
        currentRotationalSpeed = Mathf.Lerp(currentRotationalSpeed, rotationalSpeed, 1);
        velocity = moveDirection * currentSpeed;
        transform.position += velocity * Time.deltaTime;
    }
}
