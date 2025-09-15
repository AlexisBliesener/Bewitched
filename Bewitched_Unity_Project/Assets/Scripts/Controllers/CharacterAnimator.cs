using System.Collections;
using UnityEngine;

/// <summary>
/// Base animator controller for characters.
/// Handles animation state transitions and enforces timing rules.
/// </summary>
public class CharacterAnimator : MonoBehaviour
{
    [Header("Animation Timings")]
    [SerializeField, Tooltip("Time delay before completing the primary ability animation.")]
    private float primaryAnimationDelay = 0.5f;
    [SerializeField, Tooltip("Time delay before completing the secondary ability animation.")]
    private float secondaryAnimationDelay = 0.5f;
    [SerializeField, Tooltip("Primary attack animation length.")]
    private float primaryAttackLength = 1f;
    [SerializeField, Tooltip("Secondary attack animation length.")]
    private float secondaryAttackLength = 1f;

    [Header("References")]
    [SerializeField, Tooltip("Animator component responsible for handling character animations.")]
    protected Animator animator;
    [SerializeField, Tooltip("Character controller attached to this gameobject.")]
    private CharacterController characterController;

    [Tooltip("Defines the possible animation states for the character.")]
    public enum AnimationStates { idle, run, primaryAttack, secondaryAttack, death, possession }
    [Tooltip("The current animation state of the character")]
    protected AnimationStates currentAnimationState = AnimationStates.idle;
    [Tooltip("Whether the animation state can change right now")]
    protected bool canChange = true;
    [Tooltip("Holds the current animator state info")]
    protected AnimatorStateInfo stateInfo;

    protected virtual void Start()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        characterController = GetComponent<CharacterController>();
    }

    protected virtual void Update()
    {
        if (animator == null)
        {
            Debug.LogWarning($"[{nameof(CharacterAnimator)}] No animator assigned on {gameObject.name}");
            return;
        }

        // Track current animator state
        stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        UpdateCurrentStateFromAnimator();

        // Idle/run switching
        if (canChange && characterController != null)
        {
            if (characterController.velocity == Vector3.zero)
                SwitchState(AnimationStates.idle);
            else
                SwitchState(AnimationStates.run);
        }
    }

    /// <summary>
    /// Switches the character's animation state and updates the Animator accordingly.
    /// </summary>
    public virtual void SwitchState(AnimationStates newState)
    {
        if (!canChange || currentAnimationState == AnimationStates.death || currentAnimationState == newState)
            return;

        if (animator == null) return;

        ResetAllTriggers();

        switch (newState)
        {
            case AnimationStates.idle:
                animator.SetTrigger("Idle");
                canChange = true;
                break;
            case AnimationStates.run:
                animator.SetTrigger("Run");
                canChange = true;
                break;
            case AnimationStates.primaryAttack:
                animator.SetTrigger("PrimaryAttack");
                canChange = false;
                StartCoroutine(WaitForEndAnimation(primaryAttackLength));
                break;
            case AnimationStates.secondaryAttack:
                animator.SetTrigger("SecondaryAttack");
                canChange = false;
                StartCoroutine(WaitForEndAnimation(secondaryAttackLength));
                break;
            case AnimationStates.death:
                animator.SetTrigger("Death");
                canChange = false;
                break;
        }
    }

    /// <summary>
    /// Updates currentAnimationState based on the animator’s active state.
    /// </summary>
    private void UpdateCurrentStateFromAnimator()
    {
        if (stateInfo.IsName("Run")) currentAnimationState = AnimationStates.run;
        else if (stateInfo.IsName("Idle")) currentAnimationState = AnimationStates.idle;
        else if (stateInfo.IsName("PrimaryAttack")) currentAnimationState = AnimationStates.primaryAttack;
        else if (stateInfo.IsName("SecondaryAttack")) currentAnimationState = AnimationStates.secondaryAttack;
        else if (stateInfo.IsName("Death")) currentAnimationState = AnimationStates.death;
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
        animator.ResetTrigger("Possession"); // handled by subclasses if needed
    }

    /// <summary>
    /// Checks if the character is not currently in a primary attack animation.
    /// </summary>
    public bool NotInPrimary()
    {
        return currentAnimationState != AnimationStates.primaryAttack;
    }

    /// <summary>
    /// Returns the animation state the character is currently in
    /// </summary>
    /// <returns>Current animation state </returns>
    public AnimationStates GetCurrentState()
    {
        return currentAnimationState;
    }

    /// <summary>
    /// Waits for a delay corresponding to the current animation state.
    /// </summary>
    public IEnumerator WaitForDelay(AnimationStates animation)
    {
        switch (animation)
        {
            case AnimationStates.primaryAttack:
                yield return new WaitForSeconds(primaryAnimationDelay);
                break;
            case AnimationStates.secondaryAttack:
                yield return new WaitForSeconds(secondaryAnimationDelay);
                break;
        }
    }

    /// <summary>
    /// Waits for the end of an animation before allowing new state changes.
    /// </summary>
    protected IEnumerator WaitForEndAnimation(float sec)
    {
        yield return new WaitForSeconds(sec);
        canChange = true;
    }
}
