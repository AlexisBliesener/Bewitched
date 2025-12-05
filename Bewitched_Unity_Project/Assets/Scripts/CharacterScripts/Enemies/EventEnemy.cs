using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// This is the event enemy for the event system
[RequireComponent(typeof(Enemy))]
public class EventEnemy : MonoBehaviour
{
    [Tooltip("The enemy that is controlled by this event enemy")]
    private Enemy enemyForEvent;
    /// <summary>
    /// The enum for the enemy state
    /// </summary>
    public enum EventEnemyState
    {
        Attacking,
        Dizzy,
        Possessed,
    }
    [Tooltip("The current state of the enemy")]
    private EventEnemyState enemyState = EventEnemyState.Attacking;
    private void Start()
    {
        enemyForEvent = GetComponent<Enemy>();
        if (enemyForEvent == null)
        {
            Debug.LogWarning("Enemy not found on event enemy");
        }
    }
    /// <summary>
    /// Sets the state of the enemy
    /// </summary>
    public void SetState(EventEnemyState state)
    {
        enemyState = state;
        if (enemyForEvent == null)
        {
            Debug.LogWarning("Enemy not found on event enemy");
            return;
        }
        switch (state)
        {
            case EventEnemyState.Attacking:
                enemyForEvent.TransitionToState(Enemy.AIMovementState.Chasing);
                break;
            case EventEnemyState.Dizzy:;
                enemyForEvent.TransitionToState(Enemy.AIMovementState.Blocked);
                break;
            case EventEnemyState.Possessed:
                break;
        }
    }
    /// <summary>
    /// Gets the state of the enemy
    /// </summary>
    public EventEnemyState GetState()
    {
        return enemyState;
    }
    /// <summary>
    /// Gets the enemy controlled by this event enemy
    /// </summary>
    public Enemy GetEnemy()
    {
        if (enemyForEvent == null)
        {
            enemyForEvent = GetComponent<Enemy>();
        }
        return enemyForEvent;
    }

    

}
