using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// This script is attached to the shop alter object
// It will trigger the interaction event when the player is near the shop alter
public class ShopAlter : MonoBehaviour, IInteract
{
    /// <summary>
    /// It will set the object of the shop alter in the player controller, and show the interact UI
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Character character))
        {
            if (character == PlayerController.instance.currentCharacter)
            {
                PlayerController.instance.nearbyInteractable = this;
                PlayerController.instance.ShowInteractUI();
            }
        }
    }
    /// <summary>
    /// It will hide the interact UI when the player is out of range of the shop alter
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out Character character))
        {
            PlayerController.instance.nearbyInteractable = null;
            PlayerController.instance.HideInteractUI();
        }
    }
    /// <summary>
    /// This is called when the player interacts with the interactable object
    /// It will trigger the interaction event
    /// </summary>
    public void Interact()
    {
        DropSystem.Instance.OnShopAlterInteract?.Invoke();
    }

    public GameObject GetGameObject() => gameObject;
}
