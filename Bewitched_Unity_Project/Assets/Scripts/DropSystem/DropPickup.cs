using System.Collections;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
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
    [Tooltip("The sound effect to play when the player picks up the drop")]
    private EventInstance dropSound;
    [Tooltip("If the player is in range of the drop")]
    public bool isPlayerInRange = false;
    // [Tooltip("The prefab of the UI that will be shown when the player nears the drop")]
    // public GameObject interactUI;

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
        //Sound Effect
        AudioManager.TryGetReference("UpgradeDrop", out EventReference evRef);
        dropSound = RuntimeManager.CreateInstance(evRef);
        dropSound.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject));
        dropSound.start();
        dropSound.release();
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
            if (character == PlayerController.instance.currentCharacter && !isPlayerInRange)
            {
                isPlayerInRange = true;
                PlayerController.instance.nearbyDrop = this;
                PlayerController.instance.ShowInteractUI();
                // Pickup();
            }
        }
    }
    /// <summary>
    /// This is called when the player is out of range of the drop
    /// it will set the nearby drop to null
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out Character character))
        {
            if (character == PlayerController.instance.currentCharacter && isPlayerInRange)
            {
                isPlayerInRange = false;
                if (PlayerController.instance.nearbyDrop == this)
                {
                    PlayerController.instance.nearbyDrop = null;
                    PlayerController.instance.HideInteractUI();
                }
            }
        }
    }
    /// <summary>
    /// This is called when the player is near the drop
    /// And this is used to avoid when two drops are near the player at the same time and the player intearct with one of them it will set the other drop to the nearby drop 
    /// </summary>
    private void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent(out Character character))
        {
            if (character == PlayerController.instance.currentCharacter)
            {
                if (PlayerController.instance.nearbyDrop == this)
                {
                    PlayerController.instance.ShowInteractUI();
                }
                else if (PlayerController.instance.nearbyDrop == null)
                {
                    PlayerController.instance.ShowInteractUI();
                    PlayerController.instance.nearbyDrop = this;
                }
            }
        }
    }
    /// <summary>
    /// This is called when the player picks up the drop
    /// It will trigger the drop selection event
    /// Might add another functionallity for that later
    /// </summary>
    public void Pickup()
    {
        if (!isPlayerInRange) return;
        PlayerController.instance.HideInteractUI();
        // Trigger the drop selection event
        DropSystem.Instance.ShowDropSelection(transform.position);
        //Sound Effect
        dropSound.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        // Draw pickup range in editor
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}
