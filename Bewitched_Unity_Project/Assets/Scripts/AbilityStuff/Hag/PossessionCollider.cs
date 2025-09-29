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
    [SerializeField, Tooltip("The current character that is being controlled")]
    private Character currentCharacter;

    /// <summary>
    /// Used to set the currentCharacter variable
    /// It should only be set to the character the player is currently possessing
    /// </summary>
    /// <param name="character">The new currentCharacter</param>
    public void SetCurrentCharacter(Character character)
    {
        currentCharacter = character;
    }

    /// <summary>
    /// Gets the list of characters currently inside the possession collider.
    /// </summary>
    /// <returns>A list of characters available for possession.</returns>
    public List<Character> GetCharactersInPossession()
    {
        List<Character> characterToRemove = new List<Character>();
        foreach (Character character in charactersInPossession)
        {
            if(character == null)
            {
                characterToRemove.Add(character);
            }
        }

        foreach (Character character in characterToRemove)
        { 
             charactersInPossession.Remove(character);
        }

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
        if (character != null && character != currentCharacter)
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
        if (character != null && character != currentCharacter)
        {
            charactersInPossession.Remove(character);
        }
    }
}
