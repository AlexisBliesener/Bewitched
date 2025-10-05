using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Cinemachine;
using UnityEngine.AI;
using DG.Tweening;
using FMOD;
using Debug = UnityEngine.Debug;

[RequireComponent(typeof(HealthController))]
[RequireComponent(typeof(CharacterAnimator))]
public abstract class Character : MonoBehaviour
{
    // Abstract class for characters in our game
    const string FILE_ENDING = ".json";

    [Header("Character Settings")]
    [Tooltip("Character Name")]
    public string characterName;

    [Header("Movement Settings")]
    [Tooltip("Speed the Character Can Move While Chasing")]
    public float movementSpeed = 5;
    [Tooltip("Speed the character can move while approaching for an attack")]
    public float approachSpeed = 7;
    [SerializeField ,Tooltip("How much yVelocity the Character will get when hitting jump")]
    private float jumpSpeed;
    [SerializeField, Tooltip("How long to wait before doing a jump")]
    private float jumpDelay;
    [Tooltip("Acceleration of the Character")]
    public float acceleration = 5;
    [Tooltip("Deceleration of the Character")]
    public float deceleration = 5;
    [Tooltip("Rotational velocity of the character")]
    public float rotationalVelocity = 240;
    [Tooltip("Time chasing a character for an attack")]
    public float chaseTime = 3;
    public float windupTime = 0.25f;

    [Tooltip("Weight of the character")]
    public float weight = 10;
    [Tooltip("Character Hitbox Radius")]
    public float sizeRadius;
    [Tooltip("Number of Points to Surround")]
    public int numSurroundingPoints = 8;
    [Tooltip("Minimum Radius of Surrounding Points (For AI Navigation)")]
    public float minSurroundingRadius = 2;
    [Tooltip("Maximum Radius of Surrounding Points (For AI Navigation)")]
    public float maxSurroundingRadius = 5;
    [Tooltip("Team of the character")]
    public int teamID;
    [Tooltip("Primary Fire Image")]
    public Sprite primaryFireIcon;
    [Tooltip("Secondary Fire Image")]
    public Sprite secondaryFireIcon;
    [SerializeField, Tooltip("The shoulder offset the camera has from the character")]
    private Vector3 shoulderOffset = new Vector3(1f, 2.5f, 0f);

    [Tooltip("Attack Delay")]
    public float attackDelay = 1;
    [Tooltip("Cooldown After Primary Ability")]
    public float primaryCooldown = 5;
    [Tooltip("Cooldown After Secondary Ability")]
    public float secondaryCooldown = 5;
    [Tooltip("Primary Attack Range")]
    public float primaryAttackRange;


    [Header("Primary Combo Stats")]
    [Tooltip("Primary Combo Steps")]
    public int primaryComboSteps;
    [Tooltip("Primary Cooldown Reset Time")]
    public float[] primaryComboResetTime;
    [Tooltip("Primary combo min time to wait to hit the next combo")]
    public float[] primaryComboMinTime;

    [Header("Note: Health settings can be changed on the Health Controller component!")]
    [SerializeField] public HealthController health;

    [Header("Possession Settings")]
    [Tooltip("Can the player possess the character?")]
    public bool canPossess = true;

    [Header("Hit Stun Settings")]
    [Tooltip("Hit Stun Prefab")]
    public GameObject hitStunPrefab;

    [Tooltip("Hit Stun Duration")]
    public float hitStunDuration = 0.5f;

    public LayerMask characters;

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

    protected int currentPrimaryComboStep = 0;

    [SerializeField, Tooltip("The Cinemachine FreeLook camera used for zoomed out in combat movement.")]
    private CinemachineFreeLook combatCam;
    [SerializeField, Tooltip("The Cinemachine Virtual Camera used for aiming and close-up view.")]
    private CinemachineVirtualCamera aimCam;
    [SerializeField, Tooltip("The Cinemachine explore Camera used for regular out of combat view.")]
    private CinemachineFreeLook exploreCam;

    [Tooltip("The script that controls chaning animation states")]
    protected CharacterAnimator characterAnimator;

    public List<AttackStatusEffects> attackEffects = new List<AttackStatusEffects>(); // This list is for simple saving
    [SerializeField] private List<string> effectJSONs = new List<string>();

    private SurroundingPoints surroundingPoints;

    [Tooltip("Character to lock onto")]
    protected Character lockedCharacter = null;

    [Tooltip("The coroutine handling attacking actions")]
    protected Coroutine attackStateCoroutine = null;

    [Tooltip("Bool determining if an attack has hit a character")]
    protected bool hitCharacter = false;

    protected bool dodgable = false;
    protected bool attackDodged = false;
    protected bool dodging = false;
    private bool invulnerable = false;

    protected bool inCounter = false;

    protected Character attackingEnemy = null;

    [Tooltip("Attack indicator prefab")]
    public GameObject attackIndicatorPrefab;

    [Tooltip("Attack indicator")]
    protected GameObject attackIndicator = null;

    [Tooltip("Target tween position")]
    protected Vector3 targetTweenPosition;

    protected float timeLastDodge = 0;

    protected bool stunned = false;

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
    protected AttackState attackState;

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

    protected virtual void FixedUpdate()
    {
    }

    /// <summary>
    /// Returns the amount of time to wait before doing a jump
    /// For animation purposes
    /// </summary>
    /// <returns>The amount of time to wait before doing a jump </returns>
    public float GetJumpDelay()
    {
        return jumpDelay;
    }

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

    public int GetCurrentPrimaryComboStep()
    {
        return currentPrimaryComboStep;
    }

    public float GetTimeLastPrimary()
    {
        return timeLastPrimary;
    }

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
        DeactivateSurroundingPoints();
        StopAllCoroutines();
        Die();
    }

    /// <summary>
    /// Called when the character dies to switch animation state to death
    /// </summary>
    public void AnimateDeath()
    {
        characterAnimator.SwitchState("Death", currentPrimaryComboStep, timeLastPrimary, primaryComboResetTime);
    }

    public abstract void Die();

    /// <summary>
    /// Return the jump speed of this character
    /// </summary>
    /// <returns>The jump speed of this character</returns>
    public float GetJumpSpeed()
    {
        return jumpSpeed;
    }

    protected virtual bool CheckPrimaryCooldown() {
        float cooldown = primaryCooldown;
        return Time.time - timeLastPrimary >= cooldown && Time.time - timeLastAny >= attackDelay;
    }

    protected bool CheckSecondaryCooldown() {
        return Time.time - timeLastSecondary >= secondaryCooldown && Time.time - timeLastAny >= attackDelay;
    }

    public virtual bool CheckPrimaryUsable()
    {
        if (!CheckPrimaryCooldown()) return false;
        if (attackingPrimary || attackingSecondary || !characterAnimator.NotInPrimary() || stunned) return false;

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

    /// <summary>
    /// Sets the characters animation state to jump
    /// </summary>
    public void Jump()
    {
        characterAnimator.SwitchState("Jump", currentPrimaryComboStep, timeLastPrimary, primaryComboResetTime);
    }

    public IEnumerator StartTime(float stopTime)
    {
        yield return new WaitForSecondsRealtime(stopTime);

        if (!GameObject.FindGameObjectWithTag("PauseMenu")) // If not paused set timescale normal
        {
            Time.timeScale = 1;
        }
    }

    public virtual IEnumerator StartHitStun(float duration)
    {
        hitStunActual = Instantiate(hitStunPrefab, transform);
        stunned = true;
        float timeStarted = Time.time;
        while (Time.time - timeStarted < duration)
        {
            PlayerController.instance.SetAllowMovement(false);
            yield return null;
        }
        PlayerController.instance.SetAllowMovement(true);
        stunned = false;
        Destroy(hitStunActual); hitStunActual = null;
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

    public virtual void PrimaryAttack()
    {
    }

    public virtual void SecondaryAttack()
    {
    }

    public void ResetPrimaryComboStep()
    {
        currentPrimaryComboStep = 0;
        characterAnimator.SetPrimaryComboEnded();

       // primaryCoolDownStart = Time.time;
    }

    public virtual IEnumerator BeginPrimary()
    {
        if (gameObject != null)
        {
            if(currentPrimaryComboStep == 0 || (  Time.time - timeLastPrimary <= primaryComboResetTime[currentPrimaryComboStep] && Time.time - timeLastPrimary >= primaryComboMinTime[currentPrimaryComboStep]))
            {
                if (Time.time - timeLastPrimary >= primaryComboResetTime[currentPrimaryComboStep])
                {
                    ResetPrimaryComboStep();
                }

                if (currentPrimaryComboStep >= primaryComboSteps)
                {
                    ResetPrimaryComboStep();
                }
                timeLastPrimary = Time.time;

                characterAnimator.SwitchState("PrimaryAttack", currentPrimaryComboStep, timeLastPrimary, primaryComboResetTime);
                yield return StartCoroutine(characterAnimator.WaitForDelay("PrimaryAttack", currentPrimaryComboStep));

                currentPrimaryComboStep += 1;

                Debug.Log("Starting Attack Function");
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
    /// Create surrounding points for AI navigation
    /// </summary>
    public void ActivateSurroundingPoints()
    {
        if (!surroundingPoints)
        {
            gameObject.TryGetComponent<SurroundingPoints>(out surroundingPoints);
        }

        if (surroundingPoints != null)
        {
            surroundingPoints.Init(numSurroundingPoints, minSurroundingRadius, maxSurroundingRadius);
        }
    }

    /// <summary>
    /// Destroy the surrounding points when inactive
    /// </summary>
    public void DeactivateSurroundingPoints()
    {
        if(surroundingPoints != null)
        {
            surroundingPoints.DestroyPoints();
        }
    }

    /// <summary>
    /// Finds the closest available surrounding point
    /// </summary>
    /// <param name="enemy"> Enemy searching for a point </param>
    /// <returns></returns>
    public GameObject FindClosestSurroundingPoint(Enemy enemy)
    {
        return surroundingPoints.AssignPoint(enemy);
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
    /// Gives the character invulnerability for a duration
    /// </summary>
    /// <param name="duration"> Duration to be invulnerable </param>
    /// <returns> Time </returns>
    public IEnumerator GiveInvulnerability(float duration)
    {
        invulnerable = true;
        yield return new WaitForSeconds(duration);
        invulnerable = false;
    }

    /// <summary>
    /// Gets the invulnerability status
    /// </summary>
    /// <returns> Invulnerability status </returns>
    public bool Invulnerable()
    {
        return invulnerable;
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
            GiveInvulnerability(0.75f);
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

    public IEnumerator HandleAfterDodge(float inputTime)
    {
        float timeStarted = Time.time;
        while (Time.time - timeStarted < inputTime && inCounter)
        {
            yield return null;
        }
        dodging = false;
    }
}
