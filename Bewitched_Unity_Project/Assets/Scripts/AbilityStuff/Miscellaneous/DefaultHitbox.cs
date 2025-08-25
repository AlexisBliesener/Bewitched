using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DefaultHitbox : MonoBehaviour
{
    public Character user;

    protected bool active = true;

    protected bool hitWall = false;

    protected float thrustSpeed;
    protected float rotationalSpeed;

    public float duration;

    private List<Character> hitChars;

    protected float damage;
    protected float slamDamage;
    protected float timeAlive;

    protected float slamDist;

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

    public void AddToHit(Character character)
    {
        hitChars.Add(character);
    }

    public bool HasBeenHit(Character character)
    {
        if (hitChars.Contains(character))
        {
            return true;
        }
        return false;
    }

    public void SetAttackName(string name)
    {
        attackName = name;
    }

    public void SetActive(bool val)
    {
        active = val;
    }

    public bool HasHitWall()
    {
        return hitWall;
    }

    public virtual void Init(Character character, float dmg = 0, float slamDMG = 0, float forwardVelocity = 0, float rotationalVelocity = 0, AttackStatusEffects status = null, float slamRange = 0)
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
        slamDist = slamRange;
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

        if (Time.time - timeAlive > duration) Destroy(gameObject);

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
                character.SubHealth(damage);
                AddStatusEffects(character);
                hitChars.Add(character);

                foreach (DefaultHitbox hitbox in children)
                {
                    hitbox.hitChars.Add(character);
                }

                parent.hitChars.Add(character);
            }
            else if (other.gameObject.layer == 8)
            {
                hitWall = true;
            }
        }
    }

    private void AddStatusEffects(Character character)
    {
        statusEffects.ApplyStatusEffects(user, character, this);
    }

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

    public void SlamImpact(AttackStatusEffects impactEffects)
    {
        Collider[] impacts = Physics.OverlapSphere(transform.position, slamDist, characters);

        for (int i = 0; i < impacts.Length; i++)
        {
            if (impacts[i].TryGetComponent(out Character hitChar) && hitChar.teamID != user.teamID)
            {
                impactEffects.ApplyStatusEffects(user, hitChar, this);
                hitChar.SubHealth(slamDamage);
            }
        }

        Destroy(gameObject);
    }
}

