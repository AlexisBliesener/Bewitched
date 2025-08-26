using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KnifeHitBox : MonoBehaviour
{
    public Character user;
    public float duration = 0.05f;
    float lastingTime;
    float damage;
    bool retracting;
    float thrustSpeed;
    float currentSpeed = 0;

    private List<Character> hitCharacters;

    public void Init(Character caller, float dmg, float speed)
    {
        user = caller;
        GetComponent<Rigidbody>().velocity = caller.transform.forward.normalized;
        lastingTime = Time.time;
        damage = dmg;
        retracting = false;
        thrustSpeed = speed;
        hitCharacters = new List<Character>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Character character) && character.teamID != user.teamID)
        {
            if (!hitCharacters.Contains(character))
            {
                character.SubHealth(damage);
                hitCharacters.Add(character);
            }
        }
    }

    void Update()
    {
        if (user == null)
        {
            Destroy(gameObject);
            return;
        }

        if (Time.time - lastingTime > duration)
        {
            user.SetPrimaryStatus(false);
            user.SetPrimaryAnimStatus(false);
            Destroy(gameObject);
        }
        
        currentSpeed = Mathf.Lerp(currentSpeed, thrustSpeed, 1);
        GetComponent<Rigidbody>().velocity = user.transform.forward.normalized * currentSpeed;
    }
}
