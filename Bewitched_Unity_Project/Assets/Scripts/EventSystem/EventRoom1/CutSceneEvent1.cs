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
    /// <summary>
    /// This will set the player to the start position and set the flag to true
    /// </summary>
    private void Awake()
    {
        PlayerController.instance.GetHag().transform.position = startPosition.transform.position;
        PlayerController.instance.GetHag().transform.rotation = startPosition.transform.rotation;
        isMoving = true;
    }
    /// <summary>
    /// This will set the player to the start position and set the flag to true
    /// </summary>
    void OnEnable()
    {
        PlayerController.instance.GetHag().transform.position = startPosition.transform.position;
        PlayerController.instance.GetHag().transform.rotation = startPosition.transform.rotation;
        isMoving = true;
    }
    /// <summary>
    /// It will move the player to the target position
    /// </summary>
    private void Update()
    {
        if (isMoving)
        {
            PlayerController.instance.GetHag().transform.position = Vector3.Lerp(PlayerController.instance.GetHag().transform.position, targetPosition.transform.position, speedOfTheMovement * Time.deltaTime);
            if (Vector3.Distance(PlayerController.instance.GetHag().transform.position, targetPosition.transform.position) < 1f)
            {
                isMoving = false;
            }
        }
    }
}
