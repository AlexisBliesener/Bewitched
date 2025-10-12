using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// A class representing a path through the graph
/// </summary>
public class NavPath
{
    [Tooltip("Total path cost")]
    float totalCost;

    [Tooltip("Actual distance of path")]
    float distance;

    [Tooltip("Origin node")]
    Node origin;

    [Tooltip("Destination node")]
    Node destination;

    [Tooltip("Dictionary of nodes to parent nodes in path")]
    Dictionary<Node, Vertex> parentNodes;

    [Tooltip("List of nodes representing the path")]
    List<Node> positions = new List<Node>();

    [Tooltip("List of corners for easier calculation")]
    List<Node> corners = new List<Node>();

    [Tooltip("Path Status")]
    bool pathComplete;

    /// <summary>
    /// Default constructor
    /// </summary>
    public NavPath()
    {
        totalCost = 0;
        distance = 0;
        origin = null;
        destination = null;
        parentNodes = new Dictionary<Node, Vertex>();
        pathComplete = false;
    }

    public NavPath(Enemy enemy)
    {
        totalCost = 0;
        distance = 0;
        origin = GraphBuilder.instance.FindClosestNode(enemy.transform.position);
        destination = GraphBuilder.instance.FindClosestNode(enemy.transform.position);
        parentNodes = new Dictionary<Node, Vertex>();
        pathComplete = true;
    }

    /// <summary>
    /// Sets the parent of a node on the path
    /// </summary>
    /// <param name="node"> Node setting </param>
    /// <param name="parent"> Parent node </param>
    public void SetPathVertex(Node node, Vertex vertex)
    {
        parentNodes[node] = vertex;
    }

    /// <summary>
    /// Sets the origin node
    /// </summary>
    /// <param name="node"> Origin node </param>
    public void SetOrigin(Node node)
    {
        origin = node;
    }

    /// <summary>
    /// Sets the destination node
    /// </summary>
    /// <param name="node"> Destination node </param>
    public void SetDestination(Node node)
    {
        destination = node;
    }

    /// <summary>
    /// Creates the list of positions for the path
    /// </summary>
    public void CalculatePath()
    {
        positions = new List<Node>();
        corners = new List<Node>();

        Tuple<int, int, int> prevDirection = new Tuple<int,int, int>(0, 0, 0);

        if (destination == origin)
        {
            corners.Add(destination);
            return;
        }

        Node currentNode = destination;

        while (currentNode != origin)
        {
            positions.Insert(0, currentNode);

            Vertex jumpVertex = parentNodes[currentNode];

            Node next = GraphBuilder.instance.GetNodeFromTuple(jumpVertex.GetNode(currentNode));

            distance += jumpVertex.GetDistance();

            totalCost += jumpVertex.GetDistance() + next.GetCost();

            Tuple<int, int, int> jumpDirection = GetDirection(currentNode.GetNodeValues(), next.GetNodeValues());

            if (jumpDirection.Item1 != prevDirection.Item1 || jumpDirection.Item2 != prevDirection.Item2 || jumpDirection.Item3 != prevDirection.Item3 || jumpVertex.IsVertical())
            {
                corners.Add(currentNode);
            }
            prevDirection = jumpDirection;

            currentNode = next;
        }
        pathComplete = true;
        corners.Reverse(); // Since we made our corners backwards, reverse the list

        return;
    }

    /// <summary>
    /// Gets the direction traversed between two nodes and that including vertical movement
    /// </summary>
    /// <param name="first"> First node in path </param>
    /// <param name="second"> Second node in path </param>
    /// <returns> Tuple representing the direction travelled </returns>
    private Tuple<int,int, int> GetDirection(Tuple<int,int, int> first, Tuple<int,int, int> second)
    {
        int x = second.Item1 - first.Item1;
        int z = second.Item2 - first.Item2;
        int y = second.Item3 - first.Item3;
        return new Tuple<int, int, int>(x, z, y);
    }

    /// <summary>
    /// Gets the list of positions
    /// </summary>
    /// <returns> List of positions </returns>
    public List<Vector3> GetPathPositions()
    {
        List<Vector3> pathPositions = new List<Vector3>();

        foreach (Node node in positions)
        {
            pathPositions.Add(node.GetPosition());
        }

        return pathPositions;
    }

    /// <summary>
    /// Gets the corners
    /// </summary>
    /// <returns> The corners of the path </returns>
    public List<Node> GetCornerNodes()
    {
        return corners;
    }

    /// <summary>
    /// Gets the position of the destination node
    /// </summary>
    /// <returns> Destination position </returns>
    public Vector3 GetDestinationPosition(GameObject obj = null)
    {
        return destination.GetPosition(); // now it will use real y (floor height)
    }

    /// <summary>
    /// Checks if an enemy has reached their destination
    /// </summary>
    /// <param name="enemy"> Enemy checking </param>
    /// <returns> True if they reached their destination </returns>
    public bool ReachedDestination(Enemy enemy)
    {

        // if (Vector3.Distance(enemy.transform.position, destination.GetPosition(enemy.gameObject)) <= enemy.minStopDistance)
        // {
        //     return true;
        // }
        // return false;

        // we will check first the horizontal distance then the vertical distance
        float horizontalDistance = Vector2.Distance(new Vector2(enemy.transform.position.x, enemy.transform.position.z), new Vector2(destination.GetPosition().x, destination.GetPosition().z));
        if (horizontalDistance <= enemy.minStopDistance)
        {
            return true;
        }   
        return false;
    }

    /// <summary>
    /// Get distance
    /// </summary>
    /// <returns> Distance </returns>
    public float GetDistance()
    {
        return distance / 10;
    }

    /// <summary>
    /// Gets the status of the path
    /// </summary>
    /// <returns> True if complete </returns>
    public bool PathComplete()
    {
        return pathComplete;
    }
}

