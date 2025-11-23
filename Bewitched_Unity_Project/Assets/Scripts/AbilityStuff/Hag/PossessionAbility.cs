using DG.Tweening;
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
    [SerializeField, Tooltip("Maximum distance from the camera where possession is possible.")]
    protected float maxPossessionDistance;
    [SerializeField, Tooltip("Layer mask used to check valid possession targets.")]
    private LayerMask possessionMask;

    [Header("UI References")]
    [SerializeField, Tooltip("The slider of the possession ability UI")]
    private Slider possessionAbilitySlider;
    [SerializeField, Tooltip("UI element that displays the currently controlled character's health bar.")]
    private GameObject secondaryHealthBar;
    [SerializeField, Tooltip("Crosshair image that changes color based on possession availability.")]
    private Image crossHair;
    [SerializeField, Range(1,2), Tooltip("The scale of the pulse when the slider is at the max value")]
    private float pulseScale = 1.15f;
    [SerializeField, Range(0,1), Tooltip("The time it takes to pulse the slider when it's at the max value")]
    private float pulseTime = 0.25f;

    [Header("Character References")]
    // protected for test purposes
    [SerializeField, Tooltip("Reference to the Hag character script.")]
    protected Hag eleth;
    [Tooltip("The current character that is being controlled (Hag or possessed enemy).")]
    protected Character currentCharacter;
    [SerializeField, Tooltip("The current smoke vfx")]
    private GameObject currentSmokeVFX;

    [Header("VFX")]
    [SerializeField, Tooltip("Highlights the enemy currently targeted for possession.")]
    private GameObject targetVFX;
    [SerializeField, Tooltip("Prefab for firing possession visual effect.")]
    private GameObject firingVFX;
    [SerializeField, Tooltip("Prefab for smoke cloud spawned when possession succeeds.")]
    private GameObject smokeCloudVFX;
    [SerializeField, Tooltip("Prefab for the teleport VFX spawns on counter dodge")]
    private GameObject teleportVFX;

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

    [Header("Ability Charging")]
    [SerializeField, Tooltip("The number of hits the player must do to refill the possession ability")]
    private int hitsToCharge = 4;
    [SerializeField, Tooltip("The time eleth has to wait in witch form to get a 'hit' refilling some charge of the possession ability")]
    private float possessionChargeTime;

    [Header("Dodge Counter")]
    [SerializeField, Tooltip("The distance the player will dodge backwards when using dodge")]
    private float dodgeDistance = 4f;
    [SerializeField, Tooltip("The layer that the enviornment objects are in")]
    private LayerMask environmentLayer;

    [Header("Runtime Data")]
    [Tooltip("The angle of possession cone currently being used.")]
    private float currentPossessionAngle;
    [Tooltip("The distance of possession ray currently being used.")]
    private float currentPossesionDistance;
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
    [Tooltip("The enemy that is avalible to counter")]
    private Enemy counteringEnemy = null;
    [Tooltip("The current value of the possession ability charge")]
    private int possessionCharge;
    [Tooltip("The time eleth has been waiting for another possession ability 'hit' to increase charge")]
    private float possessionChargeTimer;
    [Tooltip("Is currently in the process of possession")]
    private bool isPossessing = false;
    [Tooltip("If the player is allowed to leave the enemy they are possessing")]
    private bool canLeavePossession = true;
    [Tooltip("This enemy overrides all countering possession needs, used for event enemies so they get possessed first")]
    private Enemy possessionOverride = null;

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

    /// <summary>
    /// Adds a point of charge to the possession ability charge
    /// </summary>
    public void AddHitDone()
    {
        possessionCharge++;
        possessionCharge = Mathf.Min(possessionCharge, hitsToCharge);
    }

    /// <summary>
    /// Sets if the player is allowed to leave possession manually
    /// </summary>
    /// <param name="val">True also player to leave false does not </param>
    public void SetCanLeavePossession(bool val)
    {
        canLeavePossession = val;
    }

    /// <summary>
    /// Sets if an enemy is overriding countering.
    /// The overridng enemy will be first priority for possession
    /// </summary>
    /// <param name="enemy">The enemy to be set as the override</param>
    public void SetPossessionOverride(Enemy enemy)
    {
        possessionOverride = enemy;
    }

    private void Awake()
    {
        instance = this;
        Cursor.lockState = CursorLockMode.Locked;
        currentCharacter = eleth;
        CharacterControlChangeEvent += SwitchCharacter;
        currentPossessionAngle = startingPossessionAngle;
        currentPossesionDistance = startingPossessionDistance;
        possessionCharge = hitsToCharge;

        possessionCollider = GetComponentInChildren<PossessionCollider>();

        if (possessionCollider != null)
        {
            possessionCollider.SetCurrentCharacter(currentCharacter);
        }
        else
        {
            Debug.LogWarning("The possession collider is not found!");
        }
        if (possessionAbilitySlider != null)
        {
            possessionAbilitySlider.wholeNumbers = false;
            possessionAbilitySlider.maxValue = hitsToCharge;
            possessionAbilitySlider.value = possessionAbilitySlider.maxValue;
            possessionAbilitySlider.onValueChanged.AddListener(OnPossessionSliderChange);
        }

        // Set the collider radius to max of starting and ending
        SetPossessionDistance(startingPossessionDistance, endingPossesionDistance);
    }

    private void OnDisable()
    {
        CharacterControlChangeEvent -= SwitchCharacter;
        if (possessionAbilitySlider != null)
        {
            possessionAbilitySlider.onValueChanged.RemoveListener(OnPossessionSliderChange);
        }
    }
    /// <summary>
    /// Handles the slider value change event, it will make it like it pulse once when it's get to the max value
    /// </summary>
    private void OnPossessionSliderChange(float value)
    {
        if (value < possessionAbilitySlider.maxValue - 0.001f) return;

        // one quick pulse 
        possessionAbilitySlider.gameObject.transform.DOKill();
        possessionAbilitySlider.gameObject.transform.DOScale(pulseScale, pulseTime).SetEase(Ease.OutQuad).SetLoops(2, LoopType.Yoyo);
    }

    private void Update()
    {
        UpdateUI();
        UpdateState();
        UpdateCrossHair();
        UpdateTargetVFX();

        if(currentCharacter != eleth)
        {
            if (currentSmokeVFX != null)
            {
                currentSmokeVFX.SetActive(true);
            }
            else
            {
                Debug.LogWarning("Smoke VFX is not assigned!");
            }
        }
        else
        {
            if (currentSmokeVFX != null)
            {
                currentSmokeVFX.SetActive(false);
            }
            else
            {
                Debug.LogWarning("Smoke VFX is not assigned!");
            }
        }

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
        if (context.started && currentCharacter.attackState == Character.AttackState.Neutral)
        {
            counteringEnemy = PlayerController.instance.GetCounterAvailable();
            if (possessionCharge == hitsToCharge)
            {
                eleth.AnimatePossess();
                StartCoroutine(FirePossession());
            }
            else if (counteringEnemy != null)
            {
                StartCoroutine(Dodge(counteringEnemy.gameObject));
            }
        }
        else
        {
             return;
        }
    }

    private IEnumerator Dodge(GameObject counteringEnemy)
    {
        if(currentCharacter != eleth)
        {
            StartCoroutine(RespawnEleth());
        }

        PlayerController.instance.SetAllowMovement(false);
        currentCharacter.health.SetInvincible(true);
        foreach(GameObject go in currentCharacter.GetModel())
        {
            go.SetActive(false);
        }
    
        GameObject vfx1 =  Instantiate(teleportVFX, currentCharacter.transform.position, Quaternion.identity);
        yield return new WaitForSeconds(0.1f);
        RaycastHit hitInfo;
        Vector3 moveDist;
        if(Physics.Raycast(currentCharacter.transform.position, counteringEnemy.transform.forward, out hitInfo, dodgeDistance, environmentLayer))
        {
             moveDist = (counteringEnemy.transform.forward.normalized * hitInfo.distance);
        }
        else
        {
            moveDist = (counteringEnemy.transform.forward.normalized * dodgeDistance);
        }

        for(int i = 0; i < 8; i++)
        {
            currentCharacter.GetComponent<CharacterController>().Move(moveDist / 8f);
            yield return null;
        }

        GameObject vfx2 = Instantiate(teleportVFX, currentCharacter.transform.position, Quaternion.identity);
        foreach (GameObject go in currentCharacter.GetModel())
        {
            go.SetActive(true);
        }
        PlayerController.instance.SetAllowMovement(true);
        currentCharacter.health.SetInvincible(false);
        yield return new WaitForSeconds(0.3f);
        Destroy(vfx1);
        
        yield return new WaitForSeconds(0.3f);
        Destroy(vfx2);
        
    }

    /// <summary>
    /// Handles input for ending possession
    /// </summary>
    /// <param name="context"></param>
    public void LeaveEnemy(InputAction.CallbackContext context)
    {
        if (!canLeavePossession) return;

        if(currentCharacter != eleth)
        {
            if (context.started)
            {
               StartCoroutine( RespawnEleth() );
            }
            else
            {
                return;
            }
        }
    }

    private IEnumerator RespawnEleth()
    {
        // gives eleth 0.1f sec of invinciblity so she doesnt get hit by the same attack that killed the enemy she was possessing
        eleth.health.SetInvincible(true);
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
        yield return new WaitForSeconds(0.1f);
        eleth.health.SetInvincible(false);
    }

    /// <summary>
    /// Possesses an enemy if currently avaliable at the time of firing
    /// </summary>
    private IEnumerator FirePossession()
    {
        isPossessing = true;
        // The speed multipler of the possession animation as set in eleths animator controller
        float possessionSpeedMult = eleth.GetComponent<ElethAnimator>().GetPossessionSpeedMult();
        // reset the possession ability charge
        possessionCharge = 0;

        // Gets either the target being aimed at, the countering enemy, or null if neither exist
        Character target = possessionState == PossessionStates.canPossess ? currentPossessableEnemy : null;
        if (counteringEnemy != null && (possessionOverride == null || target != possessionOverride))
        {
            if(counteringEnemy.canPossess)
            {
                eleth.health.SetInvincible(true);
                TimeController.instance.StartTimeSlow(0.5f, 0.9f / possessionSpeedMult);
                target = counteringEnemy;
                yield return new WaitForSeconds(0.2f / possessionSpeedMult);
            }
        }

        if (target != null && !target.canPossess)
        {
            target = null;
        }

        if (!AudioManager.TryPlayInstance("Possession", out possessionSoundEffect, true, null))
        {
            Debug.LogError("Failed to play possession sound effect. Is it assigned in the ref sheet?");
        }
        yield return new WaitForSeconds(0.5f / possessionSpeedMult);

        // Possess target if there is one
        if (target)
        {
            target.SetControlled(true);
            CharacterControlChangeEvent?.Invoke(target);

            // Possession smoke VFX
            if(smokeCloudVFX != null)
            {
                Instantiate(smokeCloudVFX, new Vector3(target.transform.position.x,
                    target.transform.position.y + target.GetComponent<CharacterController>().height / 2,
                    target.transform.position.z), target.transform.rotation);
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

        // Reset time scale if this was a counter
        if (counteringEnemy != null)
        {
            eleth.health.SetInvincible(false);
            yield return new WaitForSeconds(0.2f / possessionSpeedMult);
        }
        isPossessing = false;
    }
    
    /// <summary>
    /// Updates the aim target VFX
    /// Places it on the enemy being targeted or disabled it if there is no enemy being targeted
    /// </summary>
    private void UpdateTargetVFX()
    {
        if(targetVFX != null)
        {
            if (possessionOverride != null && possessionOverride == currentPossessableEnemy && currentCharacter != possessionOverride)
            {
                targetVFX.SetActive(true);
                targetVFX.transform.position = possessionOverride.transform.position;
            }else if(PlayerController.instance.GetCounterAvailable() && PlayerController.instance.GetCounterAvailable() != PlayerController.instance.currentCharacter)
            {
                targetVFX.SetActive(true);
                targetVFX.transform.position = PlayerController.instance.GetCounterAvailable().transform.position;
            }
            else if (PlayerController.instance.GetCounterAvailable() == PlayerController.instance.currentCharacter)
            {
                targetVFX.SetActive(false);
            }
            else
            {
                if (possessionState == PossessionStates.canPossess && currentPossessableEnemy != null)
                {
                    targetVFX.SetActive(true);
                    targetVFX.transform.position = currentPossessableEnemy.transform.position;
                }
                else
                {
                    targetVFX.SetActive(false);
                }
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
        // Can only possess if the ability is charged
        if (possessionCharge != hitsToCharge)
        {
            possessionState = PossessionStates.canNotPossess;
            return;
        }

        if(PlayerController.instance == null)
        {
            Debug.LogWarning("PlayerController instance is not set!");
            return;
        }
        else if(Camera.main == null)
        {
            Debug.LogWarning("Main camera does not exist!");
            return;
        }

        Vector3 dir = new Vector3(PlayerController.instance.GetMovementInput().x, 0, PlayerController.instance.GetMovementInput().y);
        dir = Camera.main.transform.TransformDirection(dir);

        Debug.DrawRay(currentCharacter.gameObject.transform.position, dir, Color.red);

        if (PlayerController.instance.GetMovementInput().magnitude < 0.001f)
            dir = new Vector3(Camera.main.transform.forward.x, 0, Camera.main.transform.forward.z);

        dir.y = 0;
        dir = dir.normalized;

        if (possessionColliderScript != null && possessionColliderScript.GetCharactersInPossession().Count != 0)
        {
            PriorityQueue<(float, Character)> distances = new PriorityQueue<(float, Character)>();
            foreach (Character character in possessionColliderScript.GetCharactersInPossession())
            {
                if (character.canPossess == false) continue;
                Vector3 toCharacter = new Vector3( character.transform.position.x, 0, character.transform.position.z) - new Vector3(currentCharacter.transform.position.x, 0, currentCharacter.transform.position.z);

                toCharacter = toCharacter.normalized;

                float dotProduct = Vector3.Dot(toCharacter, dir);
                float angle = Mathf.Acos(dotProduct);
                angle = Mathf.Rad2Deg * angle;
                if (angle < currentPossessionAngle / 2.0f)
                {
                    Ray possessionRay = new Ray(currentCharacter.transform.position, toCharacter);

                    Debug.DrawRay(currentCharacter.transform.position, toCharacter);

                    RaycastHit hitInfo;
                    if (Physics.Raycast(possessionRay, out hitInfo, currentPossesionDistance, possessionMask))
                    {
                        if (hitInfo.collider.gameObject.GetComponent<Character>() != null)
                        {
                            distances.Enqueue((hitInfo.distance + (1 - dotProduct) * 50, character), Mathf.FloorToInt(hitInfo.distance * 100));
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
        else
        {
            possessionState = PossessionStates.canNotPossess;
        }
    }

    /// <summary>
    /// Updates the possession UI display.
    /// </summary>
    private void UpdateUI()
    {
        if (possessionAbilitySlider != null)
        {
            float progresss = possessionCharge;
            if (possessionCharge < hitsToCharge && possessionChargeTime > 0f && !isPossessing)
            {
                progresss += Mathf.Clamp01((Time.time - possessionChargeTimer) / possessionChargeTime); 
            }
            possessionAbilitySlider.value = Mathf.Clamp(progresss, 0f, hitsToCharge);
        }
        else
        {
            Debug.LogWarning("Possession Ability Slider is not set!");
        }

        if (currentCharacter == eleth)
        {
            if (possessionCharge == hitsToCharge)
            {
                possessionChargeTimer = Time.time;
            }
            else if (Time.time - possessionChargeTimer > possessionChargeTime)
            {
                AddHitDone();
                possessionChargeTimer = Time.time;
            }
        }
        else
        {
            possessionChargeTimer = Time.time;
        }
    }

    /// <summary>
    /// Switches control between the Hag and a possessed character, updating health and UI accordingly.
    /// </summary>
    /// <param name="newCharacter">The new character to switch control to.</param>
    public void SwitchCharacter(Character newCharacter)
    {
        if(currentCharacter != eleth)
        {
            currentCharacter.SetControlled(false);
        }
        
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
        currentCharacter = newCharacter;
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
    /// Returns the amount of hits needed to charge the possession ability
    /// Used by the Backup Plan upgrade
    /// </summary>
    /// <returns>hitsToCharge, the possession ability</returns>
    public int GetHitsToCharge()
    {
        return hitsToCharge;
    }

    /// <summary>
    /// Sets the amount of hits it takes to fully charge the possession ability
    /// Used by the Backup Plan upgrade
    /// </summary>
    /// <param name="val">The amount of hits to set hitsToCharge to</param>
    public void SetHitsToCharge(int val)
    {
        hitsToCharge = val;
    }
    
    /// Gets the base focus time for possession
    /// </summary>
    /// <returns>The base focus time</returns>
    public float GetFocusTime()
    {
        return timeToFocus;
    }
    
    /// <summary>
    /// Sets the time to focus possession
    /// </summary>
    /// <param name="newTime">The new time to focus possession</param>
    public void SetFocusTime(float newTime)
    {
        timeToFocus = Mathf.Max(0.1f, newTime); // never 0 or negative
    }

    public float GetStartingPossessionDistance()
    {
        return startingPossessionDistance;
    }

    public float GetEndingPossessionDistance()
    {
        return endingPossesionDistance;
    }

    public void SetPossessionDistance(float starting, float ending)
    {
        startingPossessionDistance = starting;
        endingPossesionDistance = ending;
        if (possessionCollider != null)
        {
            possessionCollider.SetColliderRadius(Mathf.Max(starting, ending));
        }
    }
}
