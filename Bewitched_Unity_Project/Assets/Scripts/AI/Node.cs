using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A class representing a node
/// These nodes have a position (transform multiplied by 10 so indexing isn't a pain with floats)
/// They also have a cost that determines if they are going to be walked on or unavailable
/// </summary>
[Serializable]
public class Node
{
    [Tooltip("X position multiplied by 10")]
    [SerializeField] private int xPos;

    [Tooltip("Relative y position to graph")]
    [SerializeField] private int yPos;

    [Tooltip("Z position multiplied by 10")]
    [SerializeField] private int zPos;

    [Tooltip("Cost of a node")]
    [SerializeField] private int nodeCost;

    [Tooltip("List of vertices")]
    [SerializeField] private List<Vertex> vertices = new List<Vertex>();

    [Tooltip("Node separation")]
    [SerializeField] private int nodeSeparation;

    /// <summary>
    /// Sets the values for the node
    /// </summary>
    /// <param name="x"> X position </param>
    /// <param name="z"> Z position </param>
    /// <param name="nodeDistance"> Distance between other nodes </param>
    /// <param name="y"> Y position </param>
    public void SetValues(int x, int z, int nodeDistance, int y)
    {
        xPos = x;
        zPos = z;
        yPos = y;
        nodeCost = 0;
        nodeSeparation = nodeDistance;
    }

    /// <summary>
    /// Create function utilizing a private constructor
    /// If the node is not on the floor, it is destroyed and no node is created
    /// Stops us from creating unnecessary nodes
    /// </summary>
    /// <param name="x"> X position multiplied by ten into an int </param>
    /// <param name="z"> Z position multiplied by ten into an int </param>
    /// <param name="nodeDistance"> Distance apart the nodes are </param>
    /// <param name="floor"> Floor layer </param>
    /// <param name="environment">Environment layer </param>
    /// <param name="maxHeight">Maximum height to scan for floors</param>
    /// <param name="minSeparation">Minimum separation between floors</param>
    /// <returns> A new node or null </returns>
    public static  List<Node> Create(int x, int z, int nodeDistance, LayerMask floor, LayerMask environment, float maxHeight, float minSeparation)
    {
        List<Node> validNodes = new List<Node>();
        Vector3 basePosition = new Vector3(x / 10f, 0, z / 10f);
        List<float> foundFloors = new List<float>();
        
        // Scan from top to bottom using RaycastAll
        Vector3 rayStart = basePosition + Vector3.up * maxHeight;
        RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, maxHeight * 2f, floor);
        
        // sort to closest first
        Array.Sort(hits, (hit1, hit2) => hit1.distance.CompareTo(hit2.distance));
        
        foreach (RaycastHit hit in hits)
        {
            bool validFloor = true;
            foreach (float existingFloor in foundFloors)
            {
                if (Mathf.Abs(hit.point.y - existingFloor) < minSeparation)
                {
                    validFloor = false;
                    break;
                }
            }
            
            if (validFloor)
            {
                Node newNode = new Node();
                newNode.SetValues(x, z, nodeDistance, (int)(hit.point.y * 10));
                if (newNode.IsOpen(floor, environment))
                {
                    foundFloors.Add(hit.point.y);
                    validNodes.Add(newNode);
                }
            }
        }

        return validNodes;
    }

    /// <summary>
    /// Function to determine if a point is available
    /// Currently only 2 dimensional
    /// Will update when I am not gravely behind on AI cause I'm making a new system
    /// </summary>
    /// <param name="floor"> Floor layermask </param>
    /// <param name="walls"> Wall layermask </param>
    /// <returns></returns>
    public bool IsOpen(LayerMask floor, LayerMask walls)
    {
        Vector3 position = GetPosition();
        position.y += 5;

        RaycastHit hit;
        if (Physics.Raycast(position, Vector3.down, out hit, 30, floor))
        {
            yPos = (int)(hit.point.y * 10);
            // check if blocked by walls
            Vector3 checkPos = hit.point + Vector3.up * 0.1f;
            if (Physics.CheckSphere(checkPos, 0.01f, walls)) // with a radius of 0.01f
            {
                return false;
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Assign a vertex to a node
    /// </summary>
    /// <param name="vertex"> Vertex to assign </param>
    public void AssignVertex(Vertex vertex)
    {
        vertices.Add(vertex);
    }

    /// <summary>
    /// Vertex getter
    /// </summary>
    /// <returns> List of vertices </returns>
    public List<Vertex> GetVertices()
    {
        return vertices;
    }

    /// <summary>
    /// Gets the node cost
    /// </summary>
    /// <returns> Cost of the node </returns>
    public int GetCost()
    {
        return nodeCost;
    }

    /// <summary>
    /// Gets the node's real position
    /// </summary>
    /// <returns> The position of the node (y is real now:) ) </returns>
    public Vector3 GetPosition(GameObject obj = null)
    {
        if (obj == null)
        {
            return new Vector3(xPos / 10f, yPos / 10f, zPos / 10f);
        }
        return new Vector3(xPos / 10f, obj.transform.position.y, zPos / 10f);
    }

    /// <summary>
    /// Gets node separation
    /// </summary>
    /// <returns> Node distance </returns>
    public int GetNodeDistance()
    {
        return nodeSeparation;
    }

    /// <summary>
    /// Gets the node x/z/y values
    /// </summary>
    /// <returns> Tuple containing x , z and y </returns>
    public Tuple<int, int, int> GetNodeValues()
    {
        return new Tuple<int, int, int>(xPos, zPos, yPos);
    }

    /// <summary>
    /// Add a cost to a node
    /// </summary>
    /// <param name="cost"> Cost to add </param>
    public void AddCost(int cost)
    {
        nodeCost += cost;
    }

    /// <summary>
    /// Resets the node cost
    /// </summary>
    public void ResetCost()
    {
        nodeCost = 0;
    }
}
