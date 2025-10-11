using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// This is the event enemy for the event system
public class EventEnemy1 : Enemy
{
    /// <summary>
    /// The enum for the enemy state
    /// </summary>
    public enum EventEnemyState1
    {
        Attacking,
        Dizzy,
        Possessed,
    }
    [Tooltip("The current state of the enemy")]
    private EventEnemyState1 enemyState = EventEnemyState1.Attacking;
    /// <summary>
    /// Sets the state of the enemy
    /// </summary>
    public void SetState(EventEnemyState1 state)
    {
        enemyState = state;
    }
    /// <summary>
    /// Gets the state of the enemy
    /// </summary>
    public EventEnemyState1 GetState()
    {
        return enemyState;
    }

    public override void FindPath()
    {
        base.FindPath();
        pathState = PathState.Set;
    }
    /// <summary>
    /// Override of Enemy.Die to change the level music to the outro
    /// </summary>
    public override void Die()
    {
        AudioManager.ChangeMusicParameter("End", "True");
        base.Die();
    }
}
