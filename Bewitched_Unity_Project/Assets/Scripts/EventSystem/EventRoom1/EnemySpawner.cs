using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
/// This is a spawner for enemies in the event system, it will spawn enemies from a list of spawn points
public class EnemySpawner : MonoBehaviour
{
    [SerializeField, Tooltip("Enemy prefab")]
    private GameObject enemyPrefab;
    [SerializeField, Tooltip("How many enimies should be on the arena?")]
    private int maxEnemiesLimit = 5;
    [SerializeField, Tooltip("How many enimies should be jump down from the arena when the event enemy is possessed?")]
    private int maxEnemiesJumpDownLimit = 10;
    [SerializeField, Tooltip("Auto getting the points to spawn enemies from")]
    private GameObject realPlaceHolder;
    [SerializeField, Tooltip("This is used to hide the goblins when the event enemy is possessed")]
    private GameObject fakeGoblinPlaceHolder;

    [SerializeField, Tooltip("Points to spawn enemies from")]
    private List<GameObject> placeHolderEnemies;

    [SerializeField, Tooltip("Where should the enemies be jumping down to? Each index will represent a place holder (at the same index as the enemy prefab)")]
    private List<GameObject> jumpDownPlaceHolders;

    [SerializeField, Tooltip("Inital spawn points")]
    private List<GameObject> initialSpawnPoints;
    [Header("Jump Settings")]
    [SerializeField, Tooltip("How high the enemy will jump")]
    private float jumpPower = 12f;
    [SerializeField,Tooltip("How long the enemy will jump")]
    private float jumpDuration = 3f;
    [SerializeField, Tooltip("The room controller for the event room")]
    private RoomController roomController;
    
    /// <summary>
    /// Starts the spawner
    /// </summary>
    private void Start()
    {
        if (roomController == null)
        {
            Debug.LogWarning("Room controller is null on enemy spawner!");
            return;
        }
        // Instead of drag and drop each place holder ... 
        foreach (Transform placeHolder in realPlaceHolder.GetComponentInChildren<Transform>())
        {
            placeHolderEnemies.Add(placeHolder.gameObject);
            foreach (Transform go in placeHolder.GetComponentInChildren<Transform>())
            {
                if (go.name == "JumpPoint")
                {
                    jumpDownPlaceHolders.Add(go.gameObject);
                    continue;
                }
            }
        }

        StartCoroutine(SpawnInitalEnemies());
    }
    /// <summary>
    /// Spawns the final enemies
    /// </summary>
    /// <returns></returns>
    public IEnumerator SpawnFinalEnemies()
    {
        // First we will set all the enemies to be killed by one hit 
        foreach (GameObject enemyGameObject in roomController.roomEnemies)
        {
            Enemy enemy = enemyGameObject.GetComponent<Enemy>();
            enemy.health.SetCurrentHealth(1);
            enemy.sightRange = 150;
            // unsubscribe from the death event, so we don't spawn more enemies ...
            enemy.health.OnDeath -= OnEnemyDeath;
        }
        int enemiesSpawn = Mathf.Min(maxEnemiesJumpDownLimit, placeHolderEnemies.Count);
        for (int i = 0; i < enemiesSpawn; i++)
        {
            SpawnLastEnemy(i);
        }

        // After spawning all the enemies, hide all the goblins on the stands
        fakeGoblinPlaceHolder.SetActive(false);
        realPlaceHolder.SetActive(false);
        yield break;
    }
    /// <summary>
    /// Spawns the initial enemies
    /// </summary>
    /// <returns></returns>
    private IEnumerator SpawnInitalEnemies()
    {
        // spawn enemies on start
        for (int i = 0; i < maxEnemiesLimit; i++)
        {
            GameObject spawnPoint = initialSpawnPoints[UnityEngine.Random.Range(0, initialSpawnPoints.Count)];
            Enemy enemy = Instantiate(enemyPrefab, spawnPoint.transform.position, spawnPoint.transform.rotation, gameObject.transform).GetComponent<Enemy>();
            yield return null; // wait for one frame to make sure the enemy is spawned and called (Start) function on the enemy since the enemy is calling the AiState to Patrol
            enemy.aiState = Enemy.AIMovementState.Blocked;
            enemy.health.OnDeath += OnEnemyDeath;
            roomController.AddEnemy(enemy.gameObject);
        }
    }
    /// <summary>
    /// When an enemy dies, it will remove it from the list of enemies spawned and spawn another one        
    /// </summary>
    private void OnEnemyDeath(GameObject enemyGameObject)
    {
        SpawnEnemy();
    }
    /// <summary> 
    /// Spawns the last enemy in the list of place holders and it will remove it from the list (placeholder list)
    /// </summary>
    /// <returns> True if it spawned an enemy, false otherwise </returns>   
    private void SpawnLastEnemy(int index)
    {
        GameObject enemyPlaceHolder = placeHolderEnemies[index];
        if (enemyPlaceHolder == null || enemyPlaceHolder.activeSelf == false)
        {
            // if it was active it means that this place holder is being used (in proccess of spawning an enemy)
            // so we need to select another one and try again
            return;
        }

        // we found a place holder? good, let's start the process of spawning an enemy
        enemyPlaceHolder.SetActive(false); // set it to inactive to spawn the enemy in the place holder 
        Enemy enemy = Instantiate(enemyPrefab, enemyPlaceHolder.transform.position, enemyPlaceHolder.transform.rotation, gameObject.transform).GetComponent<Enemy>();
        // remove the place holder from the list and destroy it since we don't need it anymore
        // we will stop the ai to make the enemy jumping down from the stands
        enemy.aiState = Enemy.AIMovementState.Blocked;
        // Set settings for the enemy so they go to the player
        roomController.AddEnemy(enemy.gameObject);
        StartCoroutine(HandleJumpDown(enemy, index, false));
    }
    /// <summary>
    /// Spawns an enemy
    /// If the index is -1 it will pick a random one
    /// If the index is not -1 it will spawn the enemy at the corresponding index
    /// <param name="index">The index of the place holder to spawn the enemy at</param>
    /// </summary>
    private void SpawnEnemy(int index = -1)
    {
        if (index == -1)
        {
            index = UnityEngine.Random.Range(0, placeHolderEnemies.Count);
        }
        GameObject enemyPlaceHolder = placeHolderEnemies[index];
        if (enemyPlaceHolder.activeSelf == false)
        {
            // if it was active it means that this place holder is being used (in proccess of spawning an enemy)
            // so we need to select another one and try again
            SpawnEnemy();
            return;
        }

        // we found a place holder? good, let's start the process of spawning an enemy
        enemyPlaceHolder.SetActive(false); // set it to inactive to spawn the enemy in the place holder 
        Enemy enemy = Instantiate(enemyPrefab, enemyPlaceHolder.transform.position, enemyPlaceHolder.transform.rotation, gameObject.transform).GetComponent<Enemy>();
        // we will stop the ai to make the enemy jumping down from the stands
        enemy.aiState = Enemy.AIMovementState.Blocked;
        enemy.health.OnDeath += OnEnemyDeath;
        roomController.AddEnemy(enemy.gameObject);
        StartCoroutine(HandleJumpDown(enemy, index));
    }
    /// <summary>
    /// To handle the jump down for the enemy 
    /// </summary>
    /// <param name="enemy">The enemy to jump down</param>
    /// <param name="index">The index corresponding to the place holder </param>
    /// <param name="reactivatePlaceHolder">If true, it will reactivate the place holder after the enemy is spawned</param>
    private IEnumerator HandleJumpDown(Enemy enemy, int index, bool reactivatePlaceHolder = true)
    {
        yield return new WaitForSeconds(0.2f);
        Transform target = jumpDownPlaceHolders[index].transform;
        enemy.GetCharacterController().enabled = false;
        GoblinAnimator goblinAnimator = enemy.GetComponent<GoblinAnimator>();
        if (goblinAnimator != null)
        {
            // I just used the primary attack for now, since we don't have a jump animation (yet?) and I think it kind of doing the job :) 


            // Just kidding I thought they called me the animator but no :( this is caused the goblin to not move after they spawn so I will leave it to the REAL animator... 

            // goblinAnimator.SwitchState("PrimaryAttack", 0);
            // yield return StartCoroutine(goblinAnimator.WaitForDelay("PrimaryAttack", 1));

            yield return enemy.transform.DOJump(target.position, jumpPower, 1, jumpDuration).SetEase(Ease.OutQuad).WaitForCompletion();
            // goblinAnimator.ExitLeap();
        }


        enemy.transform.position = target.position;
        enemy.GetCharacterController().enabled = true;
        // Reactivate the source placeholder (this spot can spawn again)
        if (reactivatePlaceHolder)
        {
            placeHolderEnemies[index].SetActive(true);
        }
        else
        {
            enemy.sightRange = 150;
            enemy.health.SetCurrentHealth(1);
        }
        // Set the enemy to patrol
        enemy.aiState = Enemy.AIMovementState.Chasing;
    }
    /// <summary>
    /// Unsubscribe from the enemy death event when destroyed
    /// </summary>
    private void OnDestroy()
    {
        foreach (GameObject enemy in roomController.roomEnemies)
        {
            enemy.GetComponent<Enemy>().health.OnDeath -= OnEnemyDeath;
        }
    }

    /// <summary>
    /// Activates the spawner
    /// </summary>
    public void Activate()
    {
        // Set all the enimies to patrolling (This is only for the enimies that are spawned on start)
        foreach (GameObject enemy in roomController.roomEnemies)
        {
            enemy.GetComponent<Enemy>().aiState = Enemy.AIMovementState.Patrolling;
        }
    }
}