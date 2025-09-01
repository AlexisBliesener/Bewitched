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
    [Tooltip("Dictionary of Points to Characters Using Them")]
    Dictionary<GameObject, Enemy> points = new Dictionary<GameObject, Enemy>();

    [Tooltip("If the Points are Active")]
    bool pointsActive = false;

    private void Update()
    {
        List<GameObject> resetters = new List<GameObject>();
        int i = 0;

        foreach (GameObject point in points.Keys)
        {
            Enemy enemy = points[point];
            if (enemy)
            {
                NavMeshPath path = new NavMeshPath();
                if (!(enemy.agent.CalculatePath(point.transform.position, path) || path.status != NavMeshPathStatus.PathComplete))
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
        for (int i = 0; i < numPoints; i++)
        {
            GameObject point = new GameObject("point" + (i + 1));
            point.transform.SetParent(transform, worldPositionStays: true);

            point.transform.localPosition = new Vector3(radius * Mathf.Sin(2 * Mathf.PI * i / numPoints), 0, radius * Mathf.Cos(2 * Mathf.PI * i / numPoints));
            points[point] = null;
        }
        pointsActive = true;
    }

    /// <summary>
    /// Called when switching out of body, destroys all points
    /// </summary>
    public void DestroyPoints()
    {
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
            if (!enemy.agent.enabled || !pointsActive) { return null; } // Return out if enemy is possessed

            NavMeshPath path = new NavMeshPath(); // Check if position is accessible by enemy
            if (enemy.agent.CalculatePath(point.transform.position, path) && path.status == NavMeshPathStatus.PathComplete)
            {
                float distance = (enemy.transform.position - point.transform.position).magnitude;
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
}
