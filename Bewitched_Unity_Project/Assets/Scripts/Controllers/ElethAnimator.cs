using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Specialized animator controller for the Eleth character.
/// Extends CharacterAnimator by adding the "possession" animation state
/// and customizing the switch logic.
/// </summary>
public class ElethAnimator : CharacterAnimator
{
    /// <summary>
    /// Switches the Eleth character's animation state and updates the Animator accordingly.
    /// Includes an additional case for the possession state.
    /// </summary>
    /// <param name="newState">The new animation state to transition to.</param>
    public new void SwitchState(AnimationStates newState)
    {
        if (currentAnimationState == AnimationStates.death) return;
        if (currentAnimationState == newState) return;

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
            case AnimationStates.possession:
                animator.SetTrigger("Possession");
                break;
        }
    }
}
