using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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


    // HEYY REMOVE IT MOHAMMEDD THIS IS A TEST FUNCTIONS!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    public Enemy enemy;
    public Enemy enemy2;
    public Enemy enemy3;
    public Enemy enemy4;
    [ContextMenu("Kill Enemy")]
    public void KillEnemies()
    {
        enemy.Die();
        enemy2.Die();
        enemy3.Die();
        enemy4.Die();
    }
    [ContextMenu("Kill Enemy 1")]
    public void KillEnemy1()
    {
        enemy.Die();
    }
    [ContextMenu("Kill Enemy 2")]
    public void KillEnemy2()
    {
        enemy2.Die();
    }
    [ContextMenu("Kill Enemy 3")]
    public void KillEnemy3()
    {
        enemy3.Die();
    }
    [ContextMenu("Kill Enemy 4")]
    public void KillEnemy4()
    {
        enemy4.Die();
    }
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
    public int GetSoulCurrency() => soulCount;
    public void AddSouls(int amount) => soulCount += amount;
    public void UseSoulCurrency(int amount) => soulCount = Mathf.Max(0, soulCount - amount);
    
    public void ResetSouls() => soulCount = 0;
    

}
