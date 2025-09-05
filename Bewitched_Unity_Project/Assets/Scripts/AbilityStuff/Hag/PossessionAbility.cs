using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static PlayerController;


/// <summary>
/// Handles the player’s possession ability, allowing the Hag character to
/// possess enemies, manage cooldowns, update the UI, and switch between
/// controlled characters.
/// </summary>
public class PossessionAbility : MonoBehaviour
{
    public static PlayerControlHandler CharacterControlChangeEvent;

    [SerializeField, Tooltip("The game virutal camera")]
    private CinemachineVirtualCamera virtualCam;
    [SerializeField, Tooltip("The cooldown in seconds that the player must wait in witch state before being able to possess again")]
    float possessionCooldown = 10;
    [SerializeField, Tooltip("The max distance away from the camera that the player can possess")]
    private float maxPossessionDistance;
    [SerializeField, Tooltip("Possession Orb Cooldown UI")]
    private CooldownDisplay possessionCooldownDisplay;
    [SerializeField, Tooltip("Time to fill up enemy explosion")]
    private float enemyExplosionTime = 10;
    [SerializeField, Tooltip("Rate at which life is drained")]
    private float lifeDrainCoefficient = 2;
    [SerializeField, Tooltip("The currently controlled character's health bar")]
    private GameObject secondaryHealthBar;
    [SerializeField, Tooltip("Hag script on the witch gameobject")]
    private Hag oldHag;

    [Tooltip("The time possession was left")]
    private float timePossessionLastLeft = Mathf.NegativeInfinity;
    [Tooltip("If the possession button is currently being held")]
    private bool possessHeld = false;
    [Tooltip("The time when possession of the current enemy started")]
    private float timePossessing;
    [Tooltip("The current character that is possessed")]
    private Character currentCharacter;

    /// <summary>
    /// Initializes possession state and subscribes to character switching events.
    /// </summary>
    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        currentCharacter = oldHag;
        CharacterControlChangeEvent += SwitchCharacter;
    }

    /// <summary>
    /// Unsubscribes from character switching events when disabled.
    /// </summary>
    void OnDisable()
    {
        CharacterControlChangeEvent -= SwitchCharacter;
    }

    /// <summary>
    /// Updates possession abilities, UI, and keeps the Hag's position aligned when possessing an enemy.
    /// </summary>
    private void Update()
    {
        HandleHeldAbilites();
        UpdateUI();

        if (currentCharacter != oldHag)
        {
            oldHag.transform.position = currentCharacter.transform.position;
            oldHag.transform.rotation = currentCharacter.transform.rotation;
        }
    }

    /// <summary>
    /// Handles input for starting or ending possession.
    /// </summary>
    /// <param name="context">The input action callback context.</param>
    public void Possess(InputAction.CallbackContext context)
    {
        if (currentCharacter == oldHag)
        {
            if (context.started)
            {
                if (Time.time - timePossessionLastLeft >= possessionCooldown)
                {
                    timePossessionLastLeft = Time.time;
                    currentCharacter.AnimatePossess();
                    FirePossession();
                }
            }
            else
            {
                return;
            }
        }
        else
        {
            if (context.started)
            {
                timePossessionLastLeft = Time.time;
                StartCoroutine(ExplodeEnemy());
            }
            else
            {
                return;
            }
        }
    }

    /// <summary>
    /// Fires a raycast to attempt possession of an enemy in front of the camera.
    /// </summary>
    private void FirePossession()
    {
        Ray possessionRay = new Ray(virtualCam.transform.position, virtualCam.transform.forward);
        RaycastHit hitInfo;
        if (Physics.Raycast(possessionRay, out hitInfo, maxPossessionDistance))
        {
            if (hitInfo.collider.gameObject.CompareTag("Enemy"))
            {
                Character characterHit = hitInfo.collider.gameObject.GetComponent<Character>();
                CharacterControlChangeEvent?.Invoke(characterHit);
                characterHit.SetControlled(true);
            }
        }
    }

    /// <summary>
    /// Handles possession ability when the button is held.
    /// </summary>
    private void HandleHeldAbilites()
    {
        if (Time.time - timePossessionLastLeft >= possessionCooldown && possessHeld)
        {
            if (currentCharacter == oldHag)
            {
                StartCoroutine(ExplodeEnemy());
            }
            else
            {
                timePossessionLastLeft = Time.time;
                currentCharacter.AnimatePossess();
                FirePossession();
            }
        }
    }

    /// <summary>
    /// Updates the possession cooldown UI display.
    /// </summary>
    private void UpdateUI()
    {
        if (currentCharacter == oldHag)
        {
            possessionCooldownDisplay.SetAbleToUse(true);
        }
        else
        {
            possessionCooldownDisplay.SetAbleToUse(false);
        }

        possessionCooldownDisplay.SetCooldownCover(possessionCooldown - (Time.time - timePossessionLastLeft));
    }

    /// <summary>
    /// Coroutine that handles enemy explosion and switches back to the Hag when possession ends.
    /// </summary>
    private IEnumerator ExplodeEnemy()
    {
        yield return null; // wait one frame

        if (currentCharacter != oldHag)
        {
            if (Time.time - timePossessing > enemyExplosionTime)
            {
                currentCharacter.Explode();
                currentCharacter.Die();
                // Apply shunt damage
            }

            // respawn old Hag
            currentCharacter.SetControlled(false);
            CharacterControlChangeEvent?.Invoke(oldHag);
        }
    }

    /// <summary>
    /// Switches control between the Hag and a possessed character, updating health and UI accordingly.
    /// </summary>
    /// <param name="newCharacter">The new character to switch control to.</param>
    public void SwitchCharacter(Character newCharacter)
    {
        PlayerController.instance.SeteCharacterController(newCharacter.GetComponent<CharacterController>());
        HealthController hagHealth = oldHag.GetComponent<HealthController>();
        HealthController newHealth = newCharacter.GetComponent<HealthController>();
        if (newCharacter == oldHag)
        {
            // This means we are switching back to the hag
            if (hagHealth != null)
            {
                hagHealth.SetDecay(0f); // Hag does not decay
                hagHealth.EnableUpdateModel(true);
            }
            if (secondaryHealthBar != null)
            {
                oldHag.gameObject.SetActive(true);
                secondaryHealthBar.SetActive(false);
            }
            currentCharacter.SetTeamID(2);
            PlayerController.instance.SetAllowMovement(true);
        }
        else
        {
            // Possess an enemy
            if (hagHealth != null)
            {
                hagHealth.SetDecay(0f);
            }
            if (newHealth != null)
            {
                newHealth.SetDecay(lifeDrainCoefficient);
                newHealth.EnableUpdateModel(true);
                if (secondaryHealthBar != null)
                {
                    secondaryHealthBar.GetComponent<HealthBar>().Subscribe(newHealth);
                    oldHag.gameObject.SetActive(false);
                    secondaryHealthBar.SetActive(true);
                }
            }
            newCharacter.SetTeamID(1);
            timePossessing = Time.time;
        }
        currentCharacter = newCharacter;
        PlayerController.instance.currentCharacter = newCharacter;
    }
}
