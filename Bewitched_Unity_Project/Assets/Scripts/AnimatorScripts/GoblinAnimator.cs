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


