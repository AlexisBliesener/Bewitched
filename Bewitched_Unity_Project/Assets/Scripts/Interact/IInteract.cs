using UnityEngine;
// <summary>
/// Interface for interactable objects that can be interacted with by the player
/// </summary>
public interface IInteract
{

    /// <summary>
    /// Gameobject that is attached to the interactable object
    /// </summary>
    GameObject GetGameObject();
    /// <summary>
    /// Called when the player interacts with the interactable object
    /// </summary>
    void Interact();
}