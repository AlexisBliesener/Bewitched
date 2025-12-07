using UnityEngine;
using FMODUnity;
using FMOD.Studio;
[RequireComponent(typeof(EventEnemy))]
public class EventDryad : Dryad
{
    [Tooltip("If true, the dryad will die for real when Die is called")]
    public bool killDryad = false;
    public override void Die()
    {
        // since we don't want the dryad to die, we will just ovveride the die function and make it do nothing, mayve for animation later? 
        // TransitionToState(AIMovementState.Blocked);
        dead = true;
        dryadAnimator.TempDeath();
        stunned = false;
        GetCharacterController().enabled = false;
        if (killDryad)
        {
            base.Die();
        }
    }

    /// <summary>
    /// Call when the dryad is revived
    /// Takes the dryad out of the temporary death animamtion
    /// </summary>
    public void Revive()
    {
        dead = false;
        GetCharacterController().enabled = true;
        dryadAnimator.Revive();
    }

    public override void DoHitSoundEffect(float damage)
    {
        return;
    }
}