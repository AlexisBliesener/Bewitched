using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Playables;
[RequireComponent(typeof(EventHealth))]
/// This is the event system room for the event system, it will handle the fight between the player and the event enemy
public class EventSystemRoom2 : MonoBehaviour
{
    [SerializeField, Tooltip("Are you a dev? [Don't check this if you're not a dev!!]")]
    private bool dev = false;

    [SerializeField, Tooltip("Click this box to skip the cutscene for testing"), ShowIf(nameof(dev))]
    private bool skipCutscene = false;

    [SerializeField, Tooltip("The enemy event prefab"), OnValueChanged(nameof(UpdateHealthPossessInfo))]
    private List<EventEnemy> enemiesEvent;

    [SerializeField, Tooltip("The enemy spawner prefab"), ShowIf(nameof(dev))]
    private EnemySpawner enemySpawner;

    [SerializeField, Tooltip("The cut scene prefab"), ShowIf(nameof(dev))]
    private GameObject cutScene;
    // to check when the cut scene is finished
    [SerializeField, Tooltip("The director for the cut scene"), ShowIf(nameof(dev))]
    private PlayableDirector director;
    [SerializeField, Tooltip("The duration of the event enemy to get from dizzy to fighting if not possessed (in seconds)")]
    private float dizzyDuration = 5f;
    [SerializeField,Tooltip("How many enemies revive when the dizzy duration ends without possession"),ValidateInput(nameof(ValidateRevivesOnFailedPossession),"Enemies to revive on dizzy end should be less than or equal to the number of enemies!")]
    private int revivesOnFailedPossession = 2;
    [Tooltip("The time when the event enemy started to get dizzy")]
    private float timeDizzyStarted = 0f;
    [SerializeField, Tooltip("The HUD prefab to disable it when the cut scene is active"), ShowIf(nameof(dev))]
    private GameObject hud;
    [SerializeField, Tooltip("The wall script"), ShowIf(nameof(dev))]
    private BreakWallMoment wall;
    [SerializeField, Tooltip("The total health of all event enemies"), OnValueChanged(nameof(UpdateHealthPossessInfo))]
    private float healthTotal = 0f;
    [SerializeField, Tooltip("The fraction of health for the last enemy to be able to be possessed"), Range(0f, 1f), OnValueChanged(nameof(UpdateHealthPossessInfo))]
    private float lastEnemyPossessionHealthFraction = 0.5f;
    [SerializeField, ReadOnly, Tooltip("This text is shown in the inspector to show how much health each enemy needs to be taken down"), TextArea(3, 10)]
    private string healthPossessInfo = "Each enemy goes down after taking 0 damage";
    [SerializeField, ReadOnly, Tooltip("This is the current health of the shared enemy")]
    private float sharedCurrentHealth;
    [SerializeField, Tooltip("The enemy health UI"), ShowIf(nameof(dev))]
    private EventHealth enemyHealthUI;
    [Tooltip("The last enemy that is still alive and can be possessed")]
    private EventEnemy lastStayingEnemy;
    /// <summary>
    /// The enum for the fight state
    /// </summary>
    private enum FightState
    {
        Waiting,
        Fighting, // This is when the cut scene is done and the event enemy is fighting
        Ending, // This is when the event enemy will be avaliable to possess, for a short time if not possessed, the state will change to fighting
        LastEnemies, // Wil spawn the last enemies (they will jump down from the stands)
        WaitingForCleanup, // This is when the player is killing all the goblins after they jump down from the stands. When this is done, the state will change to finished
        Finished
    }
    [Tooltip("The current fight state"), ReadOnly]
    private FightState fightState = FightState.Waiting;
    [SerializeField, Tooltip("The door to open when the event enemy is possessed")]
    private IDoor door;

    private void Start()
    {
        if (enemiesEvent != null)
        {
            foreach (EventEnemy enemy in enemiesEvent)
            {
                enemy.GetEnemy().gameObject.SetActive(false);
                enemy.GetEnemy().canPossess = false;
                enemy.GetEnemy().health.SetMaxHealth(healthTotal);
                enemy.GetEnemy().health.SetHealthToMax();
                enemy.GetEnemy().health.OnDamaged += HandleDamage;
                enemy.GetEnemy().aiState = (Enemy.AIMovementState.Blocked);
            }
        }

        if (hud == null)
        {
            Debug.LogWarning("HUD is null on event system room 1");
        }
        if (wall == null)
        {
            Debug.LogWarning("Wall script is null on event system room 1");
        }
        wall.enabled = false;
        sharedCurrentHealth = healthTotal;
        enemyHealthUI = GetComponentInChildren<EventHealth>();
        enemyHealthUI.SetMaxHealth(healthTotal);
    }
    /// <summary>
    /// Handles when the event enemy is triggered by the player to activate the fight
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == PlayerController.instance.currentCharacter.gameObject && fightState == FightState.Waiting)
        {
            foreach (EventEnemy enemy in enemiesEvent)
            {
                enemy.GetEnemy().gameObject.SetActive(true);
            }
            if (skipCutscene)
            {
                StartCoroutine(SkipCutscene());
            }
            else
            {
                StartCutScene();
            }
        }
    }
    [ContextMenu("Start Cut Scene")]
    private void StartCutScene()
    {
        enemySpawner.gameObject.SetActive(true);
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
        if (hud != null) { hud.SetActive(false); }
        //Change to combat music
        AudioManager.ChangeMusicParameter("InCombat", "True");
    }
    /// <summary>
    /// Handle the fight state changes
    /// </summary>
    private void Update()
    {
        if (enemiesEvent == null || enemiesEvent.Count == 0) return;
        switch (fightState)
        {
            case FightState.Waiting:
                break;
            case FightState.Fighting:

                // if (enemiesEvent.GetEnemy().health.GetHealth() <= healthToPossess)
                // {
                //     // change the state to be able to possess the enemy
                //     fightState = FightState.Ending;
                //     timeDizzyStarted = Time.time;
                //     enemyEvent.SetState(EventEnemy.EventEnemyState.Dizzy);
                //     // Make the health bar flashing on dizzy state
                //     enemyEvent.GetEnemy().health.GetComponent<EventHealth>().SetFlashing(true);
                // }
                        // Update UI health bar
                enemyHealthUI.SetCurrentHealth(sharedCurrentHealth);
                enemyHealthUI.SetFlashing(false);
                break;
            case FightState.Ending: // Ending = dizzy 
                if (lastStayingEnemy != null && lastStayingEnemy.GetEnemy().gameObject == PlayerController.instance.currentCharacter.gameObject)
                {
                    PossessionAbility.instance.SetCanLeavePossession(false);
                    // hide the health bar
                    enemyHealthUI.HideHealthBar();
                    // this mean the player has possessed the enemy, change the state to finished for the fight
                    EndFight();
                    return;
                }
                if (Time.time - timeDizzyStarted <= dizzyDuration && lastStayingEnemy != null)
                {
                    PossessionAbility.instance.SetPossessionOverride(lastStayingEnemy.GetEnemy());
                    enemyHealthUI.SetFlashing(true);
                    // Make the enemy able to be possessed if it the dizzy duration has not passed 
                    lastStayingEnemy.GetEnemy().canPossess = true;
                    lastStayingEnemy.GetEnemy().lobotimzed = true;
                    lastStayingEnemy.GetEnemy().aiState = (Enemy.AIMovementState.Blocked);
                    return;
                }
                // if it passes the dizzy duration, make the enemy not possessable, and revive one of the even enemies
                PossessionAbility.instance.SetPossessionOverride(null);
                if (lastStayingEnemy != null)
                {
                    ReviveEnemy(lastStayingEnemy); // reset the state of this enemy
                    List<EventEnemy> enemiesToRevive = new List<EventEnemy>(enemiesEvent);
                    // We don;t want to include the last enemy that is still alive in the list of enemies to revive
                    enemiesToRevive.Remove(lastStayingEnemy);
                    // -1 to not include the last enemy that is still alive in the list of enemies to revive
                    for (int i = 0; i < Mathf.Min(revivesOnFailedPossession - 1, enemiesToRevive.Count); i++)
                    {
                        ReviveEnemy(enemiesToRevive[i]);
                    }
                    // Restore health since we revived two enemies... 
                    float healthPerEnemy = (healthTotal / Mathf.Max(1, enemiesEvent.Count));
                    sharedCurrentHealth = healthPerEnemy * revivesOnFailedPossession;
                    lastStayingEnemy = null;

                    fightState = FightState.Fighting;
                }
                break;
            case FightState.LastEnemies:
                // Start making the enemies jump down
                StartCoroutine(enemySpawner.SpawnFinalEnemies());
                // Enable the wall script so the player can walk and break the wall
                fightState = FightState.WaitingForCleanup;
                break;
            case FightState.WaitingForCleanup:
                // we will check if all enemies are dead, if so, we will enable the wall script so the player can walk and break the wall
                // 1 as the event enemy is already included in the count
                if (RoomSystem.Instance.GetActiveRoomController().GetActiveEnemyCount() == 1)
                {
                    wall.enabled = true;
                    fightState = FightState.Finished;
                }
                break;
        }
    }
    /// <summary>
    /// Ends the fight, this is going to change the state to defeated and unlock the door if it exists
    /// </summary>
    public void EndFight()
    {
        fightState = FightState.LastEnemies;
        foreach (EventEnemy enemyEvent in enemiesEvent)
        {
            enemyEvent.SetState(EventEnemy.EventEnemyState.Possessed);
        }
        if (door != null)
        {
            door.Unlock();
        }

        // Kill all the enemies?
        foreach (EventEnemy enemyEvent in enemiesEvent)
        {
            if (enemyEvent.GetEnemy().gameObject == PlayerController.instance.currentCharacter.gameObject) continue; // Don't kill the player...
            enemyEvent.GetEnemy().health.KillEnemy();
            enemyEvent.GetEnemy().lobotimzed = true;
            enemyEvent.GetEnemy().gameObject.SetActive(false);
            Destroy(enemyEvent.GetEnemy().gameObject);
        }
    }
    [ContextMenu("Give damage")]
    /// <summary>
    /// Gives damage to the event enemy (THIS IS FOR DEBUGGING PURPOSES)
    /// </summary>
    private void GiveDamage()
    {
        EventEnemy enemyEvent = enemiesEvent[UnityEngine.Random.Range(0, enemiesEvent.Count)];
        enemyEvent.GetEnemy().health.SubHealth(100f, PlayerController.instance.oldHag);
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
        foreach (EventEnemy enemyEvent in enemiesEvent)
        {
            enemyEvent.GetEnemy().canPossess = false;
            enemyEvent.GetEnemy().aiState = (Enemy.AIMovementState.Patrolling);
        }
        // Activate the enemy spawner
        enemySpawner.Activate();
        // show all the HUD
        if (hud != null) { hud.SetActive(true); }
        enemyHealthUI.ShowHealthBar();
    }

    /// <summary>
    /// Skips the cutscene starting the boss fight immediately
    /// Used for faster debugging
    /// </summary>
    private IEnumerator SkipCutscene()
    {
        yield return new WaitForSeconds(0.5f);
        AudioManager.ChangeMusicParameter("InCombat", "True");
        enemySpawner.gameObject.SetActive(true);
        PlayerController.instance.SetAllowMovement(true);
        cutScene.SetActive(false);
        fightState = FightState.Fighting;
        foreach (EventEnemy enemy in enemiesEvent)
        {
            enemy.GetEnemy().canPossess = false;
            enemy.GetEnemy().aiState = (Enemy.AIMovementState.Patrolling);
        }
        // Activate the enemy spawner
        enemySpawner.Activate();
        // show all the HUD
        if (hud != null) { hud.SetActive(true); }
        enemyHealthUI.ShowHealthBar();
    }
    /// <summary>
    /// Handles the damage of the shared enemy
    /// </summary>
    /// <param name="damage"> Damage to take </param>
    private void HandleDamage(float damage, HealthController healthController)
    {
        sharedCurrentHealth = Mathf.Max(0f, sharedCurrentHealth - damage);
        int enemyShouldBeDead = Mathf.FloorToInt((healthTotal - sharedCurrentHealth) / (healthTotal / Mathf.Max(1, enemiesEvent.Count)));
        enemyShouldBeDead = Mathf.Min(enemyShouldBeDead, enemiesEvent.Count - 1); // Make sure at least one enemy is left alive... 
        int enemyDefeatedCount = GetEnemyDefeatedCount();
        // if it need to take down more enemies 
        if (enemyShouldBeDead > enemyDefeatedCount)
        {
            // if 
            TakeDownEnemy(healthController);
            enemyDefeatedCount += 1;
        }

        // check if we need to start the possession phase 
        if (enemyDefeatedCount >= Mathf.Max(1, enemiesEvent.Count) - 1)
        {
            float possessHealthThreshold = (healthTotal / Mathf.Max(1, enemiesEvent.Count)) * lastEnemyPossessionHealthFraction;
            if (sharedCurrentHealth <= possessHealthThreshold && fightState != FightState.Ending)
            {
                fightState = FightState.Ending;
                timeDizzyStarted = Time.time;
                lastStayingEnemy = healthController.GetComponent<EventEnemy>();
                healthController.SetInvincible(true);
                lastStayingEnemy.GetEnemy().lobotimzed = true;
            }   
        }
    }
    /// <summary>
    /// Updates the health info text, this is called in the inspector (only in editor mode)
    /// </summary>
    private void UpdateHealthPossessInfo() => healthPossessInfo = $"Each enemy goes down after taking {(healthTotal / Mathf.Max(1, enemiesEvent.Count)):F1} damage\nPossession start when the last enemy drops below {((healthTotal / Mathf.Max(1, enemiesEvent.Count)) * lastEnemyPossessionHealthFraction):F1}";
    /// <summary>
    /// Validates the number of enemies to revive on dizzy end (should be less than or equal to the number of enemies)
    /// </summary>
    /// <param name="val"> Value to validate </param>
    /// <returns> True if valid, false otherwise </returns>
    private bool ValidateRevivesOnFailedPossession(int val){ return val <= enemiesEvent.Count && val > 0; }
    /// <summary>
    /// Gets the number of enemies that are defeated
    /// </summary>
    /// <returns> Number of enemies defeated </returns>
    private int GetEnemyDefeatedCount()
    {
        int enemyDefeatedCount = 0;
        foreach (EventEnemy eventEnemy in enemiesEvent)
        {
            if (eventEnemy.GetState() == EventEnemy.EventEnemyState.Dizzy)
            {
                enemyDefeatedCount++;
            }
        }
        return enemyDefeatedCount;
    }
    /// <summary>
    /// Takes down an enemy (makes it dizzy)
    /// </summary>
    private void TakeDownEnemy(HealthController healthController)
    {
        healthController.KillEnemy();
        EventEnemy enemy = healthController.GetComponent<EventEnemy>();
        if (enemy == null)
        {
            Debug.LogError("The event enemy is doesn't have an event enemy component!");
            return;
        }

        enemy.SetState(EventEnemy.EventEnemyState.Dizzy);
        enemy.GetEnemy().lobotimzed = true;
        SurroundingPoints.instance.RemoveAttackingEnemy(enemy.GetEnemy());
    }
    /// <summary>
    /// Revives an enemy from dizzy state to attacking state
    /// </summary>
    private void ReviveEnemy(EventEnemy eventEnemy)
    {
        float healthbefore = eventEnemy.GetEnemy().health.GetHealth();
        eventEnemy.GetEnemy().health.SetHealthToMax();
        eventEnemy.SetState(EventEnemy.EventEnemyState.Attacking);
        eventEnemy.GetEnemy().aiState = (Enemy.AIMovementState.Chasing);
        eventEnemy.GetEnemy().canPossess = false;
        eventEnemy.GetEnemy().lobotimzed = false;
        eventEnemy.GetEnemy().health.SetInvincible(false);
        eventEnemy.GetEnemy().GetComponent<EventDryad>().Revive();
        // sharedCurrentHealth = Mathf.Min(healthTotal, sharedCurrentHealth + (healthTotal / Mathf.Max(1, enemiesEvent.Count)) - healthbefore); // add back the health of the revived enemy to the shared health (if it was last enemy then only add the reamining health of the max health..)
    }
}


