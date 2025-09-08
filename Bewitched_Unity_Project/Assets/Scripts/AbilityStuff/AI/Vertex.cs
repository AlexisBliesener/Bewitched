using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Vertex
{
    [Tooltip("Node 1 X")]
    [SerializeField] private int node1X;
    [Tooltip("Node 1 Z")]
    [SerializeField] private int node1Z;

    [Tooltip("Node 2 X")]
    [SerializeField] private int node2X;
    [Tooltip("Node 2 Z")]
    [SerializeField] private int node2Z;

    [Tooltip("Distance of path")]
    [SerializeField] float distance;

    /// <summary>
    /// Basic constructor
    /// </summary>
    /// <param name="n1">First node</param>
    /// <param name="n2">Second node</param>
    public Vertex(Node n1, Node n2, bool diagonal)
    {
        Tuple<int, int> n1Vals = n1.GetNodeValues();
        node1X = n1Vals.Item1;
        node1Z = n1Vals.Item2;
        Tuple<int,int> n2Vals = n2.GetNodeValues();
        node2X = n2Vals.Item1;
        node2Z = n2Vals.Item2;
        if (diagonal)
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
    public Tuple<int, int> GetNode(Node node)
    {
        if (node.GetNodeValues().Item1 == node1X && node.GetNodeValues().Item2 == node1Z)
        {
            return new Tuple<int,int>(node2X, node2Z);
        }
        else
        {
            return new Tuple<int, int>(node1X, node1Z);
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
}
