using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Cinemachine;
using DG.Tweening;
using FMOD;
using Debug = UnityEngine.Debug;
using NaughtyAttributes;

[RequireComponent(typeof(HealthController))]
[RequireComponent(typeof(CharacterAnimator))]
public abstract class Character : MonoBehaviour
{
    // Abstract class for characters in our game
    const string FILE_ENDING = ".json";
    [SerializeField, Tooltip("Are you a dev? [Don't check this if you're not a dev!!]")]
    protected private bool dev = false;
    [Header("Character Settings")]
    [Tooltip("Character Name")]
    public string characterName;
    [SerializeField, Tooltip("The model of this character in art peices (keep animator on when turned off)")]
    private GameObject[] modelPieces;

    [Header("Movement Settings")]
    [Tooltip("Speed the Character Can Move While Chasing"), Range(0, 20)]
    public float movementSpeed = 5;
    [Tooltip("Speed the character can move while approaching for an attack"), Range(0, 50)]
    public float approachSpeed = 7;
    [Tooltip("Acceleration of the Character"), Range(0, 50)]
    public float acceleration = 5;
    [Tooltip("Deceleration of the Character"), Range(0, 50)]
    public float deceleration = 5;
    [Tooltip("Rotational velocity of the character"), Range(0, 360)]
    public float rotationalVelocity = 240;
    [Tooltip("Time chasing a character for an attack"), Range(0, 10)]
    public float chaseTime = 3;
    [Tooltip("Time for the character to wind up"), Range(0, 10)]
    public float windupTime = 0.25f;

    [Tooltip("Weight of the character"), Range(0, 50)]
    public float weight = 10;
    [Tooltip("Push force modifer"), Range(0, 0.5f)]
    public float pushForceModifer = 0.1f;

    [Header("Surrounding Settings")]
    [Tooltip("Character Hitbox Radius")]
    public float sizeRadius = 1.5f;
    [Tooltip("Number of Points to Surround")]
    public int numSurroundingPoints = 8;
    [Tooltip("Minimum Radius of Surrounding Points (For AI Navigation)")]
    public float minSurroundingRadius = 2;
    [Tooltip("Maximum Radius of Surrounding Points (For AI Navigation)")]
    public float maxSurroundingRadius = 5;
    [Tooltip("Team of the character")]
    public int teamID;
    [Tooltip("The priority of this character")]
    public int priority = 1;
    [SerializeField, Tooltip("The shoulder offset the camera has from the character")]
    private Vector3 shoulderOffset = new Vector3(1f, 2.5f, 0f);

    [Header("Attack Settings")]
    [Tooltip("Attack Delay"), Range(0, 10)]
    public float attackDelay = 1;
    [Tooltip("Cooldown After Primary Ability"), Range(0, 10)]
    public float primaryCooldown = 5;
    [SerializeField, Tooltip("The amount of health this character will use when using their primary attack"), Range(0, 100)]
    protected int primaryAttackCost;
    [SerializeField, Tooltip("The AI cost of the primary attack"), Range(0, 10)]
    protected int primaryAICost;
    [Tooltip("Cooldown After Secondary Ability"), Range(0, 10)]
    public float secondaryCooldown = 5;
    [SerializeField, Tooltip("The amount of health this character will use when using their secondary attack"), Range(0, 100)]
    protected int secondaryAttackCost;
    [SerializeField, Tooltip("The AI cost of the secondary attack"), Range(0, 10)]
    protected int secondaryAICost;
    [Tooltip("Primary Attack Range"), Range(0, 10)]
    public float primaryAttackRange;
    [Tooltip("The reference to the health controller"), HideInInspector]
    public HealthController health;
    [Header("Possession Settings")]
    [Tooltip("Can the player possess the character?")]
    public bool canPossess = true;

    [Tooltip("Hit Stun Duration")]
    protected float hitStunDuration = 0.12f;

    protected float timeLastPrimary = -Mathf.Infinity;
    protected float timeLastSecondary = -Mathf.Infinity;

    protected float timeLastAny;
    protected GameObject hitStunActual = null;

    protected bool attackingPrimary = false;
    protected bool attackingSecondary = false;

    protected float baseMovementSpeed;
    protected float basePrimaryCooldown;
    protected float baseSecondaryCooldown;

    protected bool releasePrimaryImm = false;
    protected bool releaseSecondaryImm = false;

    protected Vector3 velocity = Vector3.zero;
    protected Vector3 velocityToMove = Vector3.zero;

    [Tooltip("The step that the character is in there primary combo, -1 to indicate the character is currently not attacking with primary")]
    protected int currentPrimaryComboStep = -1;
    [Tooltip("The amount of combo steps that this character has on their primary attack")]
    protected int primaryComboSteps;
    [Tooltip("The script that controls chaning animation states")]
    protected CharacterAnimator characterAnimator;

    [Header("Primary Combo Stats")]
    [Tooltip("Primary Cooldown Reset Time")]
    public float[] primaryComboResetTime;
    [Tooltip("Primary combo min time to wait to hit the next combo")]
    public float[] primaryComboMinTime;

    [Tooltip("Character to lock onto")]
    protected Character lockedCharacter = null;

    [Tooltip("The coroutine handling attacking actions")]
    protected Coroutine attackStateCoroutine = null;

    [Tooltip("Bool determining if an attack has hit a character")]
    protected bool hitCharacter = false;

    protected bool dodgable = false;
    protected bool attackDodged = false;
    protected bool dodging = false;

    protected bool inCounter = false;

    protected Character attackingEnemy = null;

    [Tooltip("Counter indicator")]
    protected GameObject counterIndicatorVFX;

    [Tooltip("Target tween position")]
    protected Vector3 targetTweenPosition;

    protected float timeLastDodge = 0;

    [Tooltip("List of nodes that are costly for the area this character is taking up")]
    List<List<int>> costlyNodes = new List<List<int>>();

    [Tooltip("Position the character was last time the nodes were reset")]
    protected Vector3 previousCostlyPosition;

    [Tooltip("Threshold distance before resetting costly area")]
    protected float invalidAreaResetThreshold = 0.5f;

    protected bool stunned = false;
    [Header("Debug/Dev Options"), ShowIf("dev")]
    [Tooltip("Layer mask for the characters")]
    public LayerMask characters;
    [Tooltip("Mask for the ground layer"), ShowIf("dev")]
    public LayerMask ground;
    [Tooltip("Mask for the environment layer"), ShowIf("dev")]
    public LayerMask environment;
    [SerializeField, Tooltip("The Cinemachine FreeLook camera used for zoomed out in combat movement."), ShowIf("dev")]
    private CinemachineFreeLook combatCam;
    [SerializeField, Tooltip("The Cinemachine Virtual Camera used for aiming and close-up view."), ShowIf("dev")]
    private CinemachineVirtualCamera aimCam;
    [SerializeField, Tooltip("The Cinemachine explore Camera used for regular out of combat view."), ShowIf("dev")]
    private CinemachineFreeLook exploreCam;
    [Tooltip("The list of attack status effects"), ShowIf("dev")]
    public List<AttackStatusEffects> attackEffects = new List<AttackStatusEffects>(); // This list is for simple saving
    [Tooltip("The list of effects JSONs"), ShowIf("dev")]
    [SerializeField] private List<string> effectJSONs = new List<string>();
    [Header("References/Prefabs"), ShowIf("dev")]
    [Tooltip("Primary Fire Image")]
    public Sprite primaryFireIcon;
    [Tooltip("Secondary Fire Image"), ShowIf("dev")]
    public Sprite secondaryFireIcon;
    [Tooltip("Hit Stun Prefab"), ShowIf("dev")]
    public GameObject hitStunPrefab;
    [Tooltip("Attack indicator prefab"), ShowIf("dev")]
    public GameObject counterIndicatorVFXPrefab;
    [Tooltip("Character Controller component")]
    private CharacterController characterController;
    /// <summary>
    /// The different attacking states a character can have
    /// </summary>
    public enum AttackState
    {
        Approaching, // The run up before the attack begins to close distance
        Windup, // The windup stage of the attack - basic animation
        Attacking, // The attack itself
        Neutral, // Neutral state for enemies to be selected to begin
        Dodging  // Dodging an attack
    }

    [Tooltip("The attack state")]
    public AttackState attackState;

    #region Saving/Loading

    [ContextMenu("Save to JSON")]
    public void SaveToJson()
    {
        effectJSONs = new List<string>();
        string characterStatsStr = JsonUtility.ToJson(this, true);

        foreach (AttackStatusEffects effect in attackEffects)
        {
            string statusStr = effect.SaveToJson();
            characterStatsStr += "|";
            characterStatsStr += statusStr;
            effectJSONs.Add(statusStr);
        }

        string folderPath = Path.Combine(Application.dataPath, "JSON");
        folderPath = Path.Combine(folderPath, "CharacterStats");
        SeeFilePath();
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string filePath = Path.Combine(folderPath, characterName + FILE_ENDING);
        File.WriteAllText(filePath, characterStatsStr);


#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif


    }

    [ContextMenu("See File Path")]
    public void SeeFilePath()
    {
        string folderPath = Path.Combine(Application.persistentDataPath, "JSON");
        folderPath = Path.Combine(folderPath, "CharacterStats");
        Debug.Log("Path To JSON File:");
        Debug.Log(folderPath);
    }

    [ContextMenu("Load From JSON")]
    public void LoadFromJson()
    {

        string folderPath = Path.Combine(Application.dataPath, "JSON");
        folderPath = Path.Combine(folderPath, "CharacterStats");
        string filePath = Path.Combine(folderPath, characterName + FILE_ENDING);

        string jsonStr = File.ReadAllText(filePath);

        string[] jsons = jsonStr.Split("|");

        JsonUtility.FromJsonOverwrite(jsons[0], this);
        for (int i = 1; i < jsons.Length; i++)
        {
            attackEffects[i - 1].LoadFromJson(jsons[i]);
        }

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif

    }

    #endregion

    protected virtual void Awake()
    {
        characterAnimator = GetComponent<CharacterAnimator>();
        if (health == null)
        {
            health = GetComponent<HealthController>();
        }
        health.OnDamaged += OnDamaged;
        health.OnHealthChanged += OnHealthChanged;
        health.OnDeath += OnDeath;

        SetBaseStats();
        attackState = AttackState.Neutral;
    }
    protected virtual void OnDestroy()
    {
        health.OnDamaged -= OnDamaged;
        health.OnHealthChanged -= OnHealthChanged;
        health.OnDeath -= OnDeath;
    }

    public GameObject[] GetModel()
    {
        return modelPieces;
    }

    /// <summary>
    /// Returns the stunned status
    /// </summary>
    public bool IsStunned => stunned;

    /// <summary>
    /// Returns the Cinemachine Combat camera associated with this character.
    /// </summary>
    /// <returns>The FreeLook Cinemachine camera.</returns>
    public CinemachineFreeLook GetCombatCam()
    {
        return combatCam;
    }

    /// <summary>
    /// Returns the Cinemachine Aim Camera associated with this character.
    /// </summary>
    /// <returns>The Virtual Cinemachine camera.</returns>
    public CinemachineVirtualCamera GetAimCam()
    {
        return aimCam;
    }

    /// <summary>
    /// Returns the Cinemachine Explore Camera associated with this character.
    /// </summary>
    /// <returns>The Virtual Cinemachine camera.</returns>
    public CinemachineFreeLook GetExploreCam()
    {
        return exploreCam;
    }

    /// <summary>
    /// Gets the current step this character is in in their primary attack combo
    /// -1 represents not in combo
    /// </summary>
    /// <returns>Step in combo</returns>
    public int GetCurrentPrimaryComboStep()
    {
        return currentPrimaryComboStep;
    }

    /// <summary>
    /// Returns the time that this character last used their primary attack
    /// </summary>
    /// <returns>Last time primary attack was used</returns>
    public float GetTimeLastPrimary()
    {
        return timeLastPrimary;
    }

    /// <summary>
    /// Returns an array of floats representing the time the combo will wait till reseting
    /// Each value in the array corresponds to a step in the combo attack
    /// </summary>
    /// <returns>This characters time till reset on each primary combo step</returns>
    public float[] GetPrimaryComboResetTime()
    {
        return primaryComboResetTime;
    }

    /// <summary>
    /// OnDamaged is called when the character is damaged.
    /// </summary>
    protected virtual void OnDamaged(float amount)
    {

    }

    /// <summary>
    /// OnHealthChanged is called when the character's health changes.
    /// </summary>
    protected virtual void OnHealthChanged(float current, float max) { }
    /// <summary>
    /// OnDeath is called when the character dies.
    /// </summary>
    protected virtual void OnDeath()
    {
        StopAllCoroutines();
        Die();
        // Stop all coroutines destroy all objects too
        if (hitStunActual != null) Destroy(hitStunActual);
        if (counterIndicatorVFX != null) Destroy(counterIndicatorVFX);
    }

    /// <summary>
    /// Called when the character dies to switch animation state to death
    /// </summary>
    public void AnimateDeath()
    {
        characterAnimator.SwitchState("Death");
    }

    public abstract void Die();

    protected virtual bool CheckPrimaryCooldown() {
        float cooldown = primaryCooldown;
        return Time.time - timeLastPrimary >= cooldown && Time.time - timeLastAny >= attackDelay;
    }

    protected bool CheckSecondaryCooldown() {
        return Time.time - timeLastSecondary >= secondaryCooldown && Time.time - timeLastAny >= attackDelay;
    }

    public virtual bool CheckPrimaryUsable()
    {
        if (!CheckPrimaryCooldown() || stunned || attackingSecondary) return false;

        return true;
    }

    public virtual bool CheckSecondaryUsable()
    {
        if (!CheckSecondaryCooldown()) return false;
        if (attackingPrimary || attackingSecondary || stunned) return false;

        return true;
    }

    /// <summary>
    /// Returns the shoulder offset vector for the character.
    /// This is used by the camera to determine its relative positioning when following the character.
    /// </summary>
    /// <returns>The shoulder offset as a Vector3.</returns>
    public Vector3 GetShoulderOffset()
    {
        return shoulderOffset;
    }

    public virtual void SetControlled(bool v) { }

    public void SetTeamID(int id)
    {
        teamID = id;
    }

    public IEnumerator EnableMovement() // Call this after any movement abilities
    {
        yield return new WaitForSeconds(0.1f);
        if (PlayerController.instance != null)
            PlayerController.instance.SetAllowMovement(true);
    }

    public IEnumerator StartTime(float stopTime)
    {
        yield return new WaitForSecondsRealtime(stopTime);

        if (!GameObject.FindGameObjectWithTag("PauseMenu")) // If not paused set timescale normal
        {
            Time.timeScale = 1;
        }
    }

    /// <summary>
    /// Handles the hitstun actions for characters
    /// </summary>
    /// <param name="duration"> Duration to stun for </param>
    /// <returns> Time </returns>
    public virtual IEnumerator StartHitStun(float duration)
    {
        if (duration > 0)
        {
            if (hitStunPrefab && hitStunActual != null) hitStunActual = Instantiate(hitStunPrefab, transform);
            stunned = true;
            float timeStarted = Time.time;
            while (Time.time - timeStarted < duration)
            {
                if (this == PlayerController.instance.currentCharacter) PlayerController.instance.SetAllowMovement(false);
                yield return null;
            }
            if (attackingPrimary) // Reset primary and secondary abilities
            {
                attackingPrimary = false;
                timeLastPrimary = Time.time;
            }
            if (attackingSecondary)
            {
                attackingSecondary = false;
                timeLastSecondary = Time.time;
            }

            if (attackState != AttackState.Neutral)
            {
                attackState = AttackState.Neutral;
            }

            StartCoroutine(EnableMovement());
            stunned = false;
            Destroy(hitStunActual);
            hitStunActual = null;
        }
    }

    public virtual void CreateHitStun()
    {

    }

    public virtual void HandleHitStun()
    {

    }

    public void SetPrimaryStatus(bool val)
    {
        attackingPrimary = val;
    }

    public void SetSecondaryStatus(bool val)
    {
        attackingSecondary = val;
    }

    public void SetBaseStats()
    {
        baseMovementSpeed = movementSpeed;
        basePrimaryCooldown = primaryCooldown;
        baseSecondaryCooldown = secondaryCooldown;
    }

    public float GetCooldownPrimary()
    {
        return primaryCooldown - (Time.time - timeLastPrimary);
    }

    public float GetCooldownSecondary()
    {
        return secondaryCooldown - (Time.time - timeLastSecondary);
    }

    /// <summary>
    /// Virtual function that is called on any characters primary attack started
    /// </summary>
    public virtual void PrimaryAttack()
    {
    }

    /// <summary>
    /// Virutal function that is called on any characters secondary attack started
    /// </summary>
    public virtual void SecondaryAttack()
    {
    }

    /// <summary>
    /// Resets the primary combo of this character back to in an inactive state (-1)
    /// </summary>
    public void ResetPrimaryComboStep()
    {
        currentPrimaryComboStep = -1;
        characterAnimator.SetPrimaryComboEnded();
    }



    public virtual IEnumerator BeginPrimary()
    {
        if (gameObject != null)
        {
            if (currentPrimaryComboStep == -1 || Time.time - timeLastPrimary >= primaryComboMinTime[currentPrimaryComboStep])
            {
                if (PlayerController.instance.currentCharacter == this)
                {
                    health.SubHealth(primaryAttackCost);
                }

                currentPrimaryComboStep += 1;

                if (currentPrimaryComboStep >= primaryComboSteps)
                {
                    currentPrimaryComboStep = 0;
                }
                characterAnimator.SwitchState("PrimaryAttack");

                timeLastPrimary = Time.time;
                yield return StartCoroutine(characterAnimator.WaitForDelay("PrimaryAttack", currentPrimaryComboStep));

                PrimaryAttack();
            }

        }
    }

    public virtual IEnumerator BeginSecondary()
    {
        characterAnimator.SwitchState("SecondaryAttack");
        yield return StartCoroutine(characterAnimator.WaitForDelay("SecondaryAttack", 0));
        if (gameObject)
        {
            if (PlayerController.instance.currentCharacter == this)
            {
                health.SubHealth(secondaryAttackCost);
            }
            SecondaryAttack();

        }
    }

    public virtual void Explode()
    {

    }

    public virtual Vector3 GetCurrentSpeedVector()
    {
        return new Vector3(0, 0, 0);
    }

    public void EndAttacks()
    {
        SetPrimaryAttack(false);
        SetSecondaryAttack(false);
    }

    public void SetPrimaryAttack(bool val)
    {
        attackingPrimary = val;
    }

    public void SetSecondaryAttack(bool val)
    {
        attackingSecondary = val;
    }

    /// <summary>
    /// Deflects the user's current velocity towards a different direction
    /// </summary>
    /// <param name="direction"> Direction to deflect towards </param>
    public virtual void DeflectVelocity(Vector3 direction)
    {
        float currentMagnitude = velocity.magnitude;
        direction.y = 0;
        direction = direction.normalized;
        velocity = direction * currentMagnitude;
    }

    /// <summary>
    /// Setter function for the player controller to use when changing velocity so that
    /// when the player is controlling a character the velocity is accessible through character
    /// </summary>
    /// <param name="vel"> Velocity to set </param>
    public void SetVelocity(Vector3 vel)
    {
        velocity = vel;
    }

    /// <summary>
    /// Checks the health controller to see if the character is low health
    /// </summary>
    /// <returns> True if low health </returns>
    public bool IsLowHealth()
    {
        return health.IsLowHealth;
    }

    /// <summary>
    /// Checks the attack state to see if able to start an attack
    /// </summary>
    /// <returns></returns>
    public bool IsNeutral()
    {
        if (attackState == AttackState.Neutral) return true;
        return false;
    }

    /// <summary>
    /// Checks if the enemy is winding up or approaching (no other attacks can be started then)
    /// </summary>
    /// <returns> True if other enemies can attack </returns>
    public bool InAttackStartup()
    {
        if (attackState == AttackState.Windup || attackState == AttackState.Approaching) return true;
        return false;
    }

    /// <summary>
    /// Sets the hitcharacter value
    /// </summary>
    /// <param name="val"></param>
    public void SetHitCharacter(bool val)
    {
        hitCharacter = val;
    }

    /// <summary>
    /// Sets the attacking enemy
    /// </summary>
    /// <param name="attacker"> Enemy to set </param>
    public void SetAttacker(Character attacker)
    {
        attackingEnemy = attacker;
    }

    /// <summary>
    /// Gets the attacker
    /// </summary>
    /// <returns> Attacker </returns>
    public Character GetAttacker()
    {
        return attackingEnemy;
    }

    /// <summary>
    /// Checks if a character is dodgable
    /// </summary>
    /// <returns> True if dodgable </returns>
    public bool Dodgable()
    {
        return dodgable;
    }

    /// <summary>
    /// Sets dodged to true
    /// </summary>
    public void SetDodged()
    {
        attackDodged = true;
    }

    /// <summary>
    /// Handles dodging for a character
    /// </summary>
    /// <param name="wellTimed"></param>
    /// <param name="attacker"></param>
    /// <param name="dodgeRange"></param>
    /// <param name="inputTime"></param>
    public IEnumerator Dodge(bool wellTimed, Character attacker, float dodgeRange, float inputTime)
    {
        dodging = true;
        attackState = AttackState.Dodging;
        PlayerController.instance.SetAllowMovement(false);

        if (wellTimed)
        {
            Time.timeScale = 0.25f;
        }
        int attackDirection;
        Vector3 dodgeDirection;

        if (attacker)
        {

            attacker.SetDodged();
            Vector3 toAttacker = attacker.transform.position - transform.position;

            Vector3 direction = PlayerController.instance.GetMovementDirection();

            if (direction.magnitude < 0.01f) // If inputting in direction
            {
                attackDirection = 0; // Backwards
            }
            else
            {
                float angle = Vector3.SignedAngle(direction, toAttacker, Vector3.up);

                if (angle <= 0 && angle > -135)
                {
                    attackDirection = -1; // Left
                }
                else if (angle > 0 && angle < 135)
                {
                    attackDirection = 1; // Right
                }
                else
                {
                    attackDirection = 0;
                }
            }

            if (attackDirection == 0) // Dodge backwards
            {
                dodgeDirection = -toAttacker.normalized;
            }
            else if (attackDirection == -1)
            {
                dodgeDirection = Quaternion.AngleAxis(90f, Vector3.up) * toAttacker.normalized;
            }
            else
            {
                dodgeDirection = Quaternion.AngleAxis(-90f, Vector3.up) * toAttacker.normalized;
            }
        }
        else
        {
            dodgeDirection = -transform.forward.normalized;
        }


        targetTweenPosition = transform.position + dodgeDirection * dodgeRange;
        Vector3 lookBackDir = (targetTweenPosition - transform.position).normalized;
        lookBackDir.y = 0;

        bool moving = true;

        GetComponent<CharacterController>().enabled = false;
        transform.DOMove(targetTweenPosition, 0.5f).OnComplete(() => moving = false);
        while (moving)
        {
            yield return null;
        }
        transform.position = targetTweenPosition;
        GetComponent<CharacterController>().enabled = true;

        velocity = Vector3.zero;
        Time.timeScale = 1;
        PlayerController.instance.SetAllowMovement(true);
        attackState = AttackState.Neutral;

        StartCoroutine(HandleAfterDodge(inputTime));
    }

    /// <summary>
    /// Handles time to input abilities after the dodge
    /// </summary>
    /// <param name="inputTime"> Time range the player is able to input </param>
    /// <returns> Time </returns>
    public IEnumerator HandleAfterDodge(float inputTime)
    {
        float timeStarted = Time.time;
        while (Time.time - timeStarted < inputTime && inCounter)
        {
            yield return null;
        }
        dodging = false;
    }

    /// <summary>
    /// Creates a costly area around the player that enemies will avoid entering
    /// </summary>
    public void CreateLocalInvalidArea()
    {
        if (Vector3.Distance(transform.position, previousCostlyPosition) > invalidAreaResetThreshold)
        {
            ResetInvalidArea();

            costlyNodes = GraphBuilder.instance.GetNodesInRadius(gameObject, sizeRadius);
            foreach (List<int> position in costlyNodes)
            {
                GraphBuilder.instance.AddNodeCost(position, this, 50);
            }
            previousCostlyPosition = transform.position;
        }
    }

    /// <summary>
    /// Resets the costly area values
    /// </summary>
    public void ResetInvalidArea()
    {
        foreach (List<int> position in costlyNodes)
        {
            GraphBuilder.instance.AddNodeCost(position, this, -50);
        }
    }
    /// <summary>
    /// Gets the character controller component, if it's not found it will get it from the game object
    /// </summary>
    /// <returns> The character controller component </returns>
    public CharacterController GetCharacterController()
    {
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }
        return characterController;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (velocity.magnitude > 0.5f && hit.gameObject != gameObject && hit.gameObject.TryGetComponent(out KnockbackControl knockback))
        {

            float force = weight * velocity.magnitude * pushForceModifer;
            Vector3 direction = ((knockback.transform.position - transform.position).normalized + velocity.normalized).normalized;
            direction.y = 0;
            direction = direction.normalized;
            knockback.AddImpact(direction, force);
            GetComponent<KnockbackControl>().AddImpact(-direction, force);
        }

        if (hit.gameObject.layer == environment) // If colliding with environment, reset impact
        {
            GetComponent<KnockbackControl>().ResetImpact();
        }
    }
}
