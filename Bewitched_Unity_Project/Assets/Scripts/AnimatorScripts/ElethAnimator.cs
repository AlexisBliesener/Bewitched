using UnityEngine;
using UnityEngine.TextCore.Text;

/// <summary>
/// Specialized animator controller for the Eleth character.
/// Extends CharacterAnimator by adding the "possession" animation state.
/// </summary>
public class ElethAnimator : CharacterAnimator
{
    [Header("Eleth Settings")]
    [SerializeField, Tooltip("Possession attack animation length.")]
    private float possessionAttackLength = 1f;

    private void Awake()
    {
        animationStates.Add("Possession");
        animationStates.Remove("PrimaryAttack");
        animationStates.Remove("SecondaryAttack");
    }

    /// <summary>
    /// Switches the character's animation state and updates the Animator accordingly.
    /// Specific to the eleth animator adds in the possession state
    /// </summary>
    public override void SwitchState(string newState)
    {
        if (!animationStates.Contains(newState))
        {
            Debug.LogWarning("This animation state: " + newState + " does not exist!");
        }

        if (!canChange || currentAnimationState == "Death" || currentAnimationState == newState)
            return;

        currentAnimationState = newState;
        ResetAllTriggers();

        switch (newState)
        {
            case "Idle":
                animator.SetTrigger("Idle");
                canChange = true;
                break;
            case "Run":
                animator.SetTrigger("Run");
                canChange = true;
                break;
            case "Death":
                animator.SetTrigger("Death");
                canChange = false;
                break;
            case "Possession":
                animator.SetTrigger("Possession");
                canChange = false;
                StartCoroutine(WaitForEndAnimation(possessionAttackLength));
                break;
        }
    }

    protected override void Update()
    {
        if (animator == null)
        {
            Debug.LogWarning($"[{nameof(CharacterAnimator)}] No animator assigned on {gameObject.name}");
            return;
        }

        // Idle/run switching
        if (characterController != null)
        {
            if (characterController.velocity.x == 0 && characterController.velocity.z == 0)
                SwitchState("Idle");
            else
                SwitchState("Run");
        }
    }


    /// <summary>
    /// Resets all animator triggers
    /// </summary>
    protected override void ResetAllTriggers()
    {
        base.ResetAllTriggers();
        animator.ResetTrigger("Possession");
    }
}
