using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks characters that enter or exit the possession collider area.
/// Used to determine which characters can be possessed by the player.
/// </summary>
public class PossessionCollider : MonoBehaviour
{
    [Tooltip("List of characters currently inside the possession collider, excluding the player.")]
    private List<Character> charactersInPossession = new List<Character>();

    /// <summary>
    /// Gets the list of characters currently inside the possession collider.
    /// </summary>
    /// <returns>A list of characters available for possession.</returns>
    public List<Character> GetCharactersInPossession()
    {
        return charactersInPossession;
    }

    /// <summary>
    /// Called when another collider enters the possession trigger.
    /// Adds the character to the possession list if valid.
    /// </summary>
    /// <param name="other">The collider that entered the trigger.</param>
    private void OnTriggerEnter(Collider other)
    {
        Character character = other.GetComponent<Character>();
        if (character != null && character != PlayerController.instance.currentCharacter)
        {
            charactersInPossession.Add(character);
        }
    }

    /// <summary>
    /// Called when another collider exits the possession trigger.
    /// Removes the character from the possession list if valid.
    /// </summary>
    /// <param name="other">The collider that exited the trigger.</param>
    private void OnTriggerExit(Collider other)
    {
        Character character = other.GetComponent<Character>();
        if (character != null && character != PlayerController.instance.currentCharacter)
        {
            charactersInPossession.Remove(character);
        }
    }
}
