using FMOD.Studio;
using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base animator controller for characters.
/// Handles animation state transitions and enforces timing rules.
/// </summary>
public class CharacterAnimator : MonoBehaviour
{
    [SerializeField, Tooltip("Are you a dev? [Don't check this if you're not a dev!!]")]
    protected private bool dev = false;

    [Header("Animation Timings")]
    [SerializeField, Tooltip("Time delay before completing the primary ability animation."), ShowIf(nameof(dev))]
    protected float[] primaryAnimationDelay = { 0.5f };
    [SerializeField, Tooltip("Time delay before completing the secondary ability animation."), ShowIf(nameof(dev))]
    protected float secondaryAnimationDelay = 0.5f;

    [Header("Animation Speed Multipliers")]
    [SerializeField, Tooltip("Walk animation speed multiplier."), Range(0.1f, 10f)]
    protected float walkSpeedMult = 1f;
    [SerializeField, Tooltip("Idle animation speed multiplier."), Range(0.1f, 10f)]
    protected float idleSpeedMult = 1f;
    [SerializeField, Tooltip("Death animation speed multiplier."), Range(0.1f, 10f)]
    protected float deathSpeedMult = 1f;
    [SerializeField, Tooltip("Hit stum animation speed multiplier."), Range(0.1f, 10f)]
    protected float hitStunMult = 1f;


    [Tooltip("The possible animation states this animator can enter")]
    protected HashSet<string> animationStates = new HashSet<string>
    {
            "Idle", "Run", "PrimaryAttack", "SecondaryAttack", "Death", "Jump", "Hit", "Overriding"
    };

    [Tooltip("The current animation state of the character")]
    protected string currentAnimationState = "Idle";
    [Tooltip("Whether the animation state can change right now")]
    protected bool canChange = true;
    [Tooltip("Holds the current animator state info")]
    protected AnimatorStateInfo stateInfo;
    [Tooltip("The character this animator is working on")]
    protected Character character;
    [Tooltip("Animator component responsible for handling character animations.")]
    protected Animator animator;
    [Tooltip("Character controller attached to this gameobject.")]
    protected CharacterController characterController;
    protected bool overriding;

    protected virtual void Awake()
    {
        animator = GetComponentInChildren<Animator>();

        characterController = GetComponent<CharacterController>();
        character = GetComponent<Character>();

        if (animator != null)
        {
            animator.SetFloat("IdleSpeedMult", idleSpeedMult);
            animator.SetFloat("WalkSpeedMult", walkSpeedMult);
            animator.SetFloat("DeathSpeedMult", deathSpeedMult);
            animator.SetFloat("HitSpeedMult", hitStunMult);
        }
    }

    protected virtual void Update()
    {
        if (animator == null)
        {
            Debug.LogWarning($"[{nameof(CharacterAnimator)}] No animator assigned on {gameObject.name}");
            return;
        }

        // Idle/run switching
        if (characterController != null && !overriding)
        {
            if(PlayerController.instance.currentCharacter == character)
            {
                if (PlayerController.instance.movementInput.magnitude < 0.1f)
                {
                    SwitchState("Idle", character.GetCurrentPrimaryComboStep(), character.GetTimeLastPrimary(), character.GetPrimaryComboResetTime());
                }
                else
                {
                    SwitchState("Run", character.GetCurrentPrimaryComboStep(), character.GetTimeLastPrimary(), character.GetPrimaryComboResetTime());
                }
            }
            else
            {
                if (character == null) return;
                if (!character.GetAnimateMove())
                {
                    SwitchState("Idle", 0, character.GetTimeLastPrimary(), character.GetPrimaryComboResetTime());
                }
                else
                {
                    SwitchState("Run", 0, character.GetTimeLastPrimary(), character.GetPrimaryComboResetTime());
                }
            }
        }
    }

    public float GetHitStunMult()
    {
        return hitStunMult;
    }

    public IEnumerator SetHit()
    {
        if(animator != null)
        {
            if(!overriding)
            {
                currentAnimationState = "Hit";
                ResetAllTriggers();
                animator.SetFloat("HitSpeedMult", hitStunMult);
                canChange = false;
                if (character == PlayerController.instance.currentCharacter)
                {
                    PlayerController.instance.SetAllowMovement(false);
                }
                animator.SetTrigger("Hit");
                yield return new WaitForSeconds(0.12f / hitStunMult);
                canChange = true;
                if (character == PlayerController.instance.currentCharacter)
                {
                    PlayerController.instance.SetAllowMovement(true);
                }
            }
        }
        else
        {
            Debug.LogWarning("Animator is not assigned!");
        }
    }

    /// <summary>
    /// Sets if the character needs to move to start the primary attack
    /// Sets bool that activtes the windup state of the primary attack animation
    /// </summary>
    /// <param name="val">The value to set if primary attack movement is needed</param>
    public void SetPrimaryMovementNeeded(bool val)
    {
        if (animator != null)
        {
            animator.SetBool("PrimaryMovementNeeded", val);
        }
        else
        {
            Debug.LogWarning("Animator on this character is not set!");
        }
    }

    /// <summary>
    /// Switches the character's animation state and updates the Animator accordingly.
    /// </summary>
    public virtual void SwitchState(string newState, int currentPrimaryComboStep, float timeLastPrimary, float[] primaryComboResetTime)
    {
        if (overriding) return;

        if (currentAnimationState == "PrimaryAttack" && currentPrimaryComboStep != -1 && Time.time - timeLastPrimary >= primaryComboResetTime[currentPrimaryComboStep])
        {
            character.ResetPrimaryComboStep();
        }

        animator.SetInteger("PrimaryCombo", currentPrimaryComboStep);

        SwitchState(newState);
    }

    /// <summary>
    /// Switches the character's animation state and updates the Animator accordingly.
    /// Use when setting primary attack state 
    /// </summary>
    public virtual void SwitchState(string newState, int currentPrimaryComboStep)
    {
        if (overriding) return;

        animator.SetInteger("PrimaryCombo", currentPrimaryComboStep);

        SwitchState(newState);
    }

    /// <summary>
    /// Switches the character's animation state and updates the Animator accordingly.
    /// </summary>
    public virtual void SwitchState(string newState)
    {
        if (overriding) return;

        if (!animationStates.Contains(newState))
        {
            Debug.LogWarning("This animation state: " + newState + " does not exist!");
        }

        if (newState == "PrimaryAttack")
        {
            ResetAllTriggers();
            animator.SetTrigger("PrimaryAttack");
            canChange = false;
            currentAnimationState = newState;
        }
        else if(newState == "Death")
        {
            ResetAllTriggers();
            animator.SetTrigger("Death");
            canChange = false; 
            currentAnimationState = newState;
        }

        if (!canChange || currentAnimationState == "Death" || currentAnimationState == newState)
            return;

        currentAnimationState = newState;

        if (animator == null) return;

        ResetAllTriggers();

        switch (newState)
        {
            case "Idle":
                animator.SetFloat("IdleSpeedMult", idleSpeedMult);
                animator.SetTrigger("Idle");
                canChange = true;
                break;
            case "Run":
                animator.SetTrigger("Run");
                canChange = true;
                break;
            case "PrimaryAttack":
                animator.SetTrigger("PrimaryAttack");
                canChange = false;
                break;
            case "SecondaryAttack":
                animator.SetTrigger("SecondaryAttack");
                canChange = false;
                break;
            case "Death":
                animator.SetFloat("DeathSpeedMult", deathSpeedMult);
                animator.SetTrigger("Death");
                canChange = false;
                break;
        }
    }

    /// <summary>
    /// Resets all animation triggers to avoid conflicting transitions.
    /// </summary>
    protected virtual void ResetAllTriggers()
    {
        if (animator != null)
        {
            animator.ResetTrigger("Idle");
            animator.ResetTrigger("Run");
            animator.ResetTrigger("PrimaryAttack");
            animator.ResetTrigger("SecondaryAttack");
            animator.ResetTrigger("Death");
            animator.ResetTrigger("Hit");
        }
        else
        {
            Debug.LogWarning("Animator is not assigned!");
        }
    }

    /// <summary>
    /// Checks if the character is not currently in a primary attack animation.
    /// </summary>
    public bool NotInPrimary()
    {
        return currentAnimationState != "PrimaryAttack";
    }

    /// <summary>
    /// Returns the animation state the character is currently in
    /// </summary>
    /// <returns>Current animation state </returns>
    public string GetCurrentState()
    {
        return currentAnimationState;
    }

    /// <summary>
    /// Waits for a delay corresponding to the current animation state.
    /// </summary>
    public virtual IEnumerator WaitForDelay(string animation, int comboNum)
    {
        switch (animation)
        {
            case "PrimaryAttack":
                if (PlayerController.instance.currentCharacter == character)
                    yield return new WaitForSeconds(primaryAnimationDelay[comboNum]);
                else
                    yield return new WaitForSeconds(primaryAnimationDelay[0]);
                break;
            case "SecondaryAttack":
                yield return new WaitForSeconds(secondaryAnimationDelay);
                break;
        }
    }

    public virtual void SetSecondaryAttackEnded()
    {
        canChange = true;
    }

    public void SetPrimaryComboEnded()
    {
        canChange = true;
    }

    /// <summary>
    /// Waits for the end of an animation before allowing new state changes.
    /// </summary>
    protected virtual IEnumerator WaitForEndAnimation(float sec)
    {
        yield return new WaitForSeconds(sec);
        canChange = true;
    }

    /// <summary>
    /// Overrides the animator untill EnAnimatorOverride is called
    /// Sets the specified trigger
    /// </summary>
    /// <param name="animationTrigger"></param>
    public void OverrideAnimator(string animationTrigger)
    {
        overriding = true;
        StopAllCoroutines();
        ResetAllTriggers();
        currentAnimationState = "Overriding";
        animator.SetTrigger(animationTrigger);
    }

    /// <summary>
    /// Ends the animator override returning animator to regular function
    /// </summary>
    public void EndAnimatorOverride()
    {
        ResetAllTriggers();
        overriding = false;
    }
}
