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
    [SerializeField, Tooltip("Time delay before completing the primary ability animation."), ShowIf("dev")]
    protected float[] primaryAnimationDelay = { 0.5f };
    [SerializeField, Tooltip("Time delay before completing the secondary ability animation."), ShowIf("dev")]
    protected float secondaryAnimationDelay = 0.5f;

    [Header("Animation Speed Multipliers")]
    [SerializeField, Tooltip("Walk animation speed multiplier."), Range(0.1f, 10f)]
    protected float walkSpeedMult = 1f;
    [SerializeField, Tooltip("Idle animation speed multiplier."), Range(0.1f, 10f)]
    protected float idleSpeedMult = 1f;
    [SerializeField, Tooltip("Death animation speed multiplier."), Range(0.1f, 10f)]
    protected float deathSpeedMult = 1f;


    [Tooltip("The possible animation states this animator can enter")]
    protected HashSet<string> animationStates = new HashSet<string>
    {
            "Idle", "Run", "PrimaryAttack", "SecondaryAttack", "Death", "Jump"
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
        if (characterController != null)
        {
            if (characterController.velocity.magnitude < 0.05f)
                SwitchState("Idle",  character.GetCurrentPrimaryComboStep(), character.GetTimeLastPrimary(), character.GetPrimaryComboResetTime());
            else
                SwitchState("Run", character.GetCurrentPrimaryComboStep(), character.GetTimeLastPrimary(), character.GetPrimaryComboResetTime());
        }
    }

    /// <summary>
    /// Sets if the character needs to move to start the primary attack
    /// Sets bool that activtes the windup state of the primary attack animation
    /// </summary>
    /// <param name="val">The value to set if primary attack movement is needed</param>
    public void SetPrimaryMovementNeeded(bool val)
    {
        animator.SetBool("PrimaryMovementNeeded", val);
    }

    /// <summary>
    /// Switches the character's animation state and updates the Animator accordingly.
    /// </summary>
    public virtual void SwitchState(string newState, int currentPrimaryComboStep, float timeLastPrimary, float[] primaryComboResetTime)
    {
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
        animator.SetInteger("PrimaryCombo", currentPrimaryComboStep);

        SwitchState(newState);
    }

    /// <summary>
    /// Switches the character's animation state and updates the Animator accordingly.
    /// </summary>
    public virtual void SwitchState(string newState)
    {
        if (!animationStates.Contains(newState))
        {
            Debug.LogWarning("This animation state: " + newState + " does not exist!");
        }

        if (newState == "PrimaryAttack")
        {
            animator.SetTrigger("PrimaryAttack");
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
                animator.SetFloat("WalkSpeedMult", walkSpeedMult);
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
        animator.ResetTrigger("Idle");
        animator.ResetTrigger("Run");
        animator.ResetTrigger("PrimaryAttack");
        animator.ResetTrigger("SecondaryAttack");
        animator.ResetTrigger("Death");
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
                yield return new WaitForSeconds(primaryAnimationDelay[comboNum]);
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
    /// Waits until the character is back on the ground before letting them change out of the jump animation
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerator WaitForGrounded()
    {
        yield return new WaitForFixedUpdate();
        yield return new WaitUntil(() => characterController.isGrounded);
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
}
