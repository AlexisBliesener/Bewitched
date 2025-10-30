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

    [Tooltip("Maximum number of attack points available")]
    public int maxAttackPoints = 10;

    [Tooltip("Number of attack points currently available")]
    private int attackPoints;

    [Tooltip("Turns on debug mode")]
    [SerializeField] bool debugging = false;

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

    [Tooltip("Currently attacking enemies and their attack costs")]
    Dictionary<Character, int> attackingEnemies = new Dictionary<Character, int>();

    private void Awake()
    {
        timeLastRoomSwap = Time.time;
        attackPoints = maxAttackPoints;
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

        float timeStarted = Time.time;
        Debug.Log("Starting search");
        yield return StartCoroutine(GraphBuilder.instance.AStarSearch(enemy, origin, currentPlayer.transform.position));
        Debug.Log("Ending search after " + (Time.time - timeStarted) + " seconds");

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
        if (Time.time - timeLastAttack > startAttackTime && surroundingEnemies.Count > 0 && activeRoom != null && Time.time - timeLastRoomSwap > roomSwapEnemyWaitTime)
        {
            PriorityQueue<Enemy> tempEnemies = new PriorityQueue<Enemy>();
            foreach (Enemy enemy in surroundingEnemies)
            {
                if (enemy.IsNeutral() && !enemy.lobotimzed && !enemy.IsDead && !enemy.IsStunned && !enemy.cantAttack && !enemy.IsPlayerControlling()) // Don't attack if already attacking, lobotomized, dead, or playerControlled
                {
                    tempEnemies.Enqueue(enemy, enemy.GetAttackingPriority());
                }

                if (enemy.InAttackStartup())
                {
                    return; // For now just keep returning until no enemies are attacking
                }
            }

            if (tempEnemies.Count > 0)
            {
                while (tempEnemies.Count > 0)
                {
                    Enemy chosen = tempEnemies.Dequeue();
                    int cost = chosen.AttackFromSurrounding(this);

                    if (cost > 0)
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

    /// <summary>
    /// Gets the attack points available at this time
    /// </summary>
    /// <returns> Attack points current </returns>
    public int GetAvailableAttackPoints()
    {
        return attackPoints;
    }

    /// <summary>
    /// Adds an enemy to attacking enemies
    /// </summary>
    /// <param name="enemy"> Enemy to add </param>
    /// <param name="cost"> Cost of attack </param>
    public void AddAttackingEnemy(Character enemy, int cost)
    {
        if (!attackingEnemies.ContainsKey(enemy))
        {
            attackingEnemies[enemy] = cost;
            attackPoints -= cost;
        }
    }

    /// <summary>
    /// Removes an enemy from attacking enemies
    /// </summary>
    /// <param name="enemy"> Enemy to remove </param>
    public void RemoveAttackingEnemy(Character enemy)
    {
        if (attackingEnemies.ContainsKey(enemy))
        {
            attackPoints += attackingEnemies[enemy];
            attackingEnemies.Remove(enemy);
        }
    }
}
