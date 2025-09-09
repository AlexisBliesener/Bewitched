using System.Collections;
using UnityEngine;

/// <summary>
/// Controls character animations, including state management and delays for abilities.
/// </summary>
public class CharacterAnimator : MonoBehaviour
{
    [Tooltip("Animator component responsible for handling character animations.")]
    protected Animator animator;

    [Tooltip("Time delay before completing the primary ability animation.")]
    public float primaryAnimationDelay = 0.5f;

    [Tooltip("Time delay before completing the secondary ability animation.")]
    public float secondaryAnimationDelay;

    [Tooltip("The character controller attached to this gameobject")]
    private CharacterController characterController;

    [Tooltip("Defines the possible animation states for the character.")]
    public enum AnimationStates { idle, primaryAttack, secondaryAttack, run, death, possession };

    [Tooltip("The current animation state of the character.")]
    protected AnimationStates currentAnimationState = AnimationStates.idle;

    /// <summary>
    /// Returns the current animation state of the character.
    /// </summary>
    public AnimationStates GetCurrentState()
    {
        return currentAnimationState;
    }

    /// <summary>
    /// Checks if the character is not currently in a primary attack animation.
    /// </summary>
    public bool NotInPrimary()
    {
        return currentAnimationState != AnimationStates.primaryAttack;
    }

    protected void Start()
    {
        animator = GetComponentInChildren<Animator>();
        characterController = GetComponent<CharacterController>();

    }

    protected void Update()
    {
        if (characterController != null && characterController.velocity == Vector3.zero)
        {
            SwitchState(AnimationStates.idle);
        }
        else if(characterController != null)
        {
            SwitchState(AnimationStates.run);
        }
    }

    /// <summary>
    /// Switches the character's animation state and updates the Animator accordingly.
    /// </summary>
    /// <param name="newState">The new animation state to transition to.</param>
    public virtual void SwitchState(AnimationStates newState)
    {
        if (currentAnimationState == AnimationStates.death) return;
        if (currentAnimationState == newState) return;

        if(animator == null)
        {
            Debug.LogWarning("There is no animator assigned to this character");
            return;
        }

        currentAnimationState = newState;

        switch (currentAnimationState)
        {
            case AnimationStates.run:
                animator.SetTrigger("Run");
                break;
            case AnimationStates.idle:
                animator.SetTrigger("Idle");
                break;
            case AnimationStates.primaryAttack:
                animator.SetTrigger("PrimaryAttack");
                break;
            case AnimationStates.secondaryAttack:
                animator.SetTrigger("SecondaryAttack");
                break;
            case AnimationStates.death:
                animator.SetTrigger("Death");
                break;
        }
    }

    /// <summary>
    /// Waits for a delay corresponding to the given animation before continuing execution.
    /// </summary>
    /// <param name="animation">The animation state to wait for.</param>
    public IEnumerator WaitForDelay(AnimationStates animation)
    {
        switch (currentAnimationState)
        {
            case AnimationStates.run:
                break;
            case AnimationStates.primaryAttack:
                yield return new WaitForSeconds(primaryAnimationDelay);
                break;
            case AnimationStates.secondaryAttack:
                yield return new WaitForSeconds(secondaryAnimationDelay);
                break;
            case AnimationStates.death:
                break;
        }
    }
}
