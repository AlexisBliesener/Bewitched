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

    /// <summary>
    /// Resets all animator triggers
    /// </summary>
    protected override void ResetAllTriggers()
    {
        base.ResetAllTriggers();
        animator.ResetTrigger("ExitSecondaryAttack");
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
    /// Set the trigger to enter the leap animation
    /// </summary>
    public void SetEnterLeap()
    {
        animator.SetTrigger("EnterLeap");
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
        if (GetComponentInParent<Goblin>().IsPlayerControlling())
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
        if (GetComponentInParent<Goblin>().IsPlayerControlling())
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
        if(GetComponentInParent<Goblin>().IsPlayerControlling())
            return primaryComboSpeedMultPlayer[comboStep];
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
        if (GetComponentInParent<Goblin>().IsPlayerControlling())
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
            if (currentAnimationState == "PrimaryAttack" && currentPrimaryComboStep != -1 && Time.time - timeLastPrimary >= primaryComboResetTime[currentPrimaryComboStep] / primaryComboSpeedMultPlayer[currentPrimaryComboStep])
            {
                character.ResetPrimaryComboStep();
            }
        }

        animator.SetInteger("PrimaryCombo", currentPrimaryComboStep);

        SwitchState(newState);
    }

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
        base.ResetAllTriggers();
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
            if (GetComponentInParent<Goblin>().IsPlayerControlling())
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
                canChange = true;
                break;
            case "Run":
                animator.SetFloat("WalkSpeedMult", walkSpeedMult);
                animator.SetTrigger("Run");
                canChange = true;
                break;
            case "PrimaryAttack":
                if (GetComponentInParent<Goblin>().IsPlayerControlling())
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
                if (GetComponentInParent<Goblin>().IsPlayerControlling())
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
                if (GetComponentInParent<Goblin>().IsPlayerControlling())
                    yield return new WaitForSeconds(primaryAnimationDelay[comboNum] / primaryComboSpeedMultPlayer[comboNum]);
                else
                    yield return new WaitForSeconds(primaryAnimationDelay[0] / primaryComboSpeedMultEnemy[0]);
                break;
            case "SecondaryAttack":
                if (GetComponentInParent<Goblin>().IsPlayerControlling())
                    yield return new WaitForSeconds(secondaryAnimationDelay / secondaryWindupSpeedMultPlayer);
                else
                    yield return new WaitForSeconds(secondaryAnimationDelay / secondaryWindupSpeedMultEnemy);

                break;
        }
    }

}


