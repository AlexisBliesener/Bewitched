using UnityEngine;
/// This is the event system room for the event system, it will handle the fight between the player and the event enemy
public class EventSystemRoom1 : MonoBehaviour
{
    [SerializeField, Tooltip("The enemy event prefab")]
    private EventEnemy1 enemyEvent;
    [SerializeField, Tooltip("The enemy spawner prefab")]
    private EnemySpawner enemySpawner;

    /// <summary>
    /// The enum for the fight state
    /// </summary>
    private enum FightState
    {
        Waiting,
        Fighting,
        Ending,
        Finished
    }
    [Tooltip("The current fight state")]
    private FightState fightState = FightState.Waiting;

    [SerializeField, Tooltip("Damage to activate the ability to possess the event enemy")]
    private float damageToPossess = 5000f;

    [SerializeField, Tooltip("The duration of the event enemy to get from dizzy to fighting if not possessed (in seconds)")]
    private float dizzyDuration = 5f;
    [Tooltip("The time when the event enemy started to get dizzy")]
    private float timeDizzyStarted = 0f;

    [SerializeField, Tooltip("The amount of health to add to the event enemy when it is possessed")]
    private float healthToAdd = 1666f;

    [SerializeField, Tooltip("The door to open when the event enemy is possessed")]
    private IDoor door;
    /// <summary>
    /// Handles when the event enemy is triggered by the player to activate the fight
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == PlayerController.instance.currentCharacter.gameObject && fightState == FightState.Waiting)
        {
            StartEvent();
        }
    }
    /// <summary>
    /// Starts the event, this is goign to start the cut scene for the event enemy and then start spawning enemies 
    /// </summary>
    private void StartEvent()
    {
        // Initialize the cut scene for the event enemy 

        // Activate the enemy spawner 

        enemySpawner.Activate();
        fightState = FightState.Fighting;
        enemyEvent.canPossess = false;

    }
    /// <summary>
    /// Handle the fight state changes
    /// </summary>
    private void Update()
    {
        if (enemyEvent == null) return;
        switch (fightState)
        {
            case FightState.Waiting:
                break;
            case FightState.Fighting:
                if ((enemyEvent.health.GetMaxHealth() - enemyEvent.health.GetHealth()) >= damageToPossess)
                {
                    // change the state to be able to possess the enemy
                    fightState = FightState.Ending;
                    timeDizzyStarted = Time.time;
                    enemyEvent.SetState(EventEnemy1.EventEnemyState1.Dizzy);
                }
                break;
            case FightState.Ending:
                if (enemyEvent.gameObject == PlayerController.instance.currentCharacter.gameObject)
                {
                    // this mean the player has possessed the enemy, change the state to finished for the fight
                    EndFight();
                    return;
                }
                if (((enemyEvent.health.GetMaxHealth() - enemyEvent.health.GetHealth()) >= damageToPossess)
                       && (Time.time - timeDizzyStarted <= dizzyDuration))
                {
                    // Make the enemy able to be possessed if it the dizzy duration has not passed 
                    enemyEvent.canPossess = true;
                    return;
                }
                // if it passes the dizzy duration, make the enemy not possessable, and add health to the enemy event
                // and make the enemy to be able to attack again
                enemyEvent.canPossess = false;
                enemyEvent.health.AddHealth(healthToAdd);
                fightState = FightState.Fighting;
                enemyEvent.SetState(EventEnemy1.EventEnemyState1.Attacking);
                break;
            case FightState.Finished:
                break;
        }
    }
    /// <summary>
    /// Ends the fight, this is going to change the state to defeated and unlock the door if it exists
    /// </summary>
    public void EndFight()
    {
        fightState = FightState.Finished;
        enemyEvent.SetState(EventEnemy1.EventEnemyState1.Possessed);
        if (door != null)
        {
            door.Unlock();
        }
    }
    [ContextMenu("Give damage")]
    /// <summary>
    /// Gives damage to the event enemy (THIS IS FOR DEBUGGING PURPOSES)
    /// </summary>
    private void GiveDamage()
    {
        enemyEvent.health.SubHealth(100f);
    }

    [ContextMenu("Possess")]
    /// <summary>
    /// Possess the event enemy (THIS IS FOR DEBUGGING PURPOSES)
    /// </summary>
    private void Possess()
    {
        Character currentPossessableEnemy = enemyEvent.GetComponent<Character>();
        PossessionAbility.CharacterControlChangeEvent?.Invoke(currentPossessableEnemy);
        currentPossessableEnemy.SetControlled(true);
    }
}
