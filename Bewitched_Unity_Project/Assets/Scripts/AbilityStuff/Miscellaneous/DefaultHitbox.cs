using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A class for a hitbox
/// This hitbox will apply  effects through it on trigger with other characters
/// </summary>
public class DefaultHitbox : MonoBehaviour
{
    public Character user;

    protected bool active = true;

    protected bool hitWall = false;

    protected float thrustSpeed;
    protected float rotationalSpeed;

    public float duration;

    protected List<Character> hitChars;

    protected float damage;
    protected float slamDamage;
    protected float timeAlive;

    protected DefaultHitbox parent = null; 

    protected List<DefaultHitbox> children = new List<DefaultHitbox>();

    protected AttackStatusEffects statusEffects; // Will be changed to a class when it is made so that effects can be applied through there
    protected AttackStatusEffects impactEffects;

    protected float currentSpeed = 0;
    protected float currentRotationalSpeed = 0;

    protected Vector3 velocity;
    protected Quaternion rotationalVelocity;

    protected string attackName = ""; // Temporary for dealing with knockback types

    public LayerMask characters;

    /// <summary>
    /// Add a character to the list of hit characters
    /// </summary>
    /// <param name="character"> The character that has been hit </param>
    public void AddToHit(Character character)
    {
        if (!HasBeenHit(character))
        {
            hitChars.Add(character);
            parent.AddToHit(character);
            foreach (DefaultHitbox hitbox in children)
            {
                hitbox.AddToHit(character);
            }
        }
    }

    /// <summary>
    /// Checks to see if a character has been hit
    /// </summary>
    /// <param name="character"> Character to check </param>
    /// <returns></returns>
    public bool HasBeenHit(Character character)
    {
        if (hitChars.Contains(character))
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Sets the hitbox active/inactive
    /// </summary>
    /// <param name="val"> Bool for whether it should be active or not </param>
    public void SetActive(bool val)
    {
        active = val;
    }

    /// <summary>
    /// Checks to see if the hitbox has hit the wall
    /// </summary>
    /// <returns> True if it has, false otherwise </returns>
    public bool HasHitWall()
    {
        return hitWall;
    }

    /// <summary>
    /// Initialize function for a hitbox
    /// </summary>
    /// <param name="character"> Character using the hitbox </param>
    /// <param name="dmg"> Damage the hitbox deals </param>
    /// <param name="slamDMG"> Damage the slam impact deals </param>
    /// <param name="forwardVelocity"> Velocity of the hitbox moving forward </param>
    /// <param name="rotationalVelocity"> Velocity of the hitbox rotation </param>
    /// <param name="status"> Status effects of attack </param>
    /// <param name="attackDuration"> Duration of the attack </param>
    public virtual void Init(Character character, float dmg = 0, float slamDMG = 0, float forwardVelocity = 0, float rotationalVelocity = 0, AttackStatusEffects status = null, float attackDuration = 0)
    {
        user = character;
        hitChars = new List<Character>();
        damage = dmg;
        slamDamage = slamDMG;
        timeAlive = Time.time;
        velocity = user.transform.forward.normalized;
        thrustSpeed = forwardVelocity;
        rotationalSpeed = rotationalVelocity;
        statusEffects = status;
        characters = LayerMask.NameToLayer("Character");
        duration = attackDuration;
    }

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
                    character.SubHealth(damage);
                    AddStatusEffects(character);
                    AddToHit(character);

                    foreach (DefaultHitbox hitbox in children)
                    {
                        hitbox.AddToHit(character);
                    }
                    if (parent)
                    {
                        parent.AddToHit(character);
                    }
                }
            }
            else if (other.gameObject.layer == 8)
            {
                hitWall = true;
            }
        }
    }

    /// <summary>
    /// Adds status effects to the character
    /// </summary>
    /// <param name="character"> Character to add status effects to </param>
    private void AddStatusEffects(Character character)
    {
        if (character && user) // Both the applied character and user are still alive
        {
            Debug.Log(this);
            statusEffects.ApplyStatusEffects(user, character, this);
        }
    }

    /// <summary>
    /// Attaches another hitbox as a child
    /// </summary>
    /// <param name="hitbox"> Hitbox to add as child </param>
    public void AttachHitbox(DefaultHitbox hitbox)
    {
        children.Add(hitbox);
        hitbox.GetComponent<Transform>().SetParent(gameObject.transform);
        hitbox.parent = this;
    }

    public void OnDestroy()
    {
        Debug.Log("Destroying");
        foreach (DefaultHitbox child in children)
        {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// Slam function for impacts
    /// </summary>
    /// <param name="impactEffects"> Effects to use in slam </param>
    public void SlamImpact(AttackStatusEffects impactEffects)
    {
        Collider[] impacts = Physics.OverlapSphere(transform.position, impactEffects.GetKnockbackRange());
        Debug.Log(impactEffects.GetKnockbackRange());

        for (int i = 0; i < impacts.Length; i++)
        {
            Debug.Log(impacts[i]);
            if (impacts[i].TryGetComponent(out Character hitChar) && hitChar.teamID != user.teamID)
            {
                impactEffects.ApplyStatusEffects(user, hitChar, this);
                hitChar.SubHealth(slamDamage);
            }
        }

        Destroy(gameObject);
    }
}

