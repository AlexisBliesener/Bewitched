using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicHitbox : MonoBehaviour
{
    public float damage;
    public ParentHitbox parent;
    bool active = false;
    float knockback;

    public void Init(ParentHitbox pa, float dmg, float knock)
    {
        damage = dmg;
        parent = pa;
        active = true;
        knockback = knock;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (active)
        {
            if (other.TryGetComponent(out Character character) && character.teamID != parent.user.teamID)
            {
                if (!parent.HasBeenHit(character))
                {
                    character.SubHealth(damage);
                    character.GetComponent<KnockbackControl>().AddImpact(transform.forward.normalized, knockback);
                    parent.AddToHit(character);
                }
            }
            else if (other.gameObject.layer == LayerMask.NameToLayer("Environment"))
            {
                Destroy(parent.gameObject);
            }
        }
    }
}
