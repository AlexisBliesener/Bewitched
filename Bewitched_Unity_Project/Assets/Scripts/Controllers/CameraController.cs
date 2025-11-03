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
    public static CameraController instance;

    [SerializeField, Tooltip("Duration in seconds to prevent camera switching during transitions.")]
    private const float TRANSITION_TIME = 2;

    [SerializeField, Tooltip("The weight of the threat of the enemy holds in camera assistance")]
    private int threatWeight;

    [SerializeField, Tooltip("The weight of the distance of the enemy holds in camera assistance")]
    private int distWeight;

    [SerializeField, Tooltip("The max distance a threat should be considered in")]
    private float maxDistance;

    [SerializeField, Tooltip("The free-look Cinemachine camera used for combat view.")]
    private CinemachineFreeLook combatCam;
    [SerializeField, Tooltip("The Cinemachine virtual camera used for aiming (shoulder view).")]
    private CinemachineVirtualCamera aimCam;
    [SerializeField, Tooltip("The Cinemachine virtual camera used for exploration view")]
    private CinemachineFreeLook explorationCam;
    [SerializeField, Tooltip("The currently controlled character whose perspective the camera follows.")]
    private Character currentCharacter;
    [SerializeField, Tooltip("Crosshair image displayed on screen while aiming.")]
    private Image crossHair;

    [Tooltip("The FMOD studio listener attached to the camera for 3D audio spatialization.")]
    private StudioListener listener;
    [Tooltip("Whether the player is currently aiming.")]
    private static bool aiming = false;
    [Tooltip("Reference to the AimCam component that manages aim-related camera logic.")]
    private AimCam aimCamScript;
    [Tooltip("Reference to the CombatCam component that manages framing camera logic.")]
    private CombatCam combatCamScript;
    [Tooltip("Flag to prevent camera priority switching during character transitions.")]
    private bool transitioning = false;
    [Tooltip("True if the player is locked in a room")]
    private bool inCombat = false;

    /// <summary>
    /// Sets whether the player is in combat or not.
    /// </summary>
    /// <param name="val">Whether the player is in combat or not.</param>
    public void SetInCombat(bool val)
    {
        inCombat = val;
        UpdateCam();
    }

    public int GetThreatWeight()
    {
        return threatWeight;
    }

    public int GetDistWeight()
    {
        return distWeight;
    }

    public float GetMaxDistance()
    {
        return maxDistance;
    }

    public CombatCam GetCombatCamScript()
    {
        return combatCamScript;
    }

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
        instance = this;
        aiming = false;
        aimCamScript = aimCam.GetComponent<AimCam>();
        combatCamScript = combatCam.GetComponent<CombatCam>();
        combatCam.Priority = 0;
        aimCam.Priority = 1;
        explorationCam.Priority = 3;
        if (inCombat)
        {
            aimCamScript.SetYaw(combatCam.m_XAxis.Value);
        }
        else
        {
            aimCamScript.SetYaw(explorationCam.m_XAxis.Value);
        }

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

        if(inCombat)
        {
            if (aiming)
            {
                currentCharacter.gameObject.transform.rotation = Quaternion.Euler(currentCharacter.transform.rotation.eulerAngles.x, Camera.main.transform.rotation.eulerAngles.y, currentCharacter.transform.rotation.eulerAngles.z);
                aimCam.Priority = 2;
                combatCam.Priority = 1;
                explorationCam.Priority = 0;
            }
            else
            {
                combatCam.Priority = 2;
                aimCam.Priority = 1;
                explorationCam.Priority = 0;
            }
        }
        else
        {
            if (aiming)
            {
                currentCharacter.gameObject.transform.rotation = Quaternion.Euler(currentCharacter.transform.rotation.eulerAngles.x, Camera.main.transform.rotation.eulerAngles.y, currentCharacter.transform.rotation.eulerAngles.z);
                aimCam.Priority = 2;
                explorationCam.Priority = 1;
                combatCam.Priority = 0;
            }
            else
            {
                explorationCam.Priority = 2;
                aimCam.Priority = 1;
                combatCam.Priority = 0;
            }
        }
    }

    /// <summary>
    /// Handles camera rotation based on player input.
    /// Updates the yaw using mouse/gamepad look input.
    /// </summary>
    /// <param name="context">The input context containing look delta values.</param>
    public void Look(InputAction.CallbackContext context)
    {
        if(aiming)
        {
            aimCamScript.Look(context);
        }
    }

    /// <summary>
    /// Toggles aiming mode and updates the camera/crosshair state.
    /// </summary>
    /// <param name="context">The input action context (started/canceled).</param>
    public void Aim(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            PossessionAbility.instance.SetStartedHoldTime(Time.time);
            if (inCombat)
            {
                aimCamScript.SetYaw(combatCam.m_XAxis.Value);
            }
            else
            {
                aimCamScript.SetYaw(explorationCam.m_XAxis.Value);
            }

            crossHair.gameObject.SetActive(true);
            aiming = true;
        }
        else if (context.canceled)
        {
            PossessionAbility.instance.SetStartedHoldTime(-1);
            crossHair.gameObject.SetActive(false);
            aiming = false;
        }
        UpdateCam();
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

        aimCam.Priority = 0;
        combatCam.Priority = 0;
        explorationCam.Priority = 0;

        currentCharacter = character;
        if (!listener.attenuationObject) listener.attenuationObject = currentCharacter.gameObject;

        aimCam = character.GetAimCam();
        combatCam = character.GetCombatCam();
        explorationCam = character.GetExploreCam();

        try
        {
            aimCamScript = aimCam.GetComponent<AimCam>();
        }
        catch
        {
            Debug.LogWarning("No aim cam component found!");
        }

        try
        {
            combatCamScript = combatCam.GetComponent<CombatCam>();
        }
        catch
        {
            Debug.LogWarning("No combat cam component found!");
        }

        combatCam.Priority = 2;
        aimCam.Priority = 1;

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

    public void PlayerHitBy(GameObject hitBy)
    {
        if (combatCamScript == null || hitBy == null) return;
        StartCoroutine( combatCamScript.PlayerHitBy(hitBy) );
    }
}
