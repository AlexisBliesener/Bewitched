using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(BoxCollider))]
public class UpgradePickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    public float spinSpeed = 90f;
    public float pickupRange = 2f;

    private void Start()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(pickupRange, pickupRange, pickupRange);
        box.center = new Vector3(0, pickupRange / 2f, 0);
    }

    private void Update()
    {
        // Spin the upgrade
        transform.Rotate(0, spinSpeed * Time.deltaTime, 0);

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Pickup();
        }
    }
    private void Pickup()
    {
        // Trigger the upgrade selection event
        UpgradeSystem.Instance.ShowUpgradeSelection(transform.position);

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        // Draw pickup range in editor
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}
