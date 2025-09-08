using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(BoxCollider))]
/// <summary>
/// DropPickup is a prefab that can be used to pick up drops.
/// It has a box collider and a spin speed.
/// It will be used to pick up drops from enemies.
/// </summary>
public class DropPickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    [Tooltip("The speed of the spin of the drop")]
    public float spinSpeed = 90f;
    [Tooltip("The range for picking up the drop")]
    public float pickupRange = 2f;
    /// <summary>
    /// Get the box collider component and activate it
    /// Set the isTrigger to true
    /// </summary>
    private void Start()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(pickupRange, pickupRange, pickupRange);
        box.center = new Vector3(0, pickupRange / 2, 0);
    }
    /// <summary>
    /// Rotate the drop
    /// </summary>
    private void Update()
    {
        // Spin the drop
        transform.Rotate(0, spinSpeed * Time.deltaTime, 0);

    }
    /// <summary>
    /// This is called when the player picks up the drop
    /// It will trigger the drop selection event
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Character character))
        {
            if (character == PlayerController.instance.currentCharacter)
            {
                Pickup();
            }
        }
    }
    /// <summary>
    /// This is called when the player picks up the drop
    /// It will trigger the drop selection event
    /// Might add another functionallity for that later
    /// </summary>
    private void Pickup()
    {
        // Trigger the drop selection event
        DropSystem.Instance.ShowDropSelection(transform.position);

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        // Draw pickup range in editor
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}
