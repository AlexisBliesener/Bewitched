using NaughtyAttributes;
using UnityEngine;
using UnityEngine.TextCore.Text;

/// <summary>
/// Specialized animator controller for the Eleth character.
/// Extends CharacterAnimator by adding the "possession" animation state.
/// </summary>
public class ElethAnimator : CharacterAnimator
{
    [SerializeField, Tooltip("Possession attack animation speed multiplier."), Range(0.1f, 10f)]
    private float possessionSpeedMult = 1f;
    [Header("Eleth Settings")]
    [SerializeField, Tooltip("Possession attack animation length.")]
    private float possessionAttackLength = 1f;


    /// <summary>
    /// Returns the speed multiplier of eleths possession animation
    /// </summary>
    /// <returns>possession animation speed multiplier</returns>
    public float GetPossessionSpeedMult()
    {
        return possessionSpeedMult;
    }

    protected override void Awake()
    {
        base.Awake();

        animationStates.Add("Possession");
        animationStates.Remove("PrimaryAttack");
        animationStates.Remove("SecondaryAttack");
        animationStates.Add("Sprint");

        animator.SetFloat("PossessionSpeedMult", possessionSpeedMult);
    }

    /// <summary>
    /// Turns on the sprint animation from the run animation
    /// </summary>
    public void ToggleSprint()
    {
        if(currentAnimationState == "Run")
        {
            currentAnimationState = "Sprint";
            animator.SetTrigger("Sprint");
        }
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

        if (currentAnimationState == "Sprint" && newState == "Run") return;

        if (currentAnimationState == "Possession")
        {
            PlayerController.instance.SetAllowMovement(true);
        }
        else if (newState == "Possession")
        {
            PlayerController.instance.SetAllowMovement(false);
        }
        else if (newState == "Death")
        {
            PlayerController.instance.SetAllowMovement(false);
        }


        currentAnimationState = newState;
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
            case "Death":
                animator.SetFloat("DeathSpeedMult", deathSpeedMult);
                animator.SetTrigger("Death");
                canChange = false;
                break;
            case "Possession":
                animator.SetFloat("PossessionSpeedMult", possessionSpeedMult);
                animator.SetTrigger("Possession");
                canChange = false;
                StartCoroutine(WaitForEndAnimation(possessionAttackLength));
                break;
        }
    }

    protected override void Update()
    {
        // prevent walk and run animations from clipping into walls
        if(currentAnimationState == "Sprint")
        {
            character.GetCharacterController().center = new Vector3(0,0.4f,0.8f);
        }
        else if(currentAnimationState == "Run")
        {
            character.GetCharacterController().center = new Vector3(0, 0.4f, 0.2f);
        }
        else
        {
            character.GetCharacterController().center = new Vector3(0, 0.4f, 0f);
        }

        if (animator == null)
        {
            Debug.LogWarning($"[{nameof(CharacterAnimator)}] No animator assigned on {gameObject.name}");
            return;
        }

        // Idle/run switching
        if (PlayerController.instance.movementInput.magnitude < 0.1f)
        {
            SwitchState("Idle");
        }
        else
        {
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
