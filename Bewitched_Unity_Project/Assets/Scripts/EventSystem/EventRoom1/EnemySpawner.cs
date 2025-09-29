using UnityEngine;
/// This is a spawner for enemies in the event system, it will spawn enemies from a list of spawn points
public class EnemySpawner : MonoBehaviour
{
    [SerializeField, Tooltip("Spawn interval")]
    private float spawnInterval = 1f;
    [SerializeField, Tooltip("Enemy prefab")]
    private GameObject enemyPrefab;
    [SerializeField, Tooltip("Max enemies to spawn")]
    private int maxEnemies = 10;
    [SerializeField, Tooltip("Min enemies to spawn")]
    private int minEnemies = 1;
    [Tooltip("The number of enemies spawned")]
    private int enemiesSpawned = 0;
    [Tooltip("The time the last enemy was spawned")]
    private float timeLastSpawned = 0f;

    [SerializeField, Tooltip("Points to spawn enemies from")]
    private GameObject[] spawnPoints;
    [Tooltip("Is the spawner active or not")]
    private bool isActive = false;
    /// <summary>
    /// Starts the spawner and sets the time last spawned to the current time
    /// </summary>
    private void Start()
    {
        timeLastSpawned = Time.time + spawnInterval; // Add a little bit of time to make sure the first spawn is not instant
    }
    /// <summary>
    /// Updates the spawner, if the spawner is active and the time since the last spawn is greater than the spawn interval, it will spawn an enemy
    /// </summary>
    private void Update()
    {
        if (isActive && Time.time - timeLastSpawned >= spawnInterval)
        {
            if (enemiesSpawned < maxEnemies)
            {
                Instantiate(enemyPrefab, spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)].transform.position, transform.rotation);
                enemiesSpawned++;
            }
            timeLastSpawned = Time.time;
        }
    }
    /// <summary>
    /// Activates the spawner
    /// </summary>
    public void Activate()
    {
        isActive = true;
    }
    /// <summary>
    /// Deactivates the spawner
    /// </summary>
    public void Deactivate()
    {
        isActive = false;
    }
}