using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Cinemachine;
using UnityEngine.AI;

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
    [SerializeField ,Tooltip("How much yVelocity the Character will get when hitting jump")]
    private float jumpSpeed;
    [Tooltip("Acceleration of the Character")]
    public float acceleration = 5;
    [Tooltip("Deceleration of the Character")]
    public float deceleration = 5;

    [Tooltip("Weight of the character")]
    public float weight = 10;
    [Tooltip("Character Hitbox Radius")]
    public float sizeRadius;
    [Tooltip("Number of Points to Surround")]
    public int numSurroundingPoints = 8;
    [Tooltip("Radius of Surrounding Points (For AI Navigation)")]
    public float surroundingRadius = 2;
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
    [Tooltip("Added Cooldown After Primary Combo")]
    public float primaryComboExtraCooldown;
    [Tooltip("Primary Combo Steps")]
    public int primaryComboSteps;
    [Tooltip("Primary Cooldown Reset Time")]
    public float primaryComboResetTime;
    [Tooltip("Cooldown After Secondary Ability")]
    public float secondaryCooldown = 5;


    [Tooltip("Primary Attack Range")]
    public float primaryAttackRange;

    [Header("Note: Health settings can be changed on the Health Controller component!")]
    [SerializeField] public HealthController health;

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

    private int currentPrimaryComboStep = 0;

    [Tooltip("The Cinemachine FreeLook camera used for third-person movement.")]
    private CinemachineFreeLook freeLookCam;
    [Tooltip("The Cinemachine Virtual Camera used for aiming and close-up view.")]
    private CinemachineVirtualCamera virtualCam;

    [Tooltip("The script that controls chaning animation states")]
    private CharacterAnimator characterAnimator;

    public List<AttackStatusEffects> attackEffects = new List<AttackStatusEffects>(); // This list is for simple saving
    [SerializeField] private List<string> effectJSONs = new List<string>();

    private SurroundingPoints surroundingPoints;

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
        freeLookCam = GetComponentInChildren<CinemachineFreeLook>();
        virtualCam = GetComponentInChildren<CinemachineVirtualCamera>();

        characterAnimator = GetComponent<CharacterAnimator>();
        if (health == null)
        {
            health = GetComponent<HealthController>();
        }
        health.OnDamaged += OnDamaged;
        health.OnHealthChanged += OnHealthChanged;
        health.OnDeath += OnDeath;

        SetBaseStats();
    }
    protected virtual void OnDestroy()
    {
        health.OnDamaged -= OnDamaged;
        health.OnHealthChanged -= OnHealthChanged;
        health.OnDeath -= OnDeath;
    }

    /// <summary>
    /// Returns the Cinemachine FreeLook camera associated with this character.
    /// </summary>
    /// <returns>The FreeLook Cinemachine camera.</returns>
    public CinemachineFreeLook GetFreeLookCam()
    {
        return freeLookCam;
    }

    /// <summary>
    /// Returns the Cinemachine Virtual Camera associated with this character.
    /// </summary>
    /// <returns>The Virtual Cinemachine camera.</returns>
    public CinemachineVirtualCamera GetVirtualCam()
    {
        return virtualCam;
    }

    /// <summary>
    /// OnDamaged is called when the character is damaged.
    /// </summary>
    protected virtual void OnDamaged(float amount)
    {
        CreateHitStun();
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
        Die();
    }

    /// <summary>
    /// Called when the character dies to switch animation state to death
    /// </summary>
    public void AnimateDeath()
    {
        characterAnimator.SwitchState(CharacterAnimator.AnimationStates.death);
    }

    public virtual void PrimaryAttack()
    {
    }

    public virtual void SecondaryAttack()
    {
    }

    public abstract void Die();

    public float GetJumpSpeed()
    {
        return jumpSpeed;
    }

    protected virtual bool CheckPrimaryCooldown() {
        float cooldown = primaryCooldown;
        if (currentPrimaryComboStep >= primaryComboSteps)
        {
            cooldown += primaryComboExtraCooldown;
        }
        return Time.time - timeLastPrimary >= cooldown && Time.time - timeLastAny >= attackDelay;
    }

    protected bool CheckSecondaryCooldown() {
        return Time.time - timeLastSecondary >= secondaryCooldown && Time.time - timeLastAny >= attackDelay;
    }

    public virtual bool CheckPrimaryUsable()
    {
        if (!CheckPrimaryCooldown()) return false;
        if (attackingPrimary || attackingSecondary || !characterAnimator.NotInPrimary()) return false;

        return true;
    }

    public virtual bool CheckSecondaryUsable()
    {
        if (!CheckSecondaryCooldown()) return false;
        if (attackingPrimary || attackingSecondary) return false;

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

    public virtual void CreateHitStun()
    {

    }

    public virtual void HandleHitStun()
    {
        if (hitStunActual != null)
        {
            if (Time.time - health.TimeLastHit > hitStunDuration)
            {
                Destroy(hitStunActual);
                hitStunActual = null;
            }
        }
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

    public virtual IEnumerator BeginPrimary()
    {
        if (gameObject != null)
        {
            if (Time.time - timeLastPrimary >= primaryComboResetTime)
            {
                currentPrimaryComboStep = 0;
            }

            if (currentPrimaryComboStep >= primaryComboSteps)
            {
                currentPrimaryComboStep = 0;
            }

            currentPrimaryComboStep += 1;

            characterAnimator.SwitchState(CharacterAnimator.AnimationStates.primaryAttack);
            yield return StartCoroutine(characterAnimator.WaitForDelay(CharacterAnimator.AnimationStates.primaryAttack));

            PrimaryAttack();
        }
    }

    public virtual IEnumerator BeginSecondary()
    {
        characterAnimator.SwitchState(CharacterAnimator.AnimationStates.secondaryAttack);
        yield return StartCoroutine(characterAnimator.WaitForDelay(CharacterAnimator.AnimationStates.secondaryAttack));
        if (gameObject)
        {
            SecondaryAttack();

        }
    }

    public void AnimatePrimary()
    {
        characterAnimator.SwitchState(CharacterAnimator.AnimationStates.primaryAttack);
    }

    public virtual void Explode()
    {

    }

    public virtual Vector3 GetCurrentSpeedVector()
    {
        return new Vector3(0, 0, 0);
    public void AnimateMove()
    {
        if (animator)
        {
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Run") && !CheckInAnimations())
            {
                animator.SetTrigger("StartRunning");
            }
        }
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
}

    public virtual void Explode()
    {

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

        surroundingPoints.Init(numSurroundingPoints, surroundingRadius);
    }

    /// <summary>
    /// Destroy the surrounding points when inactive
    /// </summary>
    public void DeactivateSurroundingPoints()
    {
        surroundingPoints.DestroyPoints();
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
}
