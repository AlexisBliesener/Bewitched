using UnityEngine;
[RequireComponent(typeof(EventEnemy))]
public class EventDryad : Dryad
{
    public override void Die()
    {
        // since we don't want the dryad to die, we will just ovveride the die function and make it do nothing, mayve for animation later? 
        // TransitionToState(AIMovementState.Blocked);
        dryadAnimator.TempDeath();
        stunned = false; 
    }

    /// <summary>
    /// Call when the dryad is revived
    /// Takes the dryad out of the temporary death animamtion
    /// </summary>
    public void Revive()
    {
        dryadAnimator.Revive();
    }
}