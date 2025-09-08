using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEditor;

/// <summary>
/// This is a gambit
/// NavMesh does not allow me to calculate the pathfinding I have been envisioning
/// It's either flat out impossible to do or SUPER costly at runtime
/// So I am making my own graph
/// With it's own A* search function
/// Except this time when enemies make paths, it makes every point along it more expensive
/// depending on how close the enemy is
/// Also makes an area around the player untraversable - only attacks will allow entry
/// But it also does not let the character push them (the problem with the NavMeshObstacle approach)
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class GraphBuilder : MonoBehaviour
{
    public class PriorityQueue
    {
        [Tooltip("Priority queue dictionary")]
        private SortedDictionary<int, List<Node>> priorityQueue;

        /// <summary>
        /// Constructor
        /// </summary>
        public PriorityQueue()
        {
            priorityQueue = new SortedDictionary<int, List<Node>>();
        }

        /// <summary>
        /// Add a node to the queue
        /// </summary>
        /// <param name="node"> Node in the queue </param>
        /// <param name="cost"> Priority of node </param>
        public void Enqueue(Node node, int cost)
        {
            if (!priorityQueue.ContainsKey(cost))
            {
                priorityQueue[cost] = new List<Node>();
            }
            priorityQueue[cost].Add(node);
        }

        /// <summary>
        /// Checks if the queue is empty
        /// </summary>
        /// <returns> True if empty </returns>
        public bool IsEmpty()
        {
            if (priorityQueue.Count == 0) return true;
            return false;
        }

        /// <summary>
        /// Dequeue a node from the queue
        /// </summary>
        /// <returns> Dequeued node </returns>
        public Node Dequeue()
        {
            if (IsEmpty()) return null;

            int lowest = priorityQueue.Keys.First();
            Node node = priorityQueue[lowest][0];
            priorityQueue[lowest].RemoveAt(0);

            if (priorityQueue[lowest].Count == 0)
            {
                priorityQueue.Remove(lowest);
            }

            return node;
        }

        /// <summary>
        /// Checks if a node exists
        /// </summary>
        /// <param name="node"> Node looking for </param>
        /// <param name="f"> F value of node </param>
        /// <returns> True if node exists </returns>
        public bool Contains(Node node, int f)
        {
            if (priorityQueue.ContainsKey(f))
            {
                if (priorityQueue[f].Contains(node))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Replaces a node in the priority queue based on a new priority
        /// </summary>
        /// <param name="node"> Node to replace </param>
        /// <param name="oldVal"> Old value </param>
        /// <param name="newVal"> New value </param>
        public void Replace(Node node, int oldVal, int newVal)
        {
            priorityQueue[oldVal].Remove(node);
            if (priorityQueue[oldVal].Count == 0)
            {
                priorityQueue.Remove(oldVal);
            }

            Enqueue(node, newVal);
        }
    }

    [Header("Graph Build Settings")]
    [Tooltip("Square side length to build graph")]
    [SerializeField] float buildLength;

    [Tooltip("Distance between points")]
    [SerializeField] int pointDistance = 1;

    [Tooltip("Mesh Filter")]
    [SerializeField] MeshFilter meshFilter;

    [Tooltip("Mesh Renderer")]
    [SerializeField] MeshRenderer meshRenderer;

    [Tooltip("Mesh the graph creates")]
    Mesh graphMesh;

    [Tooltip("Floor Layer")]
    [SerializeField] LayerMask floorLayer;

    [Tooltip("Wall Layer")]
    [SerializeField] LayerMask wallLayer;

    [Tooltip("Dual dictionary holding coordinate values")]
    Dictionary<int, Dictionary<int, Node>> nodeDictionary = new Dictionary<int, Dictionary<int, Node>>();

    [Tooltip("Singleton")]
    public static GraphBuilder instance;

    // Start is called before the first frame update
    void Start()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        if (nodeDictionary.Count == 0)
        {
            CreateGraph();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// Deletes the graph
    /// </summary>
    [ContextMenu("Destroy Graph")]
    public void DestroyGraph()
    {
        nodeDictionary.Clear();
        graphMesh = new Mesh();
    }

    /// <summary>
    /// Creates the graph of points with vertices for our map
    /// </summary>
    [ContextMenu("Create Graph")]
    public void CreateGraph()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        DestroyGraph();

        Vector3[] vertices = new Vector3[(int)(buildLength * buildLength * 100)];

        int validNodes = 0;
        for (int x = (int)(-buildLength * 5); x < (int)(buildLength * 5); x++)
        {
            Dictionary<int, Node> zPositions = new Dictionary<int, Node>();
            nodeDictionary[x] = zPositions;

            for (int z = (int)(-buildLength * 5); z < (int)(buildLength * 5); z+=pointDistance)
            {
                Node newNode = Node.Create(x, z, pointDistance, floorLayer, wallLayer);
                if (newNode != null)
                {
                    zPositions[z] = newNode;
                    FillVertices(x, z);
                    vertices[validNodes] = newNode.GetRealPosition();
                    validNodes++;
                }
            }

            if (zPositions.Count == 0)
            {
                nodeDictionary.Remove(x);
            }
        }
        graphMesh.vertices = vertices;
        Debug.Log(validNodes);

        meshFilter.mesh = graphMesh;
    }

    /// <summary>
    /// Fills the vertices for the graph going backwards
    /// </summary>
    /// <param name="x"> X position in dictionary </param>
    /// <param name="z"> Z position in dictionary </param>
    public void FillVertices(int x, int z)
    {
        Node originNode = nodeDictionary[x][z];


        // Try node with z-1
        if (nodeDictionary[x].ContainsKey(z-pointDistance))
        {
            Vertex vertex = new Vertex(originNode, nodeDictionary[x][z - pointDistance], false);
            originNode.AssignVertex(vertex);
            nodeDictionary[x][z - pointDistance].AssignVertex(vertex);
        }

        if (nodeDictionary.ContainsKey(x- pointDistance))
        {
            // Try node with z-1 and x-1
            if (nodeDictionary[x- pointDistance].ContainsKey(z - pointDistance))
            {
                Vertex vertex = new Vertex(originNode, nodeDictionary[x- pointDistance][z - pointDistance], true);
                originNode.AssignVertex(vertex);
                nodeDictionary[x- pointDistance][z - pointDistance].AssignVertex(vertex);
            }

            // Try node with x-1
            if (nodeDictionary[x- pointDistance].ContainsKey(z))
            {
                Vertex vertex = new Vertex(originNode, nodeDictionary[x- pointDistance][z], false);
                originNode.AssignVertex(vertex);
                nodeDictionary[x- pointDistance][z].AssignVertex(vertex);
            }

            // Try node with x-1 and z+1
            if (nodeDictionary[x- pointDistance].ContainsKey(z + pointDistance))
            {
                Vertex vertex = new Vertex(originNode, nodeDictionary[x- pointDistance][z + pointDistance], true);
                originNode.AssignVertex(vertex);
                nodeDictionary[x- pointDistance][z + pointDistance].AssignVertex(vertex);
            }
        }
    }

    public void BuildMeshTriangles()
    {

    }

    /// <summary>
    /// Finds the closest node to a point
    /// </summary>
    /// <param name="position"> Position to search from </param>
    /// <returns> Closest node if it exists </returns>
    public Node FindClosestNode(Vector3 position)
    {
        int x = (int)(position.x * 10 / pointDistance);
        int z = (int)(position.z * 10 / pointDistance);

        if (nodeDictionary.ContainsKey(x))
        {
            if (nodeDictionary[x].ContainsKey(z))
            {
                return nodeDictionary[x][z];
            }
        }
        return null;
    }

    public NavPath AStarSearch(Enemy enemy, Vector3 destination)
    {
        Node origin = FindClosestNode(enemy.transform.position);

        if (origin == null)
        {
            return null;
        }

        NavPath path = new NavPath();
        path.SetOrigin(origin);

        if (Vector3.Distance(enemy.transform.position, destination) <= enemy.minStopDistance) // Ensure we are not at location already
        {
            path.SetDestination(origin);
            return path;
        }

        PriorityQueue nodesToSearch = new PriorityQueue();
        nodesToSearch.Enqueue(origin, 0);

        // First tuple val is actual distance so far, second is estimated is distance to go
        Dictionary<Node, Tuple<int, int>> nodesSearched = new Dictionary<Node, Tuple<int, int>>();

        nodesSearched[origin] = new Tuple<int, int>(0, 0);

        while (!nodesToSearch.IsEmpty())
        {
            Node currentNode = nodesToSearch.Dequeue();

            List<Vertex> successors = currentNode.GetVertices();

            foreach (Vertex sVertex in successors)
            {
                Node successor = sVertex.GetNode(currentNode);

                if (Vector3.Distance(successor.GetRealPosition(), destination) <= enemy.minStopDistance) // If in range, add node to path and end
                {
                    path.SetPathVertex(successor, sVertex);
                    path.SetDestination(successor);
                    path.CalculatePath();
                    return path;
                }

                int cost = nodesSearched[currentNode].Item1 + (int)(sVertex.GetDistance()) + successor.GetCost();
                int h = (int)(Vector3.Distance(successor.GetRealPosition(), destination) * 10 / pointDistance);
                int f = cost + h;

                if (nodesSearched.ContainsKey(successor)) // If we have seen this node before
                {
                    if (f < nodesSearched[successor].Item2) // If less than previous value
                    {
                        if (nodesToSearch.Contains(successor, nodesSearched[successor].Item2)) // If the node is in the queue right now
                        {
                            nodesToSearch.Replace(successor, nodesSearched[successor].Item2, f);
                        }

                        // Set cost so far for node and priority estimate
                        nodesSearched[successor] = new Tuple<int, int>(cost, f);
                        // Set path parent
                        path.SetPathVertex(successor, sVertex);
                    }
                }
                else // If we have not seen this node before, add it to dictionary and queue
                {
                    nodesToSearch.Enqueue(successor, f);
                    // Set cost so far for node and priority estimate
                    nodesSearched[successor] = new Tuple<int, int>(cost, f);
                    // Set path parent
                    path.SetPathVertex(successor, sVertex);
                }
            }
        }

        return path;
    }
}
