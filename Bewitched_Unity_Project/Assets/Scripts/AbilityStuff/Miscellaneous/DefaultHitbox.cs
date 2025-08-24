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
    protected float timeAlive;

    protected DefaultHitbox parent = null; 

    protected List<DefaultHitbox> children;

    protected Dictionary<string, float> statusEffects; // Will be changed to a list of buff classes when they are made so that effects can be applied through there

    protected float currentSpeed = 0;
    protected float currentRotationalSpeed = 0;

    protected Vector3 velocity;
    protected Quaternion rotationalVelocity;

    protected string attackName = ""; // Temporary for dealing with knockback types

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

    public virtual void Init(Character character, float dmg = 0, float forwardVelocity = 0, float rotationalVelocity = 0, Dictionary<string, float> status = null)
    {
        user = character;
        hitChars = new List<Character>();
        damage = dmg;
        timeAlive = Time.time;
        velocity = user.transform.forward.normalized;
        thrustSpeed = forwardVelocity;
        rotationalSpeed = rotationalVelocity;
        statusEffects = status;
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
        /*
        foreach (statusEffects effect in statusEffects)
        {
            statusEffects.ApplyEffect(character);
        }
        */

        // Temporary until status effects are implemented
        if (statusEffects != null)
        {
            if (statusEffects.ContainsKey("knockback"))
            {
                if (attackName == "shieldBash")
                {
                    Vector3 knockbackDirection = user.GetCurrentSpeedVector() + (character.transform.position - transform.position).normalized;

                    character.GetComponent<KnockbackControl>().AddImpact(knockbackDirection, statusEffects["knockback"]);
                }
                else if (attackName == "batSwing")
                {
                    float knockbackAngle = transform.parent.rotation.eulerAngles.y - 90;
                    Vector3 knockbackDirection = new Vector3(Mathf.Sin(knockbackAngle * Mathf.Deg2Rad), 0, Mathf.Cos(knockbackAngle * Mathf.Deg2Rad));

                    character.GetComponent<KnockbackControl>().AddImpact(knockbackDirection, statusEffects["knockback"]);
                }
                else
                {
                    character.GetComponent<KnockbackControl>().AddImpact(transform.forward.normalized, statusEffects["knockback"]);
                }
            }

            if (statusEffects.ContainsKey("timeStop"))
            {
                Time.timeScale = 0;
                StartCoroutine(character.StartTime(statusEffects["timeStop"]));
            }
        }
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
}

