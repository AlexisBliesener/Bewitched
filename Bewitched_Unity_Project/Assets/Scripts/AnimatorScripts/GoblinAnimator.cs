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

    /// <summary>
    /// Updates currentAnimationState based on the animator’s active state.
    /// </summary>
    protected override void UpdateCurrentStateFromAnimator()
    {
        if (stateInfo.IsName("Run")) currentAnimationState = "Run";
        else if (stateInfo.IsName("Idle")) currentAnimationState = "Idle";
        else if (stateInfo.IsName("PrimaryAttack")) currentAnimationState = "PrimaryAttack";
        else if (stateInfo.IsName("GoblinSecondaryStart")) currentAnimationState = "SecondaryAttack";
        else if (stateInfo.IsName("GoblinSecondaryLoop")) currentAnimationState = "SecondaryAttack";
        else if (stateInfo.IsName("GoblinSecondaryEnd")) currentAnimationState = "SecondaryAttack";
        else if (stateInfo.IsName("Jump")) currentAnimationState = "Jump";
        else if (stateInfo.IsName("Death")) currentAnimationState = "Death";
    }

    /// <summary>
    /// Waits for the end of an animation before allowing new state changes.
    /// </summary>
    protected override IEnumerator WaitForEndAnimation(float sec)
    {
        yield return new WaitForSeconds(sec);
        canChange = true;
        if(stateInfo.IsName("GoblinSecondaryLoop"))
        {
            animator.SetTrigger("ExitSecondaryAttack");
        }
    }
}


