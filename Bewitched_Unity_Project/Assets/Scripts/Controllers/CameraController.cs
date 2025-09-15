using Cinemachine;
using FMODUnity;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Manages third-person and aiming camera behavior using Cinemachine.
/// Handles switching between free-look and aiming cameras, crosshair visibility,
/// player input for camera rotation/aiming, and updates FMOD audio listener settings
/// when switching controlled characters.
/// </summary>
public class CameraController : MonoBehaviour
{
    [SerializeField, Tooltip("Duration in seconds to prevent camera switching during transitions.")]
    private const float TRANSITION_TIME = 2;

    [SerializeField, Tooltip("The free-look Cinemachine camera used for general third-person movement.")]
    private CinemachineFreeLook freeLookCam;
    [SerializeField, Tooltip("The Cinemachine virtual camera used for aiming (shoulder view).")]
    private CinemachineVirtualCamera virtualCam;
    [SerializeField, Tooltip("The currently controlled character whose perspective the camera follows.")]
    private Character currentCharacter;
    [SerializeField, Tooltip("Crosshair image displayed on screen while aiming.")]
    private Image crossHair;


    [Tooltip("The FMOD studio listener attached to the camera for 3D audio spatialization.")]
    private StudioListener listener;
    [Tooltip("Whether the player is currently aiming.")]
    private static bool aiming = false;
    [Tooltip("Reference to the AimCam component that manages aim-related camera logic.")]
    private AimCam aimCam;
    [Tooltip("Flag to prevent camera priority switching during character transitions.")]
    private bool transitioning = false;

    /// <summary>
    /// Returns whether the player is currently aiming.
    /// </summary>
    public static bool GetIsAiming()
    {
        return aiming;
    }

    /// <summary>
    /// Initializes references and sets up camera priorities and FMOD listener.
    /// </summary>
    private void Awake()
    {
        aiming = false;
        aimCam = virtualCam.GetComponent<AimCam>();
        freeLookCam.Priority = 2;
        virtualCam.Priority = 1;
        aimCam.SetYaw(freeLookCam.m_XAxis.Value);

        // FMOD set up
        if (!listener) listener = GetComponent<StudioListener>();
        if (!listener.attenuationObject) listener.attenuationObject = currentCharacter.gameObject;
    }

    /// <summary>
    /// Updates camera priorities based on whether the player is aiming.
    /// Prevents switching while in a transition.
    /// </summary>
    private void UpdateCam()
    {
        if (transitioning) return;

        if (aiming)
        {
            freeLookCam.Priority = 1;
            virtualCam.Priority = 2;
        }
        else
        {
            freeLookCam.Priority = 2;
            virtualCam.Priority = 1;
        }
    }

    /// <summary>
    /// Handles camera rotation based on player input.
    /// Updates the yaw using mouse/gamepad look input.
    /// </summary>
    /// <param name="context">The input context containing look delta values.</param>
    public void Look(InputAction.CallbackContext context)
    {
        aimCam.Look(context);
    }

    /// <summary>
    /// Toggles aiming mode and updates the camera/crosshair state.
    /// </summary>
    /// <param name="context">The input action context (started/canceled).</param>
    public void Aim(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            aimCam.SetYaw(freeLookCam.m_XAxis.Value);
            crossHair.gameObject.SetActive(true);
            aiming = true;
        }
        else if (context.canceled)
        {
            crossHair.gameObject.SetActive(false);
            aiming = false;
        }
        UpdateCam();
    }

    /// <summary>
    /// Keeps the free-look camera aligned with the current character while aiming.
    /// </summary>
    private void Update()
    {
        if (aiming)
        {
            freeLookCam.transform.rotation = currentCharacter.gameObject.transform.rotation;
            freeLookCam.transform.position = currentCharacter.gameObject.transform.position;
        }
    }

    /// <summary>
    /// Unsubscribes from character control change events when disabled.
    /// </summary>
    private void OnDisable()
    {
        PossessionAbility.CharacterControlChangeEvent -= SwitchCharacter;
    }

    /// <summary>
    /// Subscribes to character control change events when enabled.
    /// </summary>
    private void OnEnable()
    {
        PossessionAbility.CharacterControlChangeEvent += SwitchCharacter;
    }

    /// <summary>
    /// Switches the camera to follow a new character.
    /// Updates FMOD listener, Cinemachine follow/look targets, and AimCam reference.
    /// </summary>
    /// <param name="character">The new character to follow.</param>
    private void SwitchCharacter(Character character)
    {
        transitioning = true;
        StartCoroutine(WaitTransitionTime());

        virtualCam.Priority = 0;
        freeLookCam.Priority = 0;

        currentCharacter = character;
        if (!listener.attenuationObject) listener.attenuationObject = currentCharacter.gameObject;

        virtualCam = character.GetVirtualCam();
        freeLookCam = character.GetFreeLookCam();

        try
        {
            aimCam = virtualCam.GetComponent<AimCam>();
        }
        catch
        {
            Debug.LogWarning("No aim cam component found!");
        }

        freeLookCam.Priority = 2;
        virtualCam.Priority = 1;

        UpdateCam();
    }

    /// <summary>
    /// Waits for the defined transition time before allowing camera switching again.
    /// </summary>
    private IEnumerator WaitTransitionTime()
    {
        yield return new WaitForSeconds(TRANSITION_TIME);
        transitioning = false;
    }
}
