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
    [Tooltip("The Environment Layer")]
    public LayerMask environment;

    [Tooltip("Turns on debug mode")]
    [SerializeField] bool debugging = false;

    [Tooltip("Debugging Prefab")]
    [SerializeField] GameObject pointObjPrefab;

    [Tooltip("Dictionary of Points to Characters Using Them")]
    Dictionary<GameObject, Enemy> points = new Dictionary<GameObject, Enemy>();

    [Tooltip("Parent point")]
    GameObject parentPoint;

    [Tooltip("If the Points are Active")]
    bool pointsActive = false;

    [Tooltip("Radius of points")]
    float pointRadius;

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

    [Tooltip("List of nodes that are costly")]
    List<Node> costlyNodes = new List<Node>();

    [Tooltip("Position the last costly area was made")]
    Vector3 lastCostlyPosition = Vector3.zero;

    [Tooltip("Distance for node reset")]
    [SerializeField] float resetCostlyAreaDistance = 1;

    private void Update()
    {
        if (pointsActive)
        {
            HandlePointsEachFrame();
            HandleSurroundAttack();
            CreateLocalCostlyArea();
        }
    }

    /// <summary>
    /// Handles point validity and position
    /// Run every frame in Update
    /// </summary>
    public void HandlePointsEachFrame()
    {
        if (parentPoint)
        {
            parentPoint.transform.position = transform.position;
        }

        List<GameObject> resetters = new List<GameObject>();
        int i = 0;

        foreach (GameObject point in points.Keys)
        {
            Enemy enemy = points[point];
            if (enemy)
            {
                if (!PointAccessibleByParent(point))
                {
                    points[point].RemoveTargetPoint();
                    resetters.Add(point);
                }
            }
            i++;
        }

        foreach (GameObject j in resetters)
        {
            points[j] = null;
        }
    }

    /// <summary>
    /// Create all surrounding points around the player
    /// </summary>
    /// <param name="numPoints"> Number of points to make </param>
    /// <param name="radius"> Radius of point placement </param>
    public void Init(int numPoints, float radius)
    {
        startAttackTime = Random.Range(minAttackTime, maxAttackTime);
        timeLastAttack = Time.time;

        pointRadius = radius;

        surroundingEnemies = new List<Enemy>();
        parentPoint = new GameObject("Parent Point");
        for (int i = 0; i < numPoints; i++)
        {
            GameObject point;
            if (debugging)
            {
                point = Instantiate(pointObjPrefab, parent: parentPoint.transform, worldPositionStays: true);
                point.name = "point" + (i + 1);
            }
            else
            {
                point = new GameObject("point" + (i + 1));
            }
            point.transform.SetParent(parentPoint.transform, worldPositionStays: true);

            point.transform.localPosition = new Vector3(radius * Mathf.Sin(Mathf.Deg2Rad * i * 360 / numPoints), 0, radius * Mathf.Cos(Mathf.Deg2Rad * i * 360 / numPoints));
            points[point] = null;
        }
        pointsActive = true;
    }

    /// <summary>
    /// Checks if there is a line between the point and parent unhindered by the environment
    /// </summary>
    /// <param name="point"> Point to check </param>
    /// <returns> True if unhindered point </returns>
    public bool PointAccessibleByParent(GameObject point)
    {
        Vector3 direction = (point.transform.position - parentPoint.transform.position).normalized;
        float distance = Vector3.Distance(parentPoint.transform.position, point.transform.position);

        if (Physics.Raycast(transform.position, direction, distance, environment)) // If environment between points
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Called when switching out of body, destroys all points
    /// </summary>
    public void DestroyPoints()
    {
        Destroy(parentPoint);
        parentPoint = null;
        foreach (GameObject point in points.Keys)
        {
            // Set each enemy's point to null
            if (points[point])
            {
                points[point].RemoveTargetPoint();
            }
            Destroy(point);
        }
        points = new Dictionary<GameObject, Enemy>();
        surroundingEnemies = new List<Enemy>();
        pointsActive = false;
    }

    /// <summary>
    /// Finds the closest available point, removing enemies from their points if they are of the same type and closer
    /// </summary>
    /// <param name="enemy"> Enemy using the function </param>
    /// <returns></returns>
    public GameObject AssignPoint(Enemy enemy)
    {
        List<GameObject> finiteCopy = new List<GameObject>(points.Keys);
        float closestDist = Mathf.Infinity;
        GameObject closestPoint = null;
        Enemy competition = null;

        foreach (GameObject point in finiteCopy)
        {
            if (!pointsActive) { return null; } 

            // Check if position is accessible by parent
            if (PointAccessibleByParent(point))
            {
                float distance = (point.transform.position - enemy.transform.position).magnitude;

                if (distance < closestDist) // If the point is closer
                {
                    if (points[point]) // If not null
                    {
                        Enemy tempCompetition = points[point];
                        if (enemy == tempCompetition)
                        {
                            competition = tempCompetition;
                            closestPoint = point;
                            closestDist = distance;
                        }
                        else if (enemy.GetType() == tempCompetition.GetType()) // If the same type - same relative priority
                        {
                            if (distance < (tempCompetition.transform.position - point.transform.position).magnitude) // If this is closer
                            {
                                competition = tempCompetition;
                                closestPoint = point;
                                closestDist = distance;
                            }
                        }
                        else if (enemy.agent.avoidancePriority < tempCompetition.agent.avoidancePriority) // If not the same type, compare priority
                        {
                            competition = tempCompetition;
                            closestPoint = point;
                            closestDist = distance;
                        }
                    }
                    else // If null, hold onto it
                    {
                        closestDist = distance;
                        closestPoint = point;
                        competition = null;
                    }
                }
            }
        }

        if (closestPoint)
        {
            if (points.ContainsKey(closestPoint))
            {
                foreach (var item in points.Where(kvp => kvp.Value == enemy).ToList()) // If enemy assigned different point
                {
                    points[item.Key] = null; // Assign old points null
                }

                points[closestPoint] = enemy;

                if (competition) // If removing another character, make character assign a new point
                {
                    competition.RemoveTargetPoint();
                }
            }
        }
        return closestPoint;
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
    public List<Enemy> GetEnemiesSameType(Enemy enemy)
    {
        List<Enemy> sameEnemies = new List<Enemy>();

        foreach (Enemy other in surroundingEnemies)
        {
            if (enemy.GetType() == other.GetType())
            {
                sameEnemies.Add(other);
            }
        }
        return sameEnemies;
    }

    /// <summary>
    /// Tells an enemy in the surrounding list to attack
    /// </summary>
    public void HandleSurroundAttack()
    {
        if (Time.time - timeLastAttack > startAttackTime && surroundingEnemies.Count > 0)
        {
            PriorityQueue<Enemy> tempEnemies = new PriorityQueue<Enemy>();
            foreach (Enemy enemy in surroundingEnemies)
            {
                tempEnemies.Enqueue(enemy, enemy.GetAttackingPriority());
            }

            Enemy chosen = tempEnemies.Dequeue();
            while (tempEnemies.Count > 0 && !chosen.AttackFromSurrounding(this))
            {
                chosen = tempEnemies.Dequeue();
            }

            startAttackTime = Random.Range(minAttackTime, maxAttackTime);
            timeLastAttack = Time.time;
            Debug.Log("Start Attack Time: " + startAttackTime);
        }
    }

    /// <summary>
    /// Creates a costly area around the player that enemies will avoid entering
    /// </summary>
    public void CreateLocalCostlyArea()
    {
        if (Vector3.Distance(transform.position, lastCostlyPosition) > resetCostlyAreaDistance)
        {
            ResetCostlyArea();

            costlyNodes = GraphBuilder.instance.GetNodesInRadius(gameObject, pointRadius + 1.5f);
            foreach (Node node in costlyNodes)
            {
                node.AddCost(8000);
            }
            lastCostlyPosition = transform.position;
        }
    }

    /// <summary>
    /// Resets the costly area values
    /// </summary>
    public void ResetCostlyArea()
    {
        foreach (Node node in costlyNodes)
        {
            node.AddCost(-8000);
        }
    }
}
