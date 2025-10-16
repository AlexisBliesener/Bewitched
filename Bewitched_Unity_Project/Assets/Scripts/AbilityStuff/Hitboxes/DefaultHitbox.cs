using System.Collections;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

/// <summary>
/// A class for a hitbox
/// This hitbox will apply effects through it on trigger with other characters
/// </summary>
public class DefaultHitbox : MonoBehaviour
{
    [Header("VFX")]
    [SerializeField, Tooltip("Hit VFX")]
    protected GameObject hitVFX;

    [Tooltip("The Character using this Hitbox")]
    protected Character user;

    [Tooltip("If the Hitbox is Active")]
    protected bool active = true;

    [Tooltip("If the Hitbox has Hit a Wall")]
    protected bool hitWall = false;

    [Tooltip("Forward Speed")]
    protected float thrustSpeed;
    [Tooltip("Rotational Speed")]
    protected float rotationalSpeed;

    [SerializeField, Tooltip("The Duration the Hitbox Stays Alive")]
    protected float duration;

    [Tooltip("The Characters this Hitbox has hit")]
    protected List<Character> hitChars;

    [Tooltip("The Damage Dealt")]
    protected float damage;

    [Tooltip("The Slam Damage Dealt")]
    protected float slamDamage;

    [Tooltip("The Time the Hitbox has been Alive")]
    protected float timeAlive = 0;

    [Tooltip("The Parent of this Hitbox")]
    protected DefaultHitbox parent = null; 

    [Tooltip("The Children of this Hitbox")]
    protected List<DefaultHitbox> children = new List<DefaultHitbox>();

    [Tooltip("Standard Status Effects")]
    protected AttackStatusEffects statusEffects;

    [Tooltip("The Slam Status Effects")]
    protected AttackStatusEffects impactEffects;

    [Tooltip("The Current Speed")]
    protected float currentSpeed = 0;

    [Tooltip("The Current Rotational Speed")]
    protected float currentRotationalSpeed = 0;

    [Tooltip("The Current Velocity")]
    protected Vector3 velocity;

    [Tooltip("The Current Rotational Velocity")]
    protected Quaternion rotationalVelocity;
    protected enum eHitType {blunt,bladed,unique};
    [SerializeField] protected eHitType damageType = eHitType.blunt;

    /// <summary>
    /// Add a character to the list of hit characters
    /// </summary>
    /// <param name="character"> The character that has been hit </param>
    public void AddToHit(Character character)
    {
        if (!HasBeenHit(character))
        {
            //Add implementation for unique hits later
            hitChars.Add(character);
            if (parent)
            {
                parent.AddToHit(character);
            }
            foreach (DefaultHitbox hitbox in children)
            {
                hitbox.AddToHit(character);
            }
        }
        user.SetHitCharacter(true);
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
        duration = attackDuration;
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
                if (character && !hitChars.Contains(character) && character != user && !character.Invulnerable() && character.teamID != user.teamID)
                {
                    character.health.SubHealth(damage);

                    if(character == PlayerController.instance.currentCharacter)
                    {
                        CameraController.instance.PlayerHitBy(user.gameObject);
                    }

                    // Increments the possession ability charge if the hit has done to anything other than the player
                    if(character != PlayerController.instance.currentCharacter)
                    {
                        PossessionAbility.instance.AddHitDone();
                    }

                    // Hit VFX
                    if(hitVFX != null)
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
                    string soundEffectKey = character.health.IsDead? "Death" : "Hit";
                    if (AudioManager.TryGetReference(soundEffectKey, out EventReference evRef))
                    {
                        EventInstance inst = RuntimeManager.CreateInstance(evRef);
                        inst.setParameterByName("Type", (float)damageType);
                        inst.start();
                        inst.release();
                    }
                    else Debug.LogError("Could not find a valid hit/death event. Is it assigned in the refSheet?");

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
    protected void AddStatusEffects(Character character)
    {
        if (character && user) // Both the applied character and user are still alive
        {
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
                hitChar.health.SubHealth(slamDamage);
            }
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// Gets the user
    /// </summary>
    /// <returns> User of this hitbox </returns>
    public Character GetUser()
    {
        return user;
    }
}


