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
    [SerializeField, Tooltip("The time to wait after overriding camera movement to start general prioritizing assistance again")]
    private float timeWaitToPriorityRotate = 1f;
    [SerializeField, Tooltip("The time it takes to rotate the camera when the player is hit")]
    private float hitRotationTime = 0.1f;
    [SerializeField, Tooltip("The time it takes to rotate the camera when the player is attacking")]
    private float attackingRotationTime = 0.5f;
    [SerializeField, Tooltip("The time it takes to rotate the camera in general priority")]
    private float generalPriorityRotationTime = 0.01f;

    [Header("Goblin cam")]
    [SerializeField, Tooltip("The time to rotate to follow attack on secondary")]
    private float goblinSecondaryRotateTime = 0.2f;

    [Header("References")]
    [SerializeField, Tooltip("The free-look Cinemachine camera used for combat view.")]
    private CinemachineFreeLook combatCam;
    [SerializeField, Tooltip("The Cinemachine virtual camera used for exploration view")]
    private CinemachineFreeLook explorationCam;
    [SerializeField, Tooltip("The currently controlled character whose perspective the camera follows.")]
    private Character currentCharacter;

    [Tooltip("The FMOD studio listener attached to the camera for 3D audio spatialization.")]
    private StudioListener listener;
    [Tooltip("Reference to the CombatCam component that manages framing camera logic.")]
    private CombatCam combatCamScript;
    [Tooltip("Flag to prevent camera priority switching during character transitions.")]
    private bool transitioning = false;
    [Tooltip("True if the player is locked in a room")]
    private bool inCombat = false;
    [Tooltip("True if the player is currently using the right stick to control the camera")]
    private bool looking = false;
    [Tooltip("eleth combat cam")]
    private CinemachineFreeLook elethCombatCam;
    [Tooltip("eleth exploration cam")]
    private CinemachineFreeLook elethExplorationCam;

    /// <summary>
    /// Sets whether the player is in combat or not.
    /// </summary>
    /// <param name="val">Whether the player is in combat or not.</param>
    public void SetInCombat(bool val)
    {
        inCombat = val;
        UpdateCam();
    }

    /// <summary>
    /// Gets the weight applied to threat level when prioritizing camera focus.
    /// </summary>
    /// <returns>The integer weight of the threat value.</returns>
    public int GetThreatWeight()
    {
        return threatWeight;
    }

    /// <summary>
    /// Gets the time it takes for the camera to rotate to follow a secondary attack (e.g., goblin secondary attack).
    /// </summary>
    /// <returns>The secondary attack rotation time in seconds.</returns>
    public float GetGoblinSecondaryRotateTime()
    {
        return goblinSecondaryRotateTime;
    }

    /// <summary>
    /// Gets the duration it takes for the camera to rotate when the player is hit.
    /// </summary>
    /// <returns>The hit rotation time in seconds.</returns>
    public float GetHitRotationTime()
    {
        return hitRotationTime;
    }

    /// <summary>
    /// Gets the duration it takes for the camera to rotate when adjusting based on general priority.
    /// </summary>
    /// <returns>The general priority rotation time in seconds.</returns>
    public float GetGeneralPriorityRotationTime()
    {
        return generalPriorityRotationTime;
    }

    /// <summary>
    /// Gets the duration it takes for the camera to rotate when the player is attacking.
    /// </summary>
    /// <returns>The attacking rotation time in seconds.</returns>
    public float GetAttackingRotationTime()
    {
        return attackingRotationTime;
    }

    /// <summary>
    /// Gets the weight applied to distance when prioritizing camera focus.
    /// </summary>
    /// <returns>The integer weight of the distance value.</returns>
    public int GetDistWeight()
    {
        return distWeight;
    }

    /// <summary>
    /// Gets the delay duration before resuming automatic camera priority rotation.
    /// </summary>
    /// <returns>The time in seconds to wait after manual override.</returns>
    public float GetTimeWaitToPriorityRotate()
    {
        return timeWaitToPriorityRotate;
    }

    /// <summary>
    /// Gets the maximum distance at which threats are considered for camera assistance.
    /// </summary>
    /// <returns>The maximum threat distance in world units.</returns>
    public float GetMaxDistance()
    {
        return maxDistance;
    }

    /// <summary>
    /// Returns the CombatCam script currently being used by this controller.
    /// </summary>
    /// <returns>The active CombatCam component.</returns>
    public CombatCam GetCombatCamScript()
    {
        return combatCamScript;
    }

    /// <summary>
    /// Returns whether the player is currently providing manual camera input (right stick or mouse movement).
    /// </summary>
    /// <returns>True if the player is manually rotating the camera; otherwise, false.</returns>
    public bool GetLooking()
    {
        return looking;
    }

    /// <summary>
    /// Initializes references and sets up camera priorities and FMOD listener.
    /// </summary
    private void Awake()
    {
        instance = this;
        elethCombatCam = combatCam;
        elethExplorationCam = explorationCam;
        combatCamScript = combatCam.GetComponent<CombatCam>();
        combatCam.Priority = 0;
        explorationCam.Priority = 3;

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
            combatCam.Priority = 2;
            elethCombatCam.Priority = 2;
            explorationCam.Priority = 0;
            elethExplorationCam.Priority = 0;
        }
        else
        {
            explorationCam.Priority = 2;
            elethExplorationCam.Priority = 2;
            combatCam.Priority = 0;
            elethCombatCam.Priority = 0;
        }
    }

    /// <summary>
    /// Handles camera rotation based on player input.
    /// Updates the yaw using mouse/gamepad look input.
    /// </summary>
    /// <param name="context">The input context containing look delta values.</param>
    public void Look(InputAction.CallbackContext context)
    {
        if(context.started)
        {
            looking = true;
        }
        else if(context.canceled)
        {
            looking = false;
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

        combatCam.Priority = 0;
        explorationCam.Priority = 0;

        currentCharacter = character;
        if (!listener.attenuationObject) listener.attenuationObject = currentCharacter.gameObject;
        combatCam = character.GetCombatCam();
        explorationCam = character.GetExploreCam();


        try
        {
            combatCamScript = combatCam.GetComponent<CombatCam>();
        }
        catch
        {
            Debug.LogWarning("No combat cam component found!");
        }

        if(inCombat)
        {
            combatCam.Priority = 2;
            explorationCam.Priority = 1;
        }
        else
        {
            combatCam.Priority = 1;
            explorationCam.Priority = 2;
        }
    }

    /// <summary>
    /// Waits for the defined transition time before allowing camera switching again.
    /// </summary>
    private IEnumerator WaitTransitionTime()
    {
        yield return new WaitForSeconds(TRANSITION_TIME);
        transitioning = false;
        UpdateCam();
    }

    /// <summary>
    /// Triggers the combat camera to rotate and focus on the enemy that hit the player.
    /// </summary>
    /// <param name="hitBy">The GameObject representing the enemy that hit the player.</param>
    public void PlayerHitBy(GameObject hitBy)
    {
        if (combatCamScript == null || hitBy == null) return;
        StartCoroutine( combatCamScript.PlayerHitBy(hitBy) );
    }

    /// <summary>
    /// Initiates a combat camera rotation to align with the player’s current attack direction.
    /// </summary>
    /// <param name="forwardDir">The forward direction vector of the player’s attack.</param>
    /// <param name="approachTime">The time in seconds for the rotation to complete.</param>
    public void OnAttack(Vector3 forwardDir, float approachTime)
    {
        if (combatCamScript == null ) return;
        StartCoroutine(combatCamScript.OnAttack(forwardDir, approachTime));
    }

    /// <summary>
    /// Stops all ongoing combat camera rotation coroutines and resets its rotation state.
    /// </summary>
    public void StopRotations()
    {
        if (combatCamScript == null) return;
        combatCamScript.StopAllRotates();
    }
}
