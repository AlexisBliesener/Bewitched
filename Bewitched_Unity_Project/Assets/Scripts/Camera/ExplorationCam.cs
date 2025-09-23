using UnityEngine;

/// <summary>
/// Handles positioning of the camera's "look at" point during exploration mode.
/// Ensures the look target stays offset relative to the character position.
/// </summary>
public class ExplorationCam : MonoBehaviour
{
    [SerializeField, Tooltip("The target point the camera should look at.")]
    private GameObject lookAtPoint;
    [SerializeField, Tooltip("Vertical offset applied to the look at point.")]
    private float yOffset;
    [SerializeField, Tooltip("The character object the camera follows.")]
    private GameObject character;

    /// <summary>
    /// Updates the look at point position every frame
    /// so the camera has a dynamic target relative to the character.
    /// </summary>
    private void Update()
    {
        lookAtPoint.transform.position = character.transform.position
                                       + Camera.main.transform.right
                                       + new Vector3(0, yOffset, 0);
    }
}
