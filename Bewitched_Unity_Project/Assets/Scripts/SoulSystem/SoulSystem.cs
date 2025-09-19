using System;
using UnityEngine;

/// <summary>
/// This class handles the soul currency system in the game.
/// It keeps the track of the current soul currency and allow for souls to be added and used.
/// </summary>
public class SoulSystem : MonoBehaviour
{

    // Singleton instance
    public static SoulSystem Instance { get; private set; }
    [SerializeField, Tooltip("The soul prefab to instantiate")]
    private GameObject soulPrefab;
    [SerializeField, Tooltip("Current soul count")]
    private int soulCount = 0;
    [SerializeField, Tooltip("How many souls to spawn per enemy as a range from 1 to x:"), Range(1, 10)]
    private int soulPerEnemy = 1;
    // <summary>
    /// Ensures that only one instance of SoulSystem exists and create it if it doesn't exist. It also makes sure that the system doesn't get destroyed when the game is reloaded.
    /// </summary>
    void Awake()
    {
        // Only one instance of SoulSystem should be there
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
        Instance = this;
    }
    // <summary> 
    /// <summary>
    /// Spawn souls in a random direction around the position that is passed in 
    /// </summary>
    /// <param name="position"></param>
    // </summary>
    public void SpawnSoul(Vector3 position)
    {
        int soulsToSpawn = UnityEngine.Random.Range(1, soulPerEnemy + 1);
        for (int i = 0; i < soulsToSpawn; i++)
        {
            // Add a random direction so it doesn't spawn in the same place
            Vector3 randomDirection = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(0.5f, 1f), UnityEngine.Random.Range(-1f, 1f));
            Instantiate(soulPrefab, position + randomDirection, Quaternion.identity);
        }
    }
    // <summary> Get current soul currency </summary>
    public int GetSoulCurrency() => soulCount;
    // <summary> Add souls to the current soul currency </summary>
    public void AddSouls(int amount) => soulCount += amount;
    // <summary> Use souls from the current soul currency (amount is subtracted from the currency and it doesn't go below 0) </summary>
    public void UseSoulCurrency(int amount) => soulCount = Mathf.Max(0, soulCount - amount);
    // <summary> Reset souls to 0</summary>
    public void ResetSouls() => soulCount = 0;
}
