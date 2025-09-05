using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vertex
{
    [Tooltip("Node 1")]
    private Node node1;

    [Tooltip("Node 2")]
    private Node node2;

    [Tooltip("Distance of path")]
    float distance;

    /// <summary>
    /// Basic constructor
    /// </summary>
    /// <param name="n1">First node</param>
    /// <param name="n2">Second node</param>
    public Vertex(Node n1, Node n2, bool diagonal)
    {
        node1 = n1;
        node2 = n2;
        if (diagonal)
        {
            distance = Mathf.Sqrt(2);
        }
        else
        {
            distance = 1;
        }
    }

    /// <summary>
    /// When given one node, returns the other in the vertex
    /// </summary>
    /// <param name="node"> Node coming from </param>
    /// <returns>The other node</returns>
    public Node GetNode(Node node)
    {
        return node == node1 ? node2 : node1;
    }

    /// <summary>
    /// Gets the node distance
    /// </summary>
    /// <returns></returns>
    public float GetDistance()
    {
        return distance;
    }
}
