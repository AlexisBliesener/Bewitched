using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class that manages enemies and attacks
/// </summary>
public class EnemyManager : MonoBehaviour
{
    [Tooltip("List of enemies to manage")]
    private List<Enemy> enemies = new List<Enemy>();

    [Tooltip("Character currently being controlled by the player")]
    private Character currentPlayer;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        currentPlayer = PlayerController.instance.currentCharacter;
    }

    /// <summary>
    /// Sets the enemies in this room
    /// </summary>
    /// <param name="roomEnemies"> List of enemy gameobjects </param>
    public void SetEnemies(List<GameObject> roomEnemies)
    {
        foreach (GameObject enemyObj in roomEnemies)
        {
            if (enemyObj.TryGetComponent(out Enemy enemy))
            {
                enemies.Add(enemy);
                enemy.SetEnemyManager(this);
            }
        }
    }
}
