using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public abstract class Character : MonoBehaviour
{
    // Abstract class for characters in our game
    const string FILE_ENDING = ".json";

    [Header("Character Settings")]
    [Tooltip("Character Name")]
    public string characterName;
    [Tooltip("Speed the Character Can Move")]
    public float movementSpeed = 5;
    [Tooltip("Weight of the character")]
    public float weight = 10;
    [Tooltip("Character Hitbox Radius")]
    public float sizeRadius;
    [Tooltip("Team of the character")]
    public int teamID;
    [Tooltip("Character Animator")]
    public Animator animator;
    [Tooltip("Primary Fire Image")]
    public Sprite primaryFireIcon;
    [Tooltip("Secondary Fire Image")]
    public Sprite secondaryFireIcon;

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
    [Tooltip("Primary Ability Animation Delay")]
    public float primaryAnimationDelay = 0.5f;
    [Tooltip("Cooldown After Secondary Ability")]
    public float secondaryCooldown = 5;
    [Tooltip("Secondary Ability Animation Delay")]
    public float secondaryAnimationDelay;

    [Tooltip("Primary Attack Range")]
    public float primaryAttackRange;

    [Tooltip("Maximum Health")]
    public float maxHealth;

    [Header("Hit Stun Settings")]
    [Tooltip("Hit Stun Prefab")]
    public GameObject hitStunPrefab;

    [Tooltip("Hit Stun Duration")]
    public float hitStunDuration = 0.5f;

    public LayerMask characters;

    protected float timeLastPrimary = -Mathf.Infinity;
    protected float timeLastSecondary = -Mathf.Infinity;

    protected float timeLastAny;

    protected float currentHealth;

    private bool alive = true;

    protected bool invincible = false; // Title card

    protected GameObject hitStunActual = null;

    protected float timeLastHit;

    protected bool attackingPrimary = false;
    protected bool attackingSecondary = false;

    protected float baseMovementSpeed;
    protected float basePrimaryCooldown;
    protected float baseSecondaryCooldown;

    protected bool inPrimaryStartAnim = false;
    protected bool inSecondaryStartAnim = false;

    protected bool releasePrimaryImm = false;
    protected bool releaseSecondaryImm = false;

    private int currentPrimaryComboStep = 0;

    #region Saving/Loading

    [ContextMenu("Save to JSON")]
    public void SaveToJson()
    {
        string characterStatsStr = JsonUtility.ToJson(this);

        string folderPath = Path.Combine(Application.dataPath, "JSON");
        folderPath = Path.Combine(folderPath, "CharacterStats");
        SeeFilePath();
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string filePath = Path.Combine(folderPath, characterName + FILE_ENDING);
        File.WriteAllText(filePath, characterStatsStr);

        UnityEditor.AssetDatabase.Refresh();
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
        JsonUtility.FromJsonOverwrite(jsonStr, this);

        UnityEditor.AssetDatabase.Refresh();
    }

    #endregion

    public virtual void PrimaryAttack()
    {
    }

    public virtual void SecondaryAttack()
    {
    }

    public abstract void Die();

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
        if (attackingPrimary || attackingSecondary || inPrimaryStartAnim) return false;
        if (InStartAnim()) return false;

        return true;
    }

    public virtual bool CheckSecondaryUsable()
    {
        if (!CheckSecondaryCooldown()) return false;
        if (attackingPrimary || attackingSecondary) return false;
        if (InStartAnim()) return false;

        return true;
    }

    public float GetHealth()
    {
        return currentHealth;
    }

    public float GetMaxHealth()
    {
        return maxHealth;
    }

    public void AddHealth(float amt)
    {
        currentHealth += amt;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
    }

    public virtual void SubHealth(float dmg)
    {
        if (!invincible)
        {
            timeLastHit = Time.time;
            currentHealth -= dmg;
            if (currentHealth <= 0)
            {
                Die();
            }
            else
            {
                CreateHitStun();
            }
        }
    }

    public virtual void DrainLife(float amt)
    {
        if (!invincible)
        {
            currentHealth -= amt;
            if (currentHealth <= 0)
            {
                Die();
            }
        }
    }

    public void SetHealthToMax()
    {
        currentHealth = maxHealth;
    }

    public bool IsAlive()
    {
        return alive;
    }

    public virtual void SetControlled(bool v) { }

    public void SetTeamID(int id)
    {
        teamID = id;
    }

    public IEnumerator EnableMovement() // Call this after any movement abilities
    {
        yield return new WaitForSeconds(0.1f);
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

    public virtual void ReleaseSecondary()
    {
        if (releaseSecondaryImm)
        {
            releaseSecondaryImm = false;
        }

        if (InStartAnim())
        {
            releaseSecondaryImm = true;
        }
    }

    public virtual void ReleasePrimary()
    {
        if (releasePrimaryImm)
        {
            releasePrimaryImm = false;
        }

        if (InStartAnim())
        {
            releasePrimaryImm = true;
        }
    }

    public virtual void CreateHitStun()
    {

    }

    public virtual void HandleHitStun()
    {
        if (hitStunActual != null)
        {
            if (Time.time - timeLastHit > hitStunDuration)
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

    public void SetPrimaryAnimStatus(bool val)
    {
        inPrimaryStartAnim = val;
    }

    public void SetSecondaryStatus(bool val)
    {
        attackingSecondary = val;
    }

    public void SetSecondaryAnimStatus(bool val)
    {
        inSecondaryStartAnim = val;
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

            SetPrimaryAnimStatus(true);
            AnimatePrimary();
            yield return new WaitForSeconds(primaryAnimationDelay);
            PrimaryAttack();
            inPrimaryStartAnim = false;
        }
    }

    public virtual IEnumerator BeginSecondary()
    {
        yield return new WaitForSeconds(secondaryAnimationDelay);
        if (gameObject)
        {
            SecondaryAttack();
            inSecondaryStartAnim = false;
        }
    }

    public bool InStartAnim()
    {
        return (inPrimaryStartAnim || inSecondaryStartAnim);
    }

    public bool CheckInAnimations()
    {
        if (animator.IsInTransition(0)) return true;
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Possess")) return true;
        if (attackingPrimary || inPrimaryStartAnim) return true;
        // Check in secondary animation
        return false;
    }

    public void AnimateMove()
    {
        if (animator)
        {
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Run") && !CheckInAnimations())
            {
                Debug.Log(attackingPrimary);
                animator.SetTrigger("StartRunning");
            }
        }
    }

    public void AnimateIdle()
    {
        if (animator)
        {
            if (!CheckInAnimations()&&!animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"))
            {
                animator.SetTrigger("StartIdle");
            }
        }
    }

    public void AnimatePossess()
    {
        if (animator)
        {
            if (!CheckInAnimations())
            {
                animator.SetTrigger("StartPossess");
            }
        }
    }

    public void AnimatePrimary()
    {
        if (animator)
        {
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Possess"))
            {
                Debug.Log(characterName + " started primary animation " + inPrimaryStartAnim);
                if (primaryComboSteps == 0)
                {
                    animator.SetTrigger("StartPrimary");
                }
                else
                {
                    for (int i = 1; i <= primaryComboSteps; i++)
                    {
                        if (i == currentPrimaryComboStep)
                        {
                            string triggerString = "StartPrimary" + i;
                            animator.SetTrigger(triggerString);
                            Debug.Log(triggerString);
                        }
                    }
                }
            }
        }
    }

    public virtual void Explode()
    {

    }
}
