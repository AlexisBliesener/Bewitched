using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KnockbackCone : MonoBehaviour
{
    public Transform playerTrans;
    public float knockbackAmount;
    void OnTriggerEnter(Collider other)
    {
        if(!other.gameObject.GetComponent<Hag>() && other.gameObject.TryGetComponent<KnockbackControl>(out KnockbackControl knock)){
            if(!knock.gettingKnockback)knock.AddImpact((knock.transform.position - playerTrans.position).normalized, knockbackAmount);
        }
    }
}
