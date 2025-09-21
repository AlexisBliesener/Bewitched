using UnityEngine;
/// <summary>
/// This class handles the behavior of soul drops in the game.
/// Souls are attracted to the player when they come within a certain range,
/// and are collected when they reach the player.
/// </summary>
public class SoulDrop : MonoBehaviour
{
    [Tooltip("The is used to determine if the soul is attracted to the player in the update function")]
    private bool isAttracted = false;
    [Header("Pickup Settings")]
    [SerializeField,Tooltip("How close player needs to be before attraction starts? in meters")]
    private float attractionRange = 5f;
    [SerializeField,Tooltip("The speed of the souls move toward the player")]
    private float attractionSpeed = 10f;
    [SerializeField,Tooltip("How many souls this drop gives")]
    private int soulValue = 1;
    // <summary>
    /// Starts attracting the soul to the player when the player is within the attraction range, and picks up the soul when it is close enough
    /// </summary>
    private
    void Update()
    {
        float distance = Vector3.Distance(transform.position, PlayerController.instance.currentCharacter.transform.position);

        // If player is within range, start attraction
        if (distance <= attractionRange)
        {
            isAttracted = true;
        }

        if (isAttracted)
        {
            // Move the soul toward player
            transform.position = Vector3.MoveTowards(transform.position, PlayerController.instance.currentCharacter.transform.position, attractionSpeed * Time.deltaTime);
            // Check if close enough to pick up
            if (distance < 1f)
            {
                // Give the soul to the system
                SoulSystem.Instance.AddSouls(soulValue);

                Destroy(gameObject);
            }
        }
    }
}
