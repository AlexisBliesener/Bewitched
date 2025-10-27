using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// A class for creating points for AI to try to reach
/// This allows for the AI to navigate to different available points around the player
/// When the player is made an obstacle, this forces the AI to take paths around obstacles
/// </summary>
public class SurroundingPoints : MonoBehaviour
{
    // Singleton
    public static SurroundingPoints instance { get; private set; }

    [Tooltip("The Environment Layer")]
    public LayerMask environment;

    [Tooltip("Turns on debug mode")]
    [SerializeField] bool debugging = false;

    [Tooltip("If the Points are Active")]
    bool pointsActive = false;

    [Tooltip("List of enemies in surrounding range")]
    List<Enemy> surroundingEnemies = new List<Enemy>();

    [Tooltip("Minimum time range for starting attack")]
    [SerializeField] float minAttackTime = 0;

    [Tooltip("Maximum time range for starting attack")]
    [SerializeField] float maxAttackTime = 2;

    [Tooltip("The time set to start attack")]
    float startAttackTime;

    [Tooltip("The time the last attack occured")]
    float timeLastAttack;

    [Tooltip("The time between a room switching before enemies can attack")]
    float roomSwapEnemyWaitTime = 3;

    [Tooltip("Active room for enemies")]
    RoomController activeRoom = null;

    [Tooltip("Time since room swap")]
    private float timeLastRoomSwap;

    [Tooltip("Character the player is currently")]
    private Character currentPlayer;

    private void Awake()
    {
        timeLastRoomSwap = Time.time;
        instance = this;
        Init();
    }

    private void Update()
    {
        currentPlayer = PlayerController.instance.currentCharacter;

        HandleSurroundAttack();

        if (RoomSystem.Instance.GetActiveRoomController() != activeRoom)
        {
            activeRoom = RoomSystem.Instance.GetActiveRoomController();
            timeLastRoomSwap = Time.time;
        }
    }

    /// <summary>
    /// Create all surrounding points around the player
    /// </summary>
    public void Init()
    {
        startAttackTime = Random.Range(minAttackTime, maxAttackTime);
        timeLastAttack = Time.time;

        surroundingEnemies = new List<Enemy>();
        pointsActive = true;
    }

    /// <summary>
    /// Finds a path to the player and modifies the destination to be around the middle of the surrounding range
    /// </summary>
    /// <param name="enemy"> Enemy finding a path </param>
    /// <param name="backtrack"> If retreating or surrounding, finds a path from further back </param>
    /// <returns></returns>
    public IEnumerator FindPathToPlayer(Enemy enemy, bool backtrack)
    {
        Vector3 origin;
        if (backtrack)
        {
            Vector3 awayFromPlayer = (enemy.transform.position - currentPlayer.transform.position).normalized;
            origin = currentPlayer.transform.position + awayFromPlayer * (currentPlayer.sizeRadius + currentPlayer.maxSurroundingRadius);
        }
        else origin = enemy.transform.position;
        yield return StartCoroutine(GraphBuilder.instance.AStarSearch(enemy, origin, currentPlayer.transform.position));

        if (!enemy.HasSetPath()) yield break; // End if no path is found

        enemy.GetNavPath().AdjustPath(currentPlayer, enemy);
    }

    /// <summary>
    /// Adds enemy to surrounding enemy list
    /// </summary>
    /// <param name="enemy"> Enemy to add </param>
    public void AddSurroundingEnemy(Enemy enemy)
    {
        if (!surroundingEnemies.Contains(enemy))
        {
            surroundingEnemies.Add(enemy);
        }
    }

    /// <summary>
    /// Removes enemy from surrounding enemy list
    /// </summary>
    /// <param name="enemy"> Enemy to remove </param>
    public void RemoveSurroundingEnemy(Enemy enemy)
    {
        if (surroundingEnemies.Contains(enemy))
        {
            surroundingEnemies.Remove(enemy);
        }
    }

    /// <summary>
    /// Function that gets all enemies of the same type
    /// Useful for group attacks
    /// </summary>
    /// <param name="enemy"> Enemy looking for others of same type </param>
    /// <returns> List of enemies surrounding player of same type </returns>
    public List<Goblin> GetEnemiesSameType(Goblin enemy)
    {
        List<Goblin> sameEnemies = new List<Goblin>();

        foreach (Enemy other in surroundingEnemies)
        {
            if (other != null && other.TryGetComponent(out Goblin gob) && other != enemy)
            {
                sameEnemies.Add(gob);
            }
        }
        return sameEnemies;
    }

    /// <summary>
    /// Function that gets all enemies of the same type
    /// Useful for group attacks
    /// </summary>
    /// <param name="enemy"> Enemy looking for others of same type </param>
    /// <returns> List of enemies surrounding player of same type </returns>
    public List<Guard> GetEnemiesSameType(Guard enemy)
    {
        List<Guard> sameEnemies = new List<Guard>();

        foreach (Enemy other in surroundingEnemies)
        {
            if (other.TryGetComponent(out Guard gard) && other != enemy)
            {
                sameEnemies.Add(gard);
            }
        }
        return sameEnemies;
    }

    /// <summary>
    /// Tells an enemy in the surrounding list to attack
    /// </summary>
    public void HandleSurroundAttack()
    {
        Debug.Log(surroundingEnemies.Count);
        if (Time.time - timeLastAttack > startAttackTime && surroundingEnemies.Count > 0 && activeRoom != null && Time.time - timeLastRoomSwap > roomSwapEnemyWaitTime)
        {
            PriorityQueue<Enemy> tempEnemies = new PriorityQueue<Enemy>();
            foreach (Enemy enemy in surroundingEnemies)
            {
                if (enemy.IsNeutral() && !enemy.lobotimzed && !enemy.IsDead && !enemy.IsPlayerControlling()) // Don't attack if already attacking, lobotomized, dead, or playerControlled
                {
                    tempEnemies.Enqueue(enemy, enemy.GetAttackingPriority());
                }

                if (!enemy.IsNeutral())
                {
                    Debug.Log("Non-neutral enemy: " + enemy + ". Attack state: " + enemy.attackState);
                    return; // For now just keep returning until no enemies are attacking
                }
            }
            Debug.Log("Choosing enemy from: " + tempEnemies.Count + " enemies");

            if (tempEnemies.Count > 0)
            {
                while (tempEnemies.Count > 0)
                {
                    Enemy chosen = tempEnemies.Dequeue();

                    if (chosen.AttackFromSurrounding(this))
                    {
                        Debug.Log("Chosen enemy: " + chosen);
                        startAttackTime = Random.Range(minAttackTime, maxAttackTime);
                        timeLastAttack = Time.time;
                        break;
                    }
                }
            }
        }
    }
}
