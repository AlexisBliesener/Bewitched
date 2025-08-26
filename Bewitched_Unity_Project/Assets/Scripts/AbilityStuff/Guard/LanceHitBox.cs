using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LanceHitBox : ParentHitbox
{
    [Header("Lance Tip and Handle Children")]
    [Tooltip("Lance Tip Prefab")]
    public GameObject lanceTipPrefab;
    [Tooltip("Lance Handle Prefab")]
    public GameObject lanceHandlePrefab;

    public float tipScalar = 2;

    private GameObject lanceTip;
    private GameObject lanceHandle;

    public override void Init(Character character, float dmg, float speed, float knockback)
    {
        Debug.Log("Creating Lance");
        base.Init(character, dmg, speed, knockback);

        lanceHandle = Instantiate(lanceHandlePrefab, transform);
        lanceHandle.GetComponent<BasicHitbox>().Init(this, damage, knockback);

        lanceTip = Instantiate(lanceTipPrefab, transform);
        lanceTip.GetComponent<BasicHitbox>().Init(this, damage * tipScalar, knockback);
    }

    private void Update()
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

    private void OnDestroy()
    {
        user.SetPrimaryStatus(false);
        user.SetPrimaryAnimStatus(false);
        Destroy(lanceTip);
        Destroy(lanceHandle);
    }
}
