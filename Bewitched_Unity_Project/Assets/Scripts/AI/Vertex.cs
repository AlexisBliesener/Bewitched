using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Vertex
{
    [Tooltip("Node 1 X")]
    [SerializeField] private int node1X;
    [Tooltip("Node 1 Y")]
    [SerializeField] private int node1Y;
    [Tooltip("Node 1 Z")]
    [SerializeField] private int node1Z;

    [Tooltip("Node 2 X")]
    [SerializeField] private int node2X;
    [Tooltip("Node 2 Y")]
    [SerializeField] private int node2Y;
    [Tooltip("Node 2 Z")]
    [SerializeField] private int node2Z;

    [Tooltip("Distance of path")]
    [SerializeField] float distance;
    [Tooltip("Is this a vertical connection between floors")]
    [SerializeField] private bool isVertical;

    /// <summary>
    /// Basic constructor
    /// </summary>
    /// <param name="n1">First node</param>
    /// <param name="n2">Second node</param>
    /// <param name="diagonal">Is diagonal movement</param>
    /// <param name="vertical">Is vertical movement between floors</param>
    public Vertex(Node n1, Node n2, bool diagonal, bool vertical = false)
    {
        Tuple<int, int, int> n1Vals = n1.GetNodeValues();
        node1X = n1Vals.Item1;
        node1Z = n1Vals.Item2;
        node1Y = n1Vals.Item3;
        Tuple<int, int, int> n2Vals = n2.GetNodeValues();
        node2X = n2Vals.Item1;
        node2Z = n2Vals.Item2;
        node2Y = n2Vals.Item3;
        isVertical = vertical;
        if (vertical)
        {
            distance = Mathf.Abs(node1Y - node2Y) / 10f * 1.2f; // 20% for vertical movement
        }
        else if (diagonal)
        {
            distance = n1.GetNodeDistance() * Mathf.Sqrt(2);
        }
        else
        {
            distance = n1.GetNodeDistance();
        }
    }

    /// <summary>
    /// When given one node, returns the other in the vertex
    /// </summary>
    /// <param name="node"> Node coming from </param>
    /// <returns>The other node's values </returns>
    public Tuple<int, int, int> GetNode(Node node)
    {
        if (node.GetNodeValues().Item1 == node1X && node.GetNodeValues().Item2 == node1Z && node.GetNodeValues().Item3 == node1Y)
        {
            return new Tuple<int, int, int>(node2X, node2Z, node2Y);
        }
        else
        {
            return new Tuple<int, int, int>(node1X, node1Z, node1Y);
        }
    }

    /// <summary>
    /// Gets the node distance
    /// </summary>
    /// <returns></returns>
    public float GetDistance()
    {
        return distance;
    }
    
    /// <summary>
    /// Is this a vertical connection   
    /// </summary>
    /// <returns>True if vertical movement </returns>
    public bool IsVertical() => isVertical;
}
