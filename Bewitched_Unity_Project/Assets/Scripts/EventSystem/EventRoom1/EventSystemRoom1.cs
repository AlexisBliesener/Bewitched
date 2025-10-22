using UnityEngine;
using UnityEngine.Playables;
/// This is the event system room for the event system, it will handle the fight between the player and the event enemy
public class EventSystemRoom1 : MonoBehaviour
{
    [SerializeField, Tooltip("The enemy event prefab")]
    private EventEnemy enemyEvent;
    [SerializeField, Tooltip("The enemy spawner prefab")]
    private EnemySpawner enemySpawner;
    [SerializeField, Tooltip("The cut scene prefab")]
    private GameObject cutScene;
    // to check when the cut scene is finished
    [SerializeField, Tooltip("The director for the cut scene")]
    private PlayableDirector director;

    [SerializeField, Tooltip("The HUD prefab to disable it when the cut scene is active")]  
    private GameObject hud;

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
    private float damageToPossess = 100f;

    [SerializeField, Tooltip("The duration of the event enemy to get from dizzy to fighting if not possessed (in seconds)")]
    private float dizzyDuration = 5f;
    [Tooltip("The time when the event enemy started to get dizzy")]
    private float timeDizzyStarted = 0f;

    [SerializeField, Tooltip("The amount of health to add to the event enemy when it is not possessed during the dizzy period")]
    private float healthToAdd = 50f;

    [SerializeField, Tooltip("The door to open when the event enemy is possessed")]
    private IDoor door;


    private void Start()
    {
        if (enemyEvent != null)
        {
            enemyEvent.GetEnemy().gameObject.SetActive(false);
        }
        if (hud == null)
        {
            Debug.LogWarning("HUD is null on event system room 1");
        }
    }
    /// <summary>
    /// Handles when the event enemy is triggered by the player to activate the fight
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == PlayerController.instance.currentCharacter.gameObject && fightState == FightState.Waiting)
        {
            enemyEvent.GetEnemy().gameObject.SetActive(true);
            enemyEvent.GetEnemy().aiState = Enemy.AIMovementState.Blocked;
            StartCutScene();
        }
    }
    [ContextMenu("Start Cut Scene")]
    private void StartCutScene()
    {
        PlayerController.instance.SetAllowMovement(false);
        // Start the cut scene, if it's already active, it will be stopped and then started again
        if (cutScene != null)
        {
            if (cutScene.activeInHierarchy)
            {
                cutScene.SetActive(false);
            }

            cutScene.SetActive(true);
        }
        
        director = cutScene.GetComponent<PlayableDirector>();
        if (hud != null) {hud.SetActive(false);}
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
                if ((enemyEvent.GetEnemy().health.GetMaxHealth() - enemyEvent.GetEnemy().health.GetHealth()) >= damageToPossess)
                {
                    // change the state to be able to possess the enemy
                    fightState = FightState.Ending;
                    timeDizzyStarted = Time.time;
                    enemyEvent.SetState(EventEnemy.EventEnemyState.Dizzy);
                }
                break;
            case FightState.Ending:
                if (enemyEvent.GetEnemy().gameObject == PlayerController.instance.currentCharacter.gameObject)
                {
                    // this mean the player has possessed the enemy, change the state to finished for the fight
                    EndFight();
                    return;
                }
                if (((enemyEvent.GetEnemy().health.GetMaxHealth() - enemyEvent.GetEnemy().health.GetHealth()) >= damageToPossess)
                       && (Time.time - timeDizzyStarted <= dizzyDuration))
                {
                    // Make the enemy able to be possessed if it the dizzy duration has not passed 
                    enemyEvent.GetEnemy().canPossess = true;
                    return;
                }
                // if it passes the dizzy duration, make the enemy not possessable, and add health to the enemy event
                // and make the enemy to be able to attack again
                enemyEvent.GetEnemy().canPossess = false;
                enemyEvent.GetEnemy().health.AddHealth(healthToAdd);
                fightState = FightState.Fighting;
                enemyEvent.SetState(EventEnemy.EventEnemyState.Attacking);
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
        if(AudioManager.manager != null)
        {
            AudioManager.ChangeMusicParameter("End", "True");
        }
        else
        {
            Debug.LogWarning("Audio Manager instance is not set!");
        }
            
        fightState = FightState.Finished;
        enemyEvent.SetState(EventEnemy.EventEnemyState.Possessed);
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
        enemyEvent.GetEnemy().health.SubHealth(100f);
    }

    [ContextMenu("Possess")]
    /// <summary>
    /// Possess the event enemy (THIS IS FOR DEBUGGING PURPOSES)
    /// </summary>
    private void Possess()
    {
        Character currentPossessableEnemy = enemyEvent.GetEnemy().GetComponent<Character>();
        PossessionAbility.CharacterControlChangeEvent?.Invoke(currentPossessableEnemy);
        currentPossessableEnemy.SetControlled(true);
    }
    /// <summary>
    /// Subscribe to the cut scene director stopped event
    /// </summary>
    void OnEnable()
    {
        if (director != null)
            director.stopped += OnCutsceneFinished;
    }
    /// <summary>
    /// Unsubscribe from the cut scene director stopped event
    /// </summary>
    void OnDisable()
    {
        if (director != null)
            director.stopped -= OnCutsceneFinished;
    }
    /// <summary>
    /// This is called when the cut scene is finished it will start the fight!!
    /// </summary>
    private void OnCutsceneFinished(PlayableDirector director)
    {
        PlayerController.instance.SetAllowMovement(true);
        cutScene.SetActive(false);
        fightState = FightState.Fighting;
        enemyEvent.GetEnemy().canPossess = false;
        enemyEvent.GetEnemy().aiState = Enemy.AIMovementState.Patrolling;
        // Activate the enemy spawner
        enemySpawner.Activate();
        // show all the HUD
        if (hud != null) { hud.SetActive(true); }
    }
}


