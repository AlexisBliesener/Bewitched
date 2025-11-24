using UnityEngine;

// This is the cut scene for the event system room 1
// It will teleport the player to the start position and then move the player to the target position
// This is will work with the timeline of the cut scene
public class CutSceneEvent1 : MonoBehaviour
{
    [SerializeField, Tooltip("The start position of the cut scene this will teleport the player to and will start the cut scene from there")]
    private GameObject startPosition;
    [SerializeField, Tooltip("The target position of the cut scene this will move the player to")]
    private GameObject targetPosition;
    [SerializeField, Tooltip("The speed of the movement")]
    private float speedOfTheMovement = 0.5f;
    [SerializeField, Tooltip("The flag to check if the player is moving")]
    private bool isMoving = false;
    [SerializeField, Tooltip("Reference to the Hag character script.")]
    protected Hag eleth;

    /// <summary>
    /// This will set the player to the start position and set the flag to true
    /// </summary>
    private void Start()
    {
        eleth = PlayerController.instance.GetHag();
        eleth.transform.position = startPosition.transform.position;
        eleth.transform.rotation = startPosition.transform.rotation;
        isMoving = true;
        eleth.GetComponent<CharacterAnimator>().OverrideAnimator("Run");
    }

    /// <summary>
    /// It will move the player to the target position
    /// </summary>
    private void Update()
    {
        if (isMoving)
        {
            eleth.transform.position = Vector3.Lerp(eleth.transform.position, targetPosition.transform.position, speedOfTheMovement * Time.deltaTime);
            if (Vector3.Distance(eleth.transform.position, targetPosition.transform.position) < 1f)
            {
                eleth.GetComponent<CharacterAnimator>().OverrideAnimator("Idle");
                isMoving = false;
            }
        }
    }

    private void OnDisable()
    {
        eleth.GetComponent<CharacterAnimator>().EndAnimatorOverride();
    }
}
