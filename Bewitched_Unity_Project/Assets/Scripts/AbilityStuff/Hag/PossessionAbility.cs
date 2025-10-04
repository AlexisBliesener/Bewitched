using FMOD.Studio;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static PlayerController;

/// <summary>
/// Handles the player's possession ability.
/// Allows the Hag character to possess enemies, manages cooldowns, 
/// updates the UI, plays VFX/SFX, and handles character switching.
/// </summary>
public class PossessionAbility : MonoBehaviour
{
    [Header("Singleton & Events")]
    [Tooltip("Singleton instance of PossessionAbility.")]
    public static PossessionAbility instance;
    [Tooltip("Event triggered when the player switches controlled characters.")]
    public static PlayerControlHandler CharacterControlChangeEvent;

    private const string FILE_ENDING = ".json";

    [Header("Possession Settings")]
    [SerializeField, Tooltip("Cooldown (in seconds) the player must wait in Hag state before possessing again.")]
    private float possessionCooldown = 10f;
    [SerializeField, Tooltip("Maximum distance from the camera where possession is possible.")]
    protected float maxPossessionDistance;
    [SerializeField, Tooltip("Rate at which life drains from possessed enemies (percentage)."), Range(0, 100)]
    private float lifeDrainPercentage = 2f;
    [SerializeField, Tooltip("Layer mask used to check valid possession targets.")]
    private LayerMask possessionMask;

    [Header("UI References")]
    [SerializeField, Tooltip("UI element that displays the cooldown for possession.")]
    private CooldownDisplay possessionCooldownDisplay;
    [SerializeField, Tooltip("UI element that displays the currently controlled character's health bar.")]
    private GameObject secondaryHealthBar;
    [SerializeField, Tooltip("Crosshair image that changes color based on possession availability.")]
    private Image crossHair;

    [Header("Character References")]
    // protected for test purposes
    [SerializeField, Tooltip("Reference to the Hag character script.")]
    protected Hag eleth;
    [Tooltip("The current character that is being controlled (Hag or possessed enemy).")]
    protected Character currentCharacter;

    [Header("VFX")]
    [SerializeField, Tooltip("Highlights the enemy currently targeted for possession.")]
    private GameObject targetVFX;
    [SerializeField, Tooltip("Prefab for firing possession visual effect.")]
    private GameObject firingVFX;
    [SerializeField, Tooltip("Prefab for smoke cloud spawned when possession succeeds.")]
    private GameObject smokeCloudVFX;

    [Header("Possession Collider")]
    [SerializeField, Tooltip("Trigger object used to detect possessable enemies.")]
    private GameObject possessionTrigger;
    [SerializeField, Tooltip("Script attached to possession trigger for tracking nearby enemies.")]
    private PossessionCollider possessionColliderScript;

    [Header("Dynamic Possession Range")]
    [SerializeField, Tooltip("Starting angle of possession field of view.")]
    private float startingPossessionAngle;
    [SerializeField, Tooltip("Ending angle of possession field of view after charging.")]
    private float endingPossesionAngle;
    [SerializeField, Tooltip("Starting distance of possession range.")]
    private float startingPossessionDistance;
    [SerializeField, Tooltip("Ending distance of possession range after charging.")]
    private float endingPossesionDistance;
    [SerializeField, Tooltip("Time required to fully focus possession (angle and distance).")]
    private float timeToFocus;

    [Header("Runtime Data")]
    [Tooltip("The angle of possession cone currently being used.")]
    private float currentPossessionAngle;
    [Tooltip("The distance of possession ray currently being used.")]
    private float currentPossesionDistance;
    [Tooltip("The time when possession was last released.")]
    private float timePossessionLastLeft = Mathf.NegativeInfinity;
    [Tooltip("The time when possession of the current enemy started.")]
    private float timePossessing;
    [Tooltip("The current enemy targeted for possession.")]
    private Character currentPossessableEnemy = null;
    [Tooltip("Tracks whether the player is in range of a possessable enemy.")]
    private enum PossessionStates { canPossess, canNotPossess }
    [Tooltip("Current possession state (can or cannot possess).")]
    private PossessionStates possessionState = PossessionStates.canNotPossess;
    [Tooltip("Sound effect instance for possession (if currently playing).")]
    private EventInstance possessionSoundEffect;
    [Tooltip("Time when possession input started being held (-1 if not held).")]
    private float startedHoldTime = -1;
    [Tooltip("The possession collider script")]
    private PossessionCollider possessionCollider;

    #region Saving/Loading
    /// <summary>
    /// Saves current possession settings to a JSON file.
    /// </summary>
    [ContextMenu("Save to JSON")]
    public void SaveToJson()
    {
        string possessionStatsStr = JsonUtility.ToJson(this, true);

        string folderPath = Path.Combine(Application.dataPath, "JSON");
        folderPath = Path.Combine(folderPath, "PossessionAbility");
        SeeFilePath();
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string filePath = Path.Combine(folderPath, "PossessionAbility" + FILE_ENDING);
        File.WriteAllText(filePath, possessionStatsStr);

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    /// <summary>
    /// Logs the path where JSON files are stored.
    /// </summary>
    [ContextMenu("See File Path")]
    public void SeeFilePath()
    {
        string folderPath = Path.Combine(Application.persistentDataPath, "JSON");
        folderPath = Path.Combine(folderPath, "PossessionAbility");
        Debug.Log("Path To JSON File:");
        Debug.Log(folderPath);
    }

    /// <summary>
    /// Loads possession settings from a JSON file.
    /// </summary>
    [ContextMenu("Load From JSON")]
    public void LoadFromJson()
    {
        string folderPath = Path.Combine(Application.dataPath, "JSON");
        folderPath = Path.Combine(folderPath, "PossessionAbility");
        string filePath = Path.Combine(folderPath, "PossessionAbility" + FILE_ENDING);

        string jsonStr = File.ReadAllText(filePath);
        string[] jsons = jsonStr.Split("|");
        JsonUtility.FromJsonOverwrite(jsons[0], this);

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }
    #endregion

    /// <summary>
    /// Records when possession input starts being held.
    /// </summary>
    public void SetStartedHoldTime(float val) => startedHoldTime = val;

    private void Awake()
    {
        instance = this;
        Cursor.lockState = CursorLockMode.Locked;
        currentCharacter = eleth;
        CharacterControlChangeEvent += SwitchCharacter;
        currentPossessionAngle = startingPossessionAngle;
        currentPossesionDistance = startingPossessionDistance;

        possessionCollider = GetComponentInChildren<PossessionCollider>();

        if (possessionCollider != null)
        {
            possessionCollider.SetCurrentCharacter(currentCharacter);
        }
        else
        {
            Debug.LogWarning("The possession collider is not found!");
        }
    }

    private void OnDisable()
    {
        CharacterControlChangeEvent -= SwitchCharacter;
    }

    private void Update()
    {
        UpdateCooldowns();
        UpdateState();
        UpdateCrossHair();
        UpdateTargetVFX();

        if (startedHoldTime != -1)
        {
            currentPossesionDistance = Mathf.Lerp(startingPossessionDistance, endingPossesionDistance, Mathf.Clamp01((Time.time - startedHoldTime) / timeToFocus));
            currentPossessionAngle = Mathf.Lerp(startingPossessionAngle, endingPossesionAngle, Mathf.Clamp01((Time.time - startedHoldTime) / timeToFocus));
        }
        else
        {
            currentPossesionDistance = startingPossessionDistance;
            currentPossessionAngle = startingPossessionAngle;
        }

        if (possessionTrigger != null)
        {
            possessionTrigger.transform.position = currentCharacter.transform.position;
        }

        // Keep Hag aligned with possessed character
        if (currentCharacter != eleth)
        {
            eleth.transform.position = currentCharacter.transform.position;
            eleth.transform.rotation = currentCharacter.transform.rotation;
        }
    }

    /// <summary>
    /// Handles input for starting or possession.
    /// </summary>
    /// <param name="context">The input action callback context.</param>
    public void Possess(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (Time.time - timePossessionLastLeft >= possessionCooldown)
            {
                timePossessionLastLeft = Time.time;
                eleth.AnimatePossess();
                StartCoroutine(FirePossession());
            }
        }
        else
        {
             return;
        }
    }

    /// <summary>
    /// Handles input for ending possession
    /// </summary>
    /// <param name="context"></param>
    public void LeaveEnemy(InputAction.CallbackContext context)
    {
        if(currentCharacter != eleth)
        {
            if (context.started)
            {
                if (!GrandFinale.instance.GetActive())
                {
                    // respawn old Hag
                    currentCharacter.SetControlled(false);
                    CharacterControlChangeEvent?.Invoke(eleth);
                }
                else
                {
                    GrandFinale.instance.Explode(timePossessing, false);
                }

                timePossessionLastLeft = Time.time;
            }
            else
            {
                return;
            }
        }
    }

    /// <summary>
    /// Possesses an enemy if currently avaliable at the time of firing
    /// </summary>
    private IEnumerator FirePossession()
    {
        Character target = possessionState == PossessionStates.canPossess ? currentPossessableEnemy : null;
        if (!AudioManager.TryPlayInstance("Possession", out possessionSoundEffect, true, null))
        {
            Debug.LogError("Failed to play possession sound effect. Is it assigned in the ref sheet?");
        }
        yield return new WaitForSeconds(0.5f);
        if (target)
        {
            currentPossessableEnemy.SetControlled(true);
            CharacterControlChangeEvent?.Invoke(currentPossessableEnemy);

            // Possession smoke VFX
            if(smokeCloudVFX != null)
            {
                Instantiate(smokeCloudVFX, new Vector3(currentPossessableEnemy.transform.position.x,
                    currentPossessableEnemy.transform.position.y + currentPossessableEnemy.GetComponent<CharacterController>().height / 2,
                    currentPossessableEnemy.transform.position.z), currentPossessableEnemy.transform.rotation);
            }
            else
            {
                Debug.LogWarning("Smoke Cloud VFX is not assigned!");
            }

            if (possessionSoundEffect.isValid()) possessionSoundEffect.setParameterByName("Stage", 1);
            else Debug.LogError("Possession Sound Effect is not playing! Can't set param!");
        }
        else
        {
            //Possession miss currently not implemented
            if (possessionSoundEffect.isValid()) possessionSoundEffect.setParameterByName("Stage", 2);
            else Debug.LogError("Possession Sound Effect is not playing! Can't set param!");
        }

        // Possession fire VFX
        if(firingVFX != null)
        {
            Instantiate(firingVFX, eleth.transform.position + new Vector3(eleth.transform.forward.x, 1f, eleth.transform.forward.z), eleth.transform.rotation);
        }
        else
        {
            Debug.LogWarning("Firing Possession VFX is not assigned!");
        }
    }
    
    /// <summary>
    /// Updates the aim target VFX
    /// Places it on the enemy being targeted or disabled it if there is no enemy being targeted
    /// </summary>
    private void UpdateTargetVFX()
    {
        if(targetVFX != null)
        {
            if (possessionState == PossessionStates.canPossess)
            {
                targetVFX.SetActive(true);
                targetVFX.transform.position = currentPossessableEnemy.transform.position ;
            }
            else
            {
                targetVFX.SetActive(false);
            }
        }
        else
        {
            Debug.LogWarning("Target Possession VFX not assigned!");
        }
    }

    /// <summary>
    /// Updates the color of the cross hair baised on if the player can currently possess
    /// </summary>
    private void UpdateCrossHair()
    {
        if (crossHair == null)
        {
            Debug.LogWarning("Crosshair image is not assigned!");
            return;
        }

        if (possessionState == PossessionStates.canNotPossess)
        {
            crossHair.color = Color.white;
        }
        else
        {
            crossHair.color = Color.red;
        }
    }

    /// <summary>
    /// Updates the state of possession ability to tell if the player can currently possess or not
    /// </summary>
    private void UpdateState()
    {
        // Can only possess if the cooldown is over
        if(possessionCooldown - (Time.time - timePossessionLastLeft) > 0)
        {
            possessionState = PossessionStates.canNotPossess;
            return;
        }

        if (possessionColliderScript != null && possessionColliderScript.GetCharactersInPossession().Count != 0)
        {
            PriorityQueue<(float, Character)> distances = new PriorityQueue<(float, Character)>();
            foreach (Character character in possessionColliderScript.GetCharactersInPossession())
            {
                Vector3 playerForward = new Vector3( currentCharacter.transform.forward.x, 0, currentCharacter.transform.forward.z);
                Vector3 toCharacter = new Vector3( character.transform.position.x, 0, character.transform.position.z) - new Vector3(currentCharacter.transform.position.x, 0, currentCharacter.transform.position.z);

                playerForward = playerForward.normalized;
                toCharacter = toCharacter.normalized;

                float dotProduct = Vector3.Dot(playerForward, toCharacter);
                float angle = Mathf.Acos(dotProduct);
                angle = Mathf.Rad2Deg * angle;
                if (angle < currentPossessionAngle / 2.0f)
                {
                    Ray possessionRay = new Ray(currentCharacter.transform.position, toCharacter);

                    Debug.DrawRay(currentCharacter.transform.position, toCharacter);

                    RaycastHit hitInfo;
                    if (Physics.Raycast(possessionRay, out hitInfo, currentPossesionDistance, possessionMask))
                    {
                        Debug.Log("hitinfor " + hitInfo.collider.name);
                        if (hitInfo.collider.gameObject.GetComponent<Character>() != null)
                        {
                            distances.Enqueue((hitInfo.distance, character), Mathf.FloorToInt(hitInfo.distance * 100));
                        }
                    }
                }
            }

            if (distances.Count > 0)
            {
                (float, Character) characterPair = distances.Dequeue();
                currentPossessableEnemy = characterPair.Item2;
                possessionState = PossessionStates.canPossess;
            }
            else
            {
                possessionState = PossessionStates.canNotPossess;
            }
        }
    }

    /// <summary>
    /// Updates the possession cooldown UI display.
    /// </summary>
    private void UpdateCooldowns()
    {
        if (possessionCooldownDisplay == null)
        {
            Debug.LogWarning("Cooldown display is not assigned!");
            return;
        }

        possessionCooldownDisplay.SetAbleToUse(true);

        possessionCooldownDisplay.SetCooldownCover(possessionCooldown - (Time.time - timePossessionLastLeft));
    }

    /// <summary>
    /// Switches control between the Hag and a possessed character, updating health and UI accordingly.
    /// </summary>
    /// <param name="newCharacter">The new character to switch control to.</param>
    public void SwitchCharacter(Character newCharacter)
    {
        currentCharacter.DeactivateSurroundingPoints();
        currentCharacter.GetComponent<HealthController>().EnableUpdateModel(false);

        PlayerController.instance.SeteCharacterController(newCharacter.GetComponent<CharacterController>());
        HealthController hagHealth = eleth.GetComponent<HealthController>();
        HealthController newHealth = newCharacter.GetComponent<HealthController>();
        if (newCharacter == eleth)
        {
            if (hagHealth != null)
            {
                hagHealth.SetDecay(0f); // Hag does not decay
                hagHealth.EnableUpdateModel(true);
            }

            if (secondaryHealthBar != null)
            {
                eleth.EnableEleth();
                secondaryHealthBar.SetActive(false);
            }
            currentCharacter.SetTeamID(2);
            PlayerController.instance.SetAllowMovement(true);
            AudioManager.TryPlayOneShot("LeaveBody");

            // Possession smoke VFX
            if (smokeCloudVFX != null)
            {
                Instantiate(smokeCloudVFX, new Vector3(eleth.transform.position.x,
                    eleth.transform.position.y + eleth.GetComponent<CharacterController>().height / 2,
                    eleth.transform.position.z), eleth.transform.rotation);
            }
            else
            {
                Debug.LogWarning("Smoke Cloud VFX is not assigned!");
            }
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
                newHealth.SetDecay(lifeDrainPercentage);
                newHealth.EnableUpdateModel(true);
                if (secondaryHealthBar != null)
                {
                    secondaryHealthBar.GetComponent<HealthBar>().Subscribe(newHealth);
                    eleth.DisableEleth();
                    secondaryHealthBar.SetActive(true);
                }
            }
            newCharacter.SetTeamID(1);
            timePossessing = Time.time;
        }
        timePossessionLastLeft = Time.time;
        currentCharacter = newCharacter;
        currentCharacter.ActivateSurroundingPoints();
        PlayerController.instance.currentCharacter = newCharacter;

        if(currentCharacter.GetComponent<CharacterController>() != null )
        {
            currentCharacter.GetComponent<CharacterController>().enabled = true;
        }

        PlayerController.instance.SetAllowMovement(true);

        if (possessionCollider != null)
        {
            possessionCollider.SetCurrentCharacter(currentCharacter);
        }
        else
        {
            Debug.LogWarning("The possession collider is not found!");
        }
    }
    /// <summary>
}
