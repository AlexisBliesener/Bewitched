using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KnockbackOrb : MonoBehaviour
{
    //Reference to the hag class who shot this orb
    public Hag hag;
    float spawnTime;
    public float knockbackForce;

    public void Init(Vector3 velocity, Hag caller)
    {
        hag = caller;
        GetComponent<Rigidbody>().velocity = velocity;
        spawnTime = Time.time;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Character character) && character != hag)
        {
            Vector3 direction = character.transform.position - transform.position;
            character.GetComponent<KnockbackControl>().AddImpact(direction, knockbackForce);
            Destroy(gameObject);
        }
        else if (other.CompareTag("Wall")) Destroy(gameObject);


    }

    //For testing
    void Update()
    {
        if (Time.time - spawnTime > 5) Destroy(gameObject);
    }
}
