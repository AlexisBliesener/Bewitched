using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParentHitbox : MonoBehaviour
{
    public Character user;

    protected float thrustSpeed;
    public float duration;

    private List<Character> hitChars;
    protected float damage;
    protected float lastingTime;

    protected float currentSpeed = 0;

    protected Vector3 velocity;

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

    public virtual void Init(Character character, float dmg, float speed, float knockback)
    {
        user = character;
        hitChars = new List<Character>();
        damage = dmg;
        lastingTime = Time.time;
        velocity = user.transform.forward.normalized;
        thrustSpeed = speed;
    }

    void Update()
    {
        if (user == null)
        {
            Destroy(gameObject);
            return;
        }

        if (Time.time - lastingTime > duration) Destroy(gameObject);

        currentSpeed = Mathf.Lerp(currentSpeed, thrustSpeed, 1);
        velocity = user.transform.forward.normalized * currentSpeed;
        transform.position = transform.position + velocity * Time.deltaTime;
    }
}
