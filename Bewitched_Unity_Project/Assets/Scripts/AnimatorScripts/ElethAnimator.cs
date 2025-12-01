using UnityEngine;

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

    public override void SwitchState(string newState, int currentPrimaryComboStep, float timeLastPrimary, float[] primaryComboResetTime)
    {
       
    }

    /// <summary>
    /// Switches the character's animation state and updates the Animator accordingly.
    /// Specific to the eleth animator adds in the possession state
    /// </summary>
    public override void SwitchState(string newState)
    {

        if (newState == "Death")
        {
            ResetAllTriggers();
            animator.SetFloat("DeathSpeedMult", deathSpeedMult);
            animator.SetTrigger("Death");
            canChange = false;
            PlayerController.instance.SetAllowMovement(false);
            currentAnimationState = "Death";
            return;
        }

        if (overriding) return;

        if (!animationStates.Contains(newState))
        {
            Debug.LogWarning("This animation state: " + newState + " does not exist!");
        }

        if (!canChange || currentAnimationState == "Death")
            return;


        if (currentAnimationState == "Possession")
        {
            PlayerController.instance.SetAllowMovement(true);
        }
        else if (newState == "Possession")
        {
            PlayerController.instance.SetAllowMovement(false);
        }
        
        ResetAllTriggers();

        switch (newState)
        {
            case "Idle":
                currentAnimationState = newState;
                animator.SetFloat("IdleSpeedMult", idleSpeedMult);
                animator.SetTrigger("Idle");
                canChange = true;
                break;
            case "Run":
                if(PlayerController.instance.GetSprinting())
                {
                    currentAnimationState = "Sprint";
                    animator.SetBool("Sprint", true);
                }
                else
                {
                    currentAnimationState = newState;
                    animator.SetBool("Sprint", false);
                }
                animator.SetFloat("WalkSpeedMult", walkSpeedMult);
                animator.SetTrigger("Run");
                canChange = true;
                break;
            case "Possession":
                currentAnimationState = newState;
                animator.SetFloat("PossessionSpeedMult", possessionSpeedMult);
                animator.SetTrigger("Possession");
                canChange = false;
                StartCoroutine(WaitForEndAnimation(possessionAttackLength));
                break;
        }
    }

    protected override void Update()
    {
        if (overriding) return;

        // prevent walk and run animations from clipping into walls
        if (currentAnimationState == "Sprint")
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
            legsRunning = false;
            animator.ResetTrigger("LegsRun");
            animator.SetTrigger("LegsIdle");
        }
        else
        {
            SwitchState("Run");
            legsRunning = true;
            animator.ResetTrigger("LegsIdle");
            animator.SetTrigger("LegsRun");
        }

        if (legsRunning && (currentAnimationState == "Hit"))
        {
            legLayerWeight += Time.deltaTime * 5;
            if (legLayerWeight > 1) legLayerWeight = 1;
            animator.SetLayerWeight(1, legLayerWeight);
        }
        else
        {
            legLayerWeight -= Time.deltaTime * 5;
            if (legLayerWeight < 0) legLayerWeight = 0;
            animator.SetLayerWeight(1, legLayerWeight);
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
