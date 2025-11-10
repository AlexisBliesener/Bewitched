using System.Collections;
using UnityEngine;

/// <summary>
/// Specialized animator controller for the Goblin character.
/// Extends CharacterAnimator.
/// </summary>
public class OgreAnimator : CharacterAnimator
{
    [SerializeField, Tooltip("Primary windup animation speed multiplier for the player."), Range(0.1f, 10f)]
    protected float primaryWindupSpeedMultPlayer = 2f;
    [SerializeField, Tooltip("Primary windup animation speed multiplier for enemies."), Range(0.1f, 10f)]
    protected float primaryWindupSpeedMultEnemy = 0.7f;
    [SerializeField, Tooltip("Primary attack animation speed multiplier for the player."), Range(0.1f, 10f)]
    protected float[] primaryComboSpeedMultPlayer = { 1, 1 };
    [SerializeField, Tooltip("Primary attack animation speed multiplier for enemies."), Range(0.1f, 10f)]
    protected float[] primaryComboSpeedMultEnemy = { 0.7f, 0.7f };
    [SerializeField, Tooltip("Secondary windup animation speed multiplier for the player."), Range(0.1f, 10f)]
    protected float secondaryWindupSpeedMultPlayer = 1f;
    [SerializeField, Tooltip("Secondary windup animation speed multiplier for enemies."), Range(0.1f, 10f)]
    protected float secondaryWindupSpeedMultEnemy = 1f;
    [SerializeField, Tooltip("Secondary attack animation speed multiplier for the player."), Range(0.1f, 10f)]
    protected float secondaryAttackSpeedMultPlayer = 1f;
    [SerializeField, Tooltip("Secondary attack animation speed multiplier for enemies."), Range(0.1f, 10f)]
    protected float secondaryAttackSpeedMultEnemy = 1f;

    /// <summary>
    /// Resets all animator triggers
    /// </summary>
    protected override void ResetAllTriggers()
    {
        base.ResetAllTriggers();
        animator.ResetTrigger("Swing");
    }

    public void SetSwing()
    {
        ResetAllTriggers();
        animator.SetTrigger("Swing");
    }

    /// <summary>
    /// Returns the primary attack windup speed multiplier
    /// </summary>
    /// <returns>primary attack windup speed multiplier</returns>
    public float GetPrimaryWindupMult()
    {
        if (GetComponentInParent<Ogre>().IsPlayerControlling())
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
        if (GetComponentInParent<Ogre>().IsPlayerControlling())
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
        if (GetComponentInParent<Ogre>().IsPlayerControlling())
            return primaryComboSpeedMultPlayer[comboStep];
        else
            return primaryComboSpeedMultEnemy[0];
    }

    /// <summary>
    /// Switches the character's animation state and updates the Animator accordingly.
    /// </summary>
    public override void SwitchState(string newState, int currentPrimaryComboStep, float timeLastPrimary, float[] primaryComboResetTime)
    {
        if (PlayerController.instance.currentCharacter == character)
        {
            if ((currentAnimationState == "PrimaryAttack" && currentPrimaryComboStep != -1 && Time.time - timeLastPrimary >= primaryComboResetTime[currentPrimaryComboStep] / primaryComboSpeedMultPlayer[currentPrimaryComboStep]))
            {
                character.ResetPrimaryComboStep();
            }
        }

        animator.SetInteger("PrimaryCombo", currentPrimaryComboStep);

        SwitchState(newState);
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
            if (GetComponentInParent<Ogre>().IsPlayerControlling())
            {
                animator.SetFloat("PrimaryComboOneSpeedMult", primaryComboSpeedMultPlayer[0]);
                animator.SetFloat("PrimaryComboTwoSpeedMult", primaryComboSpeedMultPlayer[1]);
                animator.SetFloat("PrimaryWindupSpeedMult", primaryWindupSpeedMultPlayer);
            }
            else
            {
                animator.SetFloat("PrimaryComboOneSpeedMult", primaryComboSpeedMultEnemy[0]);
                animator.SetFloat("PrimaryComboTwoSpeedMult", primaryComboSpeedMultEnemy[1]);
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
                if (GetComponentInParent<Ogre>().IsPlayerControlling())
                {
                    animator.SetFloat("PrimaryComboOneSpeedMult", primaryComboSpeedMultPlayer[0]);
                    animator.SetFloat("PrimaryComboTwoSpeedMult", primaryComboSpeedMultPlayer[1]);
                    animator.SetFloat("PrimaryWindupSpeedMult", primaryWindupSpeedMultPlayer);
                }
                else
                {
                    animator.SetFloat("PrimaryComboOneSpeedMult", primaryComboSpeedMultEnemy[0]);
                    animator.SetFloat("PrimaryComboTwoSpeedMult", primaryComboSpeedMultEnemy[1]);
                    animator.SetFloat("PrimaryWindupSpeedMult", primaryWindupSpeedMultEnemy);
                }
                animator.SetTrigger("PrimaryAttack");
                canChange = false;
                break;
            case "SecondaryAttack":
                if (GetComponentInParent<Ogre>().IsPlayerControlling())
                {
                    animator.SetFloat("SecondaryWindupSpeedMult", secondaryWindupSpeedMultPlayer);
                    animator.SetFloat("SecondaryAttackSpeedMult", secondaryAttackSpeedMultPlayer);
                }
                else
                {
                    animator.SetFloat("SecondaryWindupSpeedMult", secondaryWindupSpeedMultEnemy);
                    animator.SetFloat("SecondaryAttackSpeedMult", secondaryAttackSpeedMultEnemy);
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
                if (GetComponentInParent<Ogre>().IsPlayerControlling())
                    yield return new WaitForSeconds(primaryAnimationDelay[comboNum] / primaryComboSpeedMultPlayer[comboNum]);
                else
                    yield return new WaitForSeconds(primaryAnimationDelay[0] / primaryComboSpeedMultEnemy[0]);
                break;
            case "SecondaryAttack":
                if (GetComponentInParent<Ogre>().IsPlayerControlling())
                    yield return new WaitForSeconds(secondaryAnimationDelay / secondaryWindupSpeedMultPlayer);
                else
                    yield return new WaitForSeconds(secondaryAnimationDelay / secondaryWindupSpeedMultEnemy);

                break;
        }
    }

}


