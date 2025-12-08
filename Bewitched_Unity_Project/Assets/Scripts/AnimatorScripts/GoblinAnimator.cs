using System.Collections;
using UnityEngine;

/// <summary>
/// Specialized animator controller for the Goblin character.
/// Extends CharacterAnimator.
/// </summary>
public class GoblinAnimator : CharacterAnimator
{
    [SerializeField, Tooltip("Primary windup animation speed multiplier for the player."), Range(0.1f, 10f)]
    protected float primaryWindupSpeedMultPlayer = 2f;
    [SerializeField, Tooltip("Primary windup animation speed multiplier for enemies."), Range(0.1f, 10f)]
    protected float primaryWindupSpeedMultEnemy = 0.7f;
    [SerializeField, Tooltip("Primary attack animation speed multiplier for the player."), Range(0.1f, 10f)]
    protected float[] primaryComboSpeedMultPlayer = { 1, 1, 1 };
    [SerializeField, Tooltip("Primary attack animation speed multiplier for enemies."), Range(0.1f, 10f)]
    protected float[] primaryComboSpeedMultEnemy = { 0.7f, 0.7f, 0.7f };
    [SerializeField, Tooltip("Secondary windup animation speed multiplier for the player."), Range(0.1f, 10f)]
    protected float secondaryWindupSpeedMultPlayer = 1f;
    [SerializeField, Tooltip("Secondary windup animation speed multiplier for enemies."), Range(0.1f, 10f)]
    protected float secondaryWindupSpeedMultEnemy = 1f;
    [SerializeField, Tooltip("Dizzy time after secondary attack player")]
    private float dizzyTimePlayer;
    [SerializeField, Tooltip("Dizzy time after secondary attack enemy")]
    private float dizzyTimeEnemy;
    [Tooltip("The goblin script for this ogre animator")]
    private Goblin goblinScript;

    private void Start()
    {
        goblinScript = GetComponentInParent<Goblin>();
    }

    protected override void Awake()
    {
        base.Awake();

        animationStates.Add("Jump");
    }

    /// <summary>
    /// Resets all animator triggers
    /// </summary>
    protected override void ResetAllTriggers()
    {
        base.ResetAllTriggers();
        animator.ResetTrigger("ExitSecondaryAttack");
        animator.ResetTrigger("Jump");
    }

    /// <summary>
    /// Returns true if the goblin is in the leap animation
    /// </summary>
    /// <returns>True if the goblin is leaping</returns>
    public bool GetInLeap()
    {
        AnimatorClipInfo[] clip = animator.GetCurrentAnimatorClipInfo(0);
        if(clip[0].clip.name == ("GoblinLeap"))
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets if the goblin is currently in the primary windup animation
    /// </summary>
    /// <returns></returns>
    public bool GetInPrimaryWindup()
    {
        AnimatorClipInfo[] clip = animator.GetCurrentAnimatorClipInfo(0);
        if (clip[0].clip.name == ("GoblinPrimaryWindup"))
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Returns the primary attack windup speed multiplier
    /// </summary>
    /// <returns>primary attack windup speed multiplier</returns>
    public float GetPrimaryWindupMult()
    {
        if (goblinScript.IsPlayerControlling())
            return primaryWindupSpeedMultPlayer;
        else
            return primaryWindupSpeedMultEnemy;
    }

    /// <summary>
    /// Returns the secondary attack windup speed multiplier
    /// </summary>
    /// <returns>secondary attack windup speed multiplier</returns>
    public float GetSecondaryWindupMult()
    {
        if (goblinScript.IsPlayerControlling())
            return secondaryWindupSpeedMultPlayer;
        else
            return secondaryWindupSpeedMultEnemy;
    }

    /// <summary>
    /// Returns the primary attack speed multipler for the correct combo step
    /// </summary>
    /// <param name="comboStep">The step in the combo to get the multiplier for</param>
    /// <returns>primary attack speed multipler</returns>
    public float GetPrimaryComboMult(int comboStep)
    {
        if (goblinScript.IsPlayerControlling())
        {
            comboStep = Mathf.Clamp(comboStep, 0, primaryComboSpeedMultPlayer.Length-1);
            return primaryComboSpeedMultPlayer[comboStep];
        }
        else
            return primaryComboSpeedMultEnemy[0];
    }

    /// <summary>
    /// Sets the secondary attack as ended
    /// Starts dizzy phase
    /// </summary>
    public override void SetSecondaryAttackEnded()
    {
        animator.SetTrigger("ExitSecondaryAttack");
        StartCoroutine(WaitForDizzy());
    }

    /// <summary>
    /// Waits for the dizzy phase to end
    /// </summary>
    private IEnumerator WaitForDizzy()
    {
        if (goblinScript.IsPlayerControlling())
            yield return new WaitForSeconds(dizzyTimePlayer);
        else
            yield return new WaitForSeconds(dizzyTimeEnemy);
        canChange = true;
    }

    /// <summary>
    /// Switches the character's animation state and updates the Animator accordingly.
    /// </summary>
    public override void SwitchState(string newState, int currentPrimaryComboStep, float timeLastPrimary, float[] primaryComboResetTime)
    {
        if(PlayerController.instance.currentCharacter == character)
        {
            if ( currentPrimaryComboStep != -1 && Time.time - timeLastPrimary >= primaryComboResetTime[currentPrimaryComboStep] / primaryComboSpeedMultPlayer[currentPrimaryComboStep])
            {
                goblinScript.SetMovementValues(true);
                character.ResetPrimaryComboStep();
            }
        }

        animator.SetInteger("PrimaryCombo", currentPrimaryComboStep);

        if (newState == "Idle")
        {
            legsRunning = false;
            animator.ResetTrigger("LegsRun");
            animator.SetTrigger("LegsIdle");
        }
        else if (newState == "Run")
        {
            legsRunning = true;
            animator.ResetTrigger("LegsIdle");
            animator.SetTrigger("LegsRun");
        }

        SwitchState(newState);
    }

    protected override void Update()
    {
        base.Update();

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
    /// Called when the goblin ends its primary attack
    /// Allows the animator to change states
    /// </summary>
    public void EndPrimary()
    {
        canChange = true;
    }

    /// <summary>
    /// Exits the leap portion of the primary attack windup 
    /// Moves into hit animation
    /// </summary>
    public void ExitLeap()
    {
        ResetAllTriggers();
        animator.SetTrigger("ExitLeap");
    }

    /// <summary>
    /// Switches the character's animation state and updates the Animator accordingly.
    /// </summary>
    public override void SwitchState(string newState)
    {
        if (!animationStates.Contains(newState))
        {
            Debug.LogWarning("This animation state: " + newState + " does not exist!");
        }

        if (newState == "PrimaryAttack")
        {
            ResetAllTriggers();
            animator.SetTrigger("PrimaryAttack");
            if (goblinScript.IsPlayerControlling())
            {
                animator.SetFloat("PrimaryComboOneSpeedMult", primaryComboSpeedMultPlayer[0]);
                animator.SetFloat("PrimaryComboTwoSpeedMult", primaryComboSpeedMultPlayer[1]);
                animator.SetFloat("PrimaryComboThreeSpeedMult", primaryComboSpeedMultPlayer[2]);
                animator.SetFloat("PrimaryWindupSpeedMult", primaryWindupSpeedMultPlayer);
            }
            else
            {
                animator.SetFloat("PrimaryComboOneSpeedMult", primaryComboSpeedMultEnemy[0]);
                animator.SetFloat("PrimaryComboTwoSpeedMult", primaryComboSpeedMultEnemy[1]);
                animator.SetFloat("PrimaryComboThreeSpeedMult", primaryComboSpeedMultEnemy[2]);
                animator.SetFloat("PrimaryWindupSpeedMult", primaryWindupSpeedMultEnemy);
            }
            canChange = false;
            currentAnimationState = newState;
            return;
        }
        else if (newState == "Death")
        {
            ResetAllTriggers();
            animator.SetTrigger("Death");
            canChange = false;
            currentAnimationState = newState;
        }

        if (!canChange || currentAnimationState == "Death" || currentAnimationState == newState)
            return;

        currentAnimationState = newState;

        if (animator == null) return;

        ResetAllTriggers();

        switch (newState)
        {
            case "Idle":
                animator.SetFloat("IdleSpeedMult", idleSpeedMult);
                animator.SetTrigger("Idle");
                animator.SetTrigger("ExitLeap");
                animator.SetTrigger("ExitSecondaryAttack");
                canChange = true;
                break;
            case "Run":
                animator.SetFloat("WalkSpeedMult", walkSpeedMult);
                animator.SetTrigger("Run");
                animator.SetTrigger("ExitLeap");
                animator.SetTrigger("ExitSecondaryAttack");
                canChange = true;
                break;
            case "PrimaryAttack":
                if (goblinScript.IsPlayerControlling())
                {
                    animator.SetFloat("PrimaryComboOneSpeedMult", primaryComboSpeedMultPlayer[0]);
                    animator.SetFloat("PrimaryComboTwoSpeedMult", primaryComboSpeedMultPlayer[1]);
                    animator.SetFloat("PrimaryComboThreeSpeedMult", primaryComboSpeedMultPlayer[2]);
                    animator.SetFloat("PrimaryWindupSpeedMult", primaryWindupSpeedMultPlayer);
                }
                else
                {
                    animator.SetFloat("PrimaryComboOneSpeedMult", primaryComboSpeedMultEnemy[0]);
                    animator.SetFloat("PrimaryComboTwoSpeedMult", primaryComboSpeedMultEnemy[1]);
                    animator.SetFloat("PrimaryComboThreeSpeedMult", primaryComboSpeedMultEnemy[2]);
                    animator.SetFloat("PrimaryWindupSpeedMult", primaryWindupSpeedMultEnemy);
                }
                animator.SetTrigger("PrimaryAttack");
                canChange = false;
                break;
            case "SecondaryAttack":
                if (goblinScript.IsPlayerControlling())
                {
                    animator.SetFloat("SecondaryWindupSpeedMult", secondaryWindupSpeedMultPlayer);
                }
                else
                {
                    animator.SetFloat("SecondaryWindupSpeedMult", secondaryWindupSpeedMultEnemy);
                }
               
                animator.SetTrigger("SecondaryAttack");
                canChange = false;
                break;
            case "Death":
                animator.SetFloat("DeathSpeedMult", deathSpeedMult);
                animator.SetTrigger("Death");
                canChange = false;
                break;
        }
    }

    public override IEnumerator WaitForDelay(string animation, int comboNum)
    {
        switch (animation)
        {
            case "PrimaryAttack":
                if (goblinScript.IsPlayerControlling())
                    yield return new WaitForSeconds(primaryAnimationDelay[comboNum] / primaryComboSpeedMultPlayer[comboNum]);
                else
                    yield return new WaitForSeconds(primaryAnimationDelay[0] / primaryComboSpeedMultEnemy[0]);
                break;
            case "SecondaryAttack":
                if (goblinScript.IsPlayerControlling())
                    yield return new WaitForSeconds(secondaryAnimationDelay / secondaryWindupSpeedMultPlayer);
                else
                    yield return new WaitForSeconds(secondaryAnimationDelay / secondaryWindupSpeedMultEnemy);

                break;
        }
    }

    /// <summary>
    /// Plays the jump animation holding the falling pose untill duration is over
    /// </summary>
    /// <param name="duration"></param>
    public IEnumerator Jump(float duration)
    {
        ResetAllTriggers();
        canChange = false;
        animator.SetTrigger("Jump");
        currentAnimationState = "Jump";
        yield return new WaitForSeconds(duration - 0.1f);
        animator.SetTrigger("JumpLanded");
        yield return new WaitForSeconds(0.667f);
        canChange = true;
    }

}


