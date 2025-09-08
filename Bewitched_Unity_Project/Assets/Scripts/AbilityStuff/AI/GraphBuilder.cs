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
[Serializable]
public class GraphBuilder : MonoBehaviour
{
    /// <summary>
    /// Priority queue class for A* search itself and ordering
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PriorityQueue<T>
    {
        [Tooltip("Priority queue dictionary")]
        private SortedDictionary<int, List<T>> priorityQueue;

        /// <summary>
        /// Constructor
        /// </summary>
        public PriorityQueue()
        {
            priorityQueue = new SortedDictionary<int, List<T>>();
        }

        /// <summary>
        /// Add an item to the queue
        /// </summary>
        /// <param name="item"> Item in the queue </param>
        /// <param name="cost"> Priority of item </param>
        public void Enqueue(T item, int cost)
        {
            if (!priorityQueue.ContainsKey(cost))
            {
                priorityQueue[cost] = new List<T>();
            }
            priorityQueue[cost].Add(item);
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
        /// Dequeue an item from the queue
        /// </summary>
        /// <returns> Dequeued item </returns>
        public T Dequeue()
        {
            if (IsEmpty()) return default(T);

            int lowest = priorityQueue.Keys.First();
            T item = priorityQueue[lowest][0];
            priorityQueue[lowest].RemoveAt(0);

            if (priorityQueue[lowest].Count == 0)
            {
                priorityQueue.Remove(lowest);
            }

            return item;
        }

        /// <summary>
        /// Checks if an item exists
        /// </summary>
        /// <param name="item"> Item looking for </param>
        /// <param name="f"> F value of node </param>
        /// <returns> True if node exists </returns>
        public bool Contains(T item, int f)
        {
            if (priorityQueue.ContainsKey(f))
            {
                if (priorityQueue[f].Contains(item))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Replaces a item in the priority queue based on a new priority
        /// </summary>
        /// <param name="item"> Item to replace </param>
        /// <param name="oldVal"> Old value </param>
        /// <param name="newVal"> New value </param>
        public void Replace(T item, int oldVal, int newVal)
        {
            priorityQueue[oldVal].Remove(item);
            if (priorityQueue[oldVal].Count == 0)
            {
                priorityQueue.Remove(oldVal);
            }

            Enqueue(item, newVal);
        }
    }

    [Header("Graph Build Settings")]
    [Tooltip("Graph name")]
    [SerializeField] string graphName;

    [Tooltip("Square side length to build graph")]
    [SerializeField] float buildLength;

    [Tooltip("Distance between points (setting below 5 is very costly)")]
    [SerializeField] int pointDistance = 5;

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
    [SerializeField] SerializableDictionary<int, SerializableDictionary<int, Node>> nodeDictionary = new SerializableDictionary<int, SerializableDictionary<int, Node>>();

    [Tooltip("Singleton")]
    public static GraphBuilder instance { get; private set; }

    [Tooltip("Dictionary of positions to vertex indexes")]
    SerializableDictionary<Tuple<int, int>, int> vertexPositions = new SerializableDictionary<Tuple<int, int>, int>();

    [Tooltip("Priority queue of all enemies in scene by their pathfinding priority")]
    PriorityQueue<Enemy> enemyQueue;

    // Start is called before the first frame update
    void Start()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        Debug.Log(nodeDictionary.Count);

        //StartCoroutine(HandleSearching()); What was causing long start time
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
        if (graphMesh != null)
        {
            graphMesh.Clear();
        }
        else
        {
            graphMesh = new Mesh();
        }

        vertexPositions = new SerializableDictionary<Tuple<int, int>, int>();
        string path = "Assets/Prefabs/Meshes/" + graphName + ".asset";

#if UNITY_EDITOR
        Mesh existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existingMesh != null)
        {
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();
        }
#endif
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

        List<Vector3> vertices = new List<Vector3>();

        int validNodes = 0;
        for (int x = (int)(-buildLength * 5); x < (int)(buildLength * 5); x+=pointDistance)
        {
            SerializableDictionary<int, Node> zPositions = new SerializableDictionary<int, Node>();
            nodeDictionary[x] = zPositions;

            for (int z = (int)(-buildLength * 5); z < (int)(buildLength * 5); z+=pointDistance)
            {
                Node newNode = Node.Create(x, z, pointDistance, floorLayer, wallLayer);
                if (newNode != null)
                {
                    zPositions[z] = newNode;
                    FillVertices(x, z);
                    vertices.Add(newNode.GetRealPosition());
                    vertexPositions[new Tuple<int, int>(x, z)] = validNodes;
                    validNodes++;
                }
            }

            if (zPositions.Count == 0)
            {
                nodeDictionary.Remove(x);
            }
        }
        graphMesh.vertices = vertices.ToArray();
        BuildMeshTriangles();
        Debug.Log(nodeDictionary.Count);

        meshFilter.sharedMesh = graphMesh;

#if UNITY_EDITOR // saves the mesh locally

        string path = "Assets/Prefabs/Meshes/" + graphName + ".asset";
        AssetDatabase.CreateAsset(graphMesh, path);
        AssetDatabase.SaveAssets();

#endif

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

    /// <summary>
    /// Builds the triangles for the mesh
    /// </summary>
    public void BuildMeshTriangles()
    {
        List<int> triangles = new List<int>();

        foreach (int x in nodeDictionary.Keys)
        {
            foreach (int z in nodeDictionary[x].Keys)
            {
                if (nodeDictionary.ContainsKey(x + pointDistance) && nodeDictionary[x+pointDistance].ContainsKey(z) && nodeDictionary[x].ContainsKey(z+pointDistance) && nodeDictionary[x+pointDistance].ContainsKey(z+pointDistance)) // If quad exists
                {
                    int TLIndex = GetVertexIndex(new Tuple<int, int>(x, z));
                    int BLIndex = GetVertexIndex(new Tuple<int, int>(x + pointDistance, z));
                    int TRIndex = GetVertexIndex(new Tuple<int, int>(x, z + pointDistance));
                    int BRIndex = GetVertexIndex(new Tuple<int, int>(x + pointDistance, z + pointDistance));

                    if (TLIndex != -1 && BLIndex != -1 && TRIndex != -1 && BRIndex != -1)
                    {
                        triangles.Add(TLIndex);
                        triangles.Add(BLIndex);
                        triangles.Add(TRIndex);

                        triangles.Add(TRIndex);
                        triangles.Add(BLIndex);
                        triangles.Add(BRIndex);
                    }
                }
            }
        }

        graphMesh.triangles = triangles.ToArray();

        graphMesh.RecalculateNormals();
        graphMesh.RecalculateBounds();
    }

    /// <summary>
    /// Gets the vertex index from a position tuple
    /// </summary>
    /// <param name="posVals"> Tuple of position values </param>
    /// <returns> Index if it exists, -1 otherwise </returns>
    int GetVertexIndex(Tuple<int, int> posVals)
    {
        if (vertexPositions.ContainsKey(posVals))
        {
            return vertexPositions[posVals];
        }
        return -1;
    }

    /// <summary>
    /// Finds the closest node to a point
    /// </summary>
    /// <param name="position"> Position to search from </param>
    /// <returns> Closest node if it exists </returns>
    public Node FindClosestNode(Vector3 position)
    {
        int xInt = (int)(position.x * 10);
        List<int> xList = new List<int>(nodeDictionary.Keys);
        int xPos = BinaryCoordinateSearch(xInt, xList);

        int zInt = (int)(position.z * 10);
        List<int> zList = new List<int>(nodeDictionary[xPos].Keys);
        int zPos = BinaryCoordinateSearch(zInt, zList);

        if (xPos == -1 || zPos == -1) return null;

        return nodeDictionary[xPos][zPos];
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

        PriorityQueue<Node> nodesToSearch = new PriorityQueue<Node>();
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
                Tuple<int, int> dictPos = sVertex.GetNode(currentNode);
                Node successor = GetNodeFromTuple(dictPos);
                if (successor != null)
                {

                    if (Vector3.Distance(successor.GetRealPosition(), destination) <= enemy.minStopDistance) // If in range, add node to path and end
                    {
                        path.SetPathVertex(successor, sVertex);
                        path.SetDestination(successor);
                        path.CalculatePath();
                        Debug.Log(successor.GetRealPosition());
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
        }

        return path;
    }

    /// <summary>
    /// Binary search for closest number in list
    /// </summary>
    /// <param name="targetNum"> Target value </param>
    /// <param name="values"> List of numbers </param>
    /// <returns> Integer closest to target </returns>
    public int BinaryCoordinateSearch(int targetNum, List<int> values)
    {
        int lower = values[0];
        int higher = values[values.Count - 1];

        foreach(int i in values)
        {
            if (i == targetNum)
            {
                return targetNum;
            }
            else if (i < targetNum)
            {
                lower = i;
            }
            else
            {
                higher = i;
            }
        }

        if (higher - targetNum > targetNum - lower)
        {
            return lower;
        }
        else
        {
            return higher;
        }
    }

    /// <summary>
    /// Returns the node at a given tuple x/z value
    /// </summary>
    /// <param name="posVals"> Given values </param>
    /// <returns> The node present there </returns>
    public Node GetNodeFromTuple(Tuple<int, int> posVals)
    {
        if (nodeDictionary.ContainsKey(posVals.Item1))
        {
            if (nodeDictionary[posVals.Item1].ContainsKey(posVals.Item2))
            {
                return nodeDictionary[posVals.Item1][posVals.Item2];
            }
        }
        return null;
    }

    /// <summary>
    /// Runs across frames - collects enemy and queues them by priority
    /// A* search is then run for each character
    /// After 0.5 seconds, run again
    /// </summary>
    /// <returns></returns>
    public IEnumerator HandleSearching()
    {
        enemyQueue = new PriorityQueue<Enemy>();
        Enemy[] enemies = FindObjectsOfType<Enemy>();

        foreach (Enemy enemy in enemies)
        {
            enemyQueue.Enqueue(enemy, enemy.pathfindingPriority);
        }

        while (!enemyQueue.IsEmpty())
        {
            Enemy enemy = enemyQueue.Dequeue();
            enemy.FindPath();
        }

        yield return new WaitForSeconds(0.5f);
        StartCoroutine(HandleSearching());
    }
}
