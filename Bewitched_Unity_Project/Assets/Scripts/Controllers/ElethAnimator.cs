using System.Collections;
using UnityEngine;

/// <summary>
/// Specialized animator controller for the Eleth character.
/// Extends CharacterAnimator by adding the "possession" animation state.
/// </summary>
public class ElethAnimator : CharacterAnimator
{
    [Header("Eleth Settings")]
    [SerializeField, Tooltip("Possession attack animation length.")]
    private float possessionAttackLength = 1f;

    public override void SwitchState(AnimationStates newState)
    {
        if (!canChange || currentAnimationState == AnimationStates.death || currentAnimationState == newState)
            return;

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
            case AnimationStates.death:
                animator.SetTrigger("Death");
                canChange = false;
                break;
            case AnimationStates.possession:
                animator.SetTrigger("Possession");
                canChange = false;
                StartCoroutine(WaitForEndAnimation(possessionAttackLength));
                break;
        }
    }

    protected override void Update()
    {
        if (animator == null) return;

        stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (stateInfo.IsName("Possession"))
            currentAnimationState = AnimationStates.possession;

        base.Update();
    }

    protected override void ResetAllTriggers()
    {
        base.ResetAllTriggers();
        animator.ResetTrigger("Possession");
    }

    private void OnEnable()
    {
        canChange = true;
    }
}
