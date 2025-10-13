using System.Collections;
using UnityEngine;

/// <summary>
/// Specialized animator controller for the Goblin character.
/// Extends CharacterAnimator.
/// </summary>
public class GoblinAnimator : CharacterAnimator
{
    /// <summary>
    /// Resets all animator triggers
    /// </summary>
    protected override void ResetAllTriggers()
    {
        base.ResetAllTriggers();
        animator.ResetTrigger("ExitSecondaryAttack");
    }

    public override void SetSecondaryAttackEnded()
    {
        base.SetSecondaryAttackEnded();
        animator.SetTrigger("ExitSecondaryAttack");
    }
}


