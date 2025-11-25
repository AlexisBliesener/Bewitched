using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls the aiming camera behavior for a character.
/// Handles camera yaw updates, look input processing, and syncing 
/// the camera POV with the character while aiming.
/// </summary>
public class AimCam : MonoBehaviour
{
    [SerializeField, Tooltip("Sensitivity multiplier of the camera x-axis movement (horizontal look).")]
    private float xSensitivity = 0.5f;
    [SerializeField, Tooltip("Sensitivity multiplier of the camera y-axis movement (vertical look).")]
    private float ySensitivity = 0.5f;
    [SerializeField, Tooltip("The virtual camera that is following the player.")]
    private CinemachineVirtualCamera virtualCamera;

    [Tooltip("The character that the camera is following.")]
    private Character characterToFollow;
    [Tooltip("The POV component of the virtual camera used to apply player input.")]
    private CinemachinePOV cameraPOVComponent;
    [Tooltip("The y-axis rotation applied to the player based on mouse or controller movement.")]
    private float yaw = 0;
    [Tooltip("Prev yaw of the aim cam last frame")]
    private float prevYaw = 0;
    [Tooltip("The look direction of the camera")]
    private Vector2 lookDir = Vector2.zero;

    /// <summary>
    /// Initializes references to the character, virtual camera, and POV component.
    /// </summary>
    private void Awake()
    {
        characterToFollow = GetComponentInParent<Character>();
        virtualCamera = GetComponent<CinemachineVirtualCamera>();
        cameraPOVComponent = virtualCamera.GetCinemachineComponent<CinemachinePOV>();
    }

    /// <summary>
    /// Updates the camera and character rotation when aiming is active.
    /// Ensures the player's yaw and camera POV stay aligned.
    /// </summary>
    private void Update()
    {
        if (virtualCamera.Priority < 2) return;

        cameraPOVComponent.m_VerticalAxis.m_MaxSpeed = 300 * ySensitivity;

        //if (CameraController.GetIsAiming())
        //{
        //    // Scale and apply input to yaw
        //    yaw += lookDir.x * xSensitivity;
        //    characterToFollow.transform.Rotate(new Vector3(0, yaw - prevYaw, 0)); 
        //    cameraPOVComponent.m_HorizontalAxis.Value = characterToFollow.transform.rotation.y;
        //    prevYaw = yaw;
        //}
    }

    /// <summary>
    /// Sets the current yaw value for the camera.
    /// Used when switching from freelook to aiming mode to align rotations.
    /// </summary>
    /// <param name="yaw">The yaw angle to set.</param>
    public void SetYaw(float yaw)
    {
        this.yaw = yaw;
        prevYaw = yaw;
    }

    /// <summary>
    /// Handles look input from mouse or controller to adjust yaw and camera axes.
    /// Scales sensitivity differently depending on input device type.
    /// </summary>
    /// <param name="context">The input context containing look direction values.</param>
    public void Look(InputAction.CallbackContext context)
    {
        Vector2 lookInput = context.ReadValue<Vector2>();

        if (context.action.activeControl.device.description.deviceClass != "Mouse")
        {
            lookInput.x *= 20;
        }

        lookDir = lookInput;
    }
}
