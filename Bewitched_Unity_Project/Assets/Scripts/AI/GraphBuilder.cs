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
    [Header("Graph Build Settings")]
    [Tooltip("Graph name")]
    [SerializeField] string graphName;

    [Tooltip("Square side length to build graph")]
    [SerializeField] float buildLength;

    [Tooltip("Distance between points (setting below 5 is very costly)")]
    [SerializeField] int pointDistance = 5;

    [Tooltip("How many nodes can be searched before the next frame is played")]
    [SerializeField] int nodesSearchedPerFrame = 60;
    [SerializeField, Tooltip("Maximum height to scan for floors")]
    private float maxFloorHeight = 50f;

    [SerializeField,Tooltip("Minimum height difference between floors")]
    private float minFloorSeparation = 2f;

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
    [SerializeField] SerializableDictionary<int, SerializableDictionary<int, SerializableDictionary<int, Node>>> nodeDictionary = new SerializableDictionary<int, SerializableDictionary<int, SerializableDictionary<int, Node>>>();

    [Tooltip("Singleton")]
    public static GraphBuilder instance { get; private set; }

    [Tooltip("Dictionary of positions to vertex indexes")]
    SerializableDictionary<Tuple<int, int, int>, int> vertexPositions = new SerializableDictionary<Tuple<int, int, int>, int>();

    [Tooltip("Priority queue of all enemies in scene by their pathfinding priority")]
    PriorityQueue<Enemy> enemyQueue;

    [Tooltip("If an enemy is searching currently")]
    bool searching = false;

    [Tooltip("If we are testing")]
    [SerializeField] bool testing = false;

    [Tooltip("Destination marker object")]
    [SerializeField] GameObject testDestinationObj;

    [Tooltip("Testing Enemy")]
    [SerializeField] Enemy testingEnemy;

    [Tooltip("Searched node prefab")]
    [SerializeField] GameObject testSearchedNode;

    [Tooltip("Created objects for cleanup")]
    List<GameObject> createdObjects;

    [Tooltip("Found destination material")]
    [SerializeField] Material greenMat;

    [Tooltip("Point of costly area")]
    [SerializeField] GameObject costlyOrigin;

    [Tooltip("Radius of costly area")]
    [SerializeField] float costlyRadius = 4;

    [Tooltip("Cost of costly area")]
    [SerializeField] int costlyAreaCost = 500;

    [Tooltip("Line renderer for path debugging")]
    [SerializeField] LineRenderer lineRenderer;

    // Start is called before the first frame update
    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        StartCoroutine(HandleSearching()); //What was causing long start time
    }
    /// <summary>
    /// Create an instance in awake since the awake function called before the start function
    /// </summary>
    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
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

        vertexPositions = new SerializableDictionary<Tuple<int, int, int>, int>();
        string path = "Assets/Prefabs/Meshes/" + graphName + ".asset";

#if UNITY_EDITOR
        Mesh existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existingMesh != null)
        {
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();
        }
#endif
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

        List<Vector3> vertices = new List<Vector3>();

        int validNodes = 0;
        for (int x = (int)(-buildLength * 5); x < (int)(buildLength * 5); x+=pointDistance)
        {
            SerializableDictionary<int, SerializableDictionary<int, Node>> zPositions = new SerializableDictionary<int, SerializableDictionary<int, Node>>();
            nodeDictionary[x] = zPositions;

            for (int z = (int)(-buildLength * 5); z < (int)(buildLength * 5); z += pointDistance)
            {
                SerializableDictionary<int, Node> yPositions = new SerializableDictionary<int, Node>();
                zPositions[z] = yPositions;
                List<Node> floorsAtPosition = Node.Create(x, z, pointDistance, floorLayer, wallLayer, maxFloorHeight, minFloorSeparation);
                foreach (Node newNode in floorsAtPosition)
                {
                    int yPos = (int)(newNode.GetPosition().y * 10);
                    yPositions[yPos] = newNode;
                    FillVertices(x, z, yPos);
                    vertices.Add(newNode.GetPosition());
                    vertexPositions[new Tuple<int, int, int>(x, z, yPos)] = validNodes;
                    validNodes++;
                    newNode.SetCreated();
                }
                // Check if this floor is far enough from existing floors
                if (yPositions.Count == 0)
                {
                    zPositions.Remove(z);
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
    /// <param name="y"> Y position in dictionary </param>
    public void FillVertices(int x, int z, int y)
    {
        Node originNode = nodeDictionary[x][z][y];


        // Try node with z-1
        if (nodeDictionary[x].ContainsKey(z - pointDistance) && nodeDictionary[x][z - pointDistance].ContainsKey(y))
        {
            Vertex vertex = new Vertex(originNode, nodeDictionary[x][z - pointDistance][y], false);
            originNode.AssignVertex(vertex);
            nodeDictionary[x][z - pointDistance][y].AssignVertex(vertex);
        }

        if (nodeDictionary.ContainsKey(x - pointDistance))
        {
            // Try node with z-1 and x-1
            if (nodeDictionary[x - pointDistance].ContainsKey(z - pointDistance) && nodeDictionary[x - pointDistance][z - pointDistance].ContainsKey(y))
            {
                Vertex vertex = new Vertex(originNode, nodeDictionary[x - pointDistance][z - pointDistance][y], true);
                originNode.AssignVertex(vertex);
                nodeDictionary[x - pointDistance][z - pointDistance][y].AssignVertex(vertex);
            }

            // Try node with x-1
            if (nodeDictionary[x - pointDistance].ContainsKey(z) && nodeDictionary[x - pointDistance][z].ContainsKey(y))
            {
                Vertex vertex = new Vertex(originNode, nodeDictionary[x - pointDistance][z][y], false);
                originNode.AssignVertex(vertex);
                nodeDictionary[x - pointDistance][z][y].AssignVertex(vertex);
            }

            // Try node with x-1 and z+1
            if (nodeDictionary[x - pointDistance].ContainsKey(z + pointDistance) && nodeDictionary[x - pointDistance][z + pointDistance].ContainsKey(y))
            {
                Vertex vertex = new Vertex(originNode, nodeDictionary[x - pointDistance][z + pointDistance][y], true);
                originNode.AssignVertex(vertex);
                nodeDictionary[x - pointDistance][z + pointDistance][y].AssignVertex(vertex);
            }
        }
        
        // Vertical connections (between floors at same X,Z)
        foreach (int otherY in nodeDictionary[x][z].Keys)
        {
            if (otherY != y)
            {
                float heightDifference = Mathf.Abs((otherY - y) / 10f);
                
                // Only connect floors that are close vertically (within minFloorSeparation)
                if (heightDifference >= minFloorSeparation && heightDifference <= minFloorSeparation * 3f)
                {
                    Node otherFloorNode = nodeDictionary[x][z][otherY];
                    Vertex verticalVertex = new Vertex(originNode, otherFloorNode, false, true); // true for vertical
                    originNode.AssignVertex(verticalVertex);
                    otherFloorNode.AssignVertex(verticalVertex);
                }
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
                foreach (int y in nodeDictionary[x][z].Keys)
                {
                    if (nodeDictionary.ContainsKey(x + pointDistance) && nodeDictionary[x + pointDistance].ContainsKey(z) && nodeDictionary[x].ContainsKey(z + pointDistance) && nodeDictionary[x + pointDistance].ContainsKey(z + pointDistance) && nodeDictionary[x + pointDistance][z].ContainsKey(y) && nodeDictionary[x][z + pointDistance].ContainsKey(y) && nodeDictionary[x + pointDistance][z + pointDistance].ContainsKey(y)) // If quad exists
                    {
                        int TLIndex = GetVertexIndex(new Tuple<int, int, int>(x, z, y));
                        int BLIndex = GetVertexIndex(new Tuple<int, int, int>(x + pointDistance, z, y));
                        int TRIndex = GetVertexIndex(new Tuple<int, int, int>(x, z + pointDistance, y));
                        int BRIndex = GetVertexIndex(new Tuple<int, int, int>(x + pointDistance, z + pointDistance, y));

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
    int GetVertexIndex(Tuple<int, int, int> posVals)
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
        if (nodeDictionary == null || nodeDictionary.Count == 0)
            return null;

        int xInt = (int)(position.x * 10);
        List<int> xList = new List<int>(nodeDictionary.Keys);
        int xPos = BinaryCoordinateSearch(xInt, xList);
        if (!nodeDictionary.ContainsKey(xPos))
            return null;

        int zInt = (int)(position.z * 10);
        List<int> zList = new List<int>(nodeDictionary[xPos].Keys);
        int zPos = BinaryCoordinateSearch(zInt, zList);
        if (!nodeDictionary[xPos].ContainsKey(zPos))
            return null;

        int yInt = (int)(position.y * 10);
        List<int> yList = new List<int>(nodeDictionary[xPos][zPos].Keys);
        int yPos = BinaryCoordinateSearch(yInt, yList);
        if (!nodeDictionary[xPos][zPos].ContainsKey(yPos))
            return null;

        if (xPos == -1 || zPos == -1 || yPos == -1) return null;
        
        return nodeDictionary[xPos][zPos][yPos];

    }

    //public NavPath AStarSearch(Enemy enemy, Vector3 destination)
    //{
    //    Node origin = FindClosestNode(enemy.transform.position);

    //    if (origin == null)
    //    {
    //        return null;
    //    }

    //    NavPath path = new NavPath();
    //    path.SetOrigin(origin);

    //    if (Vector3.Distance(enemy.transform.position, destination) <= enemy.minStopDistance) // Ensure we are not at location already
    //    {
    //        path.SetDestination(origin);
    //        return path;
    //    }

    //    PriorityQueue<Node> nodesToSearch = new PriorityQueue<Node>();
    //    nodesToSearch.Enqueue(origin, 0);

    //    // First tuple val is actual distance so far, second is estimated is distance to go
    //    Dictionary<Node, Tuple<int, int>> nodesSearched = new Dictionary<Node, Tuple<int, int>>();

    //    nodesSearched[origin] = new Tuple<int, int>(0, 0);

    //    while (!nodesToSearch.IsEmpty())
    //    {
    //        Node currentNode = nodesToSearch.Dequeue();

    //        List<Vertex> successors = currentNode.GetVertices();

    //        foreach (Vertex sVertex in successors)
    //        {
    //            Tuple<int, int> dictPos = sVertex.GetNode(currentNode);
    //            Node successor = GetNodeFromTuple(dictPos);
    //            if (successor != null)
    //            {

    //                if (Vector3.Distance(successor.GetRealPosition(), destination) <= enemy.minStopDistance) // If in range, add node to path and end
    //                {
    //                    path.SetPathVertex(successor, sVertex);
    //                    path.SetDestination(successor);
    //                    path.CalculatePath();
    //                    Debug.Log(successor.GetRealPosition());
    //                    return path;
    //                }

    //                int cost = nodesSearched[currentNode].Item1 + (int)(sVertex.GetDistance()) + successor.GetCost();
    //                int h = (int)(Vector3.Distance(successor.GetRealPosition(), destination) * 10 / pointDistance);
    //                int f = cost + h;

    //                if (nodesSearched.ContainsKey(successor)) // If we have seen this node before
    //                {
    //                    if (f < nodesSearched[successor].Item2) // If less than previous value
    //                    {
    //                        if (nodesToSearch.Contains(successor, nodesSearched[successor].Item2)) // If the node is in the queue right now
    //                        {
    //                            nodesToSearch.Replace(successor, nodesSearched[successor].Item2, f);
    //                        }

    //                        // Set cost so far for node and priority estimate
    //                        nodesSearched[successor] = new Tuple<int, int>(cost, f);
    //                        // Set path parent
    //                        path.SetPathVertex(successor, sVertex);
    //                    }
    //                }
    //                else // If we have not seen this node before, add it to dictionary and queue
    //                {
    //                    nodesToSearch.Enqueue(successor, f);
    //                    // Set cost so far for node and priority estimate
    //                    nodesSearched[successor] = new Tuple<int, int>(cost, f);
    //                    // Set path parent
    //                    path.SetPathVertex(successor, sVertex);
    //                }
    //            }
    //        }
    //    }

    //    return path;
    //}

    /// <summary>
    /// Search function for finding the fastest route to a destination
    /// Made it an enumerator so we can split the search across frames for quicker handling
    /// </summary>
    /// <param name="enemy"> Enemy looking for path </param>
    /// <param name="destination"> Destination location </param>
    /// <returns></returns>
    public IEnumerator AStarSearch(Enemy enemy, Vector3 destination)
    {
        searching = true;

        PriorityQueue<Node> openSet = new PriorityQueue<Node>();
        Node origin = FindClosestNode(enemy.transform.position);
        Node targetNode = FindClosestNode(destination);

        if (origin == null || targetNode == null)
        {
            searching = false;
            enemy.SetUsingSearch(false);
            enemy.SetPath(null);
            enemy.ValidatePoint(); // Quick set path state to unset
            StartCoroutine(RetryPath(enemy));
            Debug.Log("PATHNOTFOUND");
            yield break;
        }

        int nodesSearched = 0;

        NavPath path = new NavPath();
        path.SetOrigin(origin);
        openSet.Enqueue(origin, 0);
        List<Node> closedSet = new List<Node>();
        Dictionary<Vector3, float> gscore = new Dictionary<Vector3, float>();

        gscore[origin.GetPosition()] = 0;

        Dictionary<Vector3, float> fscore = new Dictionary<Vector3, float>();
        fscore[origin.GetPosition()] = Vector3.Distance(origin.GetPosition(), destination);

        while (!openSet.IsEmpty())
        {
            Node current = openSet.Dequeue();
            closedSet.Add(current);

            nodesSearched++;

            if (targetNode == current) // If in range, add node to path and end
            {
                path.SetDestination(current);
                path.CalculatePath();
                enemy.SetPath(path);
                searching = false;
                enemy.SetUsingSearch(false);

                if (!enemy.ValidatePoint())
                {
                    StartCoroutine(RetryPath(enemy));
                    yield break;
                }

                yield break;
            }

            foreach (Vertex vertex in current.GetVertices())
            {
                Node neighbor = nodeDictionary[vertex.GetNode(current).Item1][vertex.GetNode(current).Item2][vertex.GetNode(current).Item3];

                if (closedSet.Contains(neighbor))
                    continue;

                float tentativeGScore = gscore[current.GetPosition()] +
                        Vector3.Distance(current.GetPosition(), neighbor.GetPosition());

                float neighborGScore;
                if (!gscore.TryGetValue(neighbor.GetPosition(), out neighborGScore))
                    neighborGScore = float.PositiveInfinity;

                if (tentativeGScore < neighborGScore)
                {
                    path.SetPathVertex(neighbor, vertex);
                    gscore[neighbor.GetPosition()] = tentativeGScore;
                    fscore[neighbor.GetPosition()] = gscore[neighbor.GetPosition()] + Vector3.Distance(neighbor.GetPosition(), destination);

                    openSet.Enqueue(neighbor, (int)fscore[neighbor.GetPosition()]);
                }
            }

            if (nodesSearched % nodesSearchedPerFrame == 0) // If we have reached the threshold
            {
                yield return null; // Go to next frame
            }
        }

        searching = false;
        enemy.SetUsingSearch(false);
        enemy.SetPath(null);
        enemy.ValidatePoint(); // Quick set path state to unset
        StartCoroutine(RetryPath(enemy));
        Debug.Log("PATHNOTFOUND");
        yield break;

    }

    /// <summary>
    /// Binary search for closest number in list
    /// </summary>
    /// <param name="targetNum"> Target value </param>
    /// <param name="values"> List of numbers </param>
    /// <returns> Integer closest to target </returns>
    public int BinaryCoordinateSearch(int targetNum, List<int> values)
    {
        values.Sort();
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
                break; // the list already sorted 
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
    /// Returns the node at a given tuple x/z/y value
    /// </summary>
    /// <param name="posVals"> Given values </param>
    /// <returns> The node present there </returns>
    public Node GetNodeFromTuple(Tuple<int, int, int> posVals)
    {
        if (nodeDictionary.ContainsKey(posVals.Item1))
        {
            if (nodeDictionary[posVals.Item1].ContainsKey(posVals.Item2))
            {
                if (nodeDictionary[posVals.Item1][posVals.Item2].ContainsKey(posVals.Item3))
                {
                    return nodeDictionary[posVals.Item1][posVals.Item2][posVals.Item3];
                }
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
        if (testing) yield break;
        int iter = 1;

        while (true)
        {
            enemyQueue = new PriorityQueue<Enemy>();
            Enemy[] enemies = FindObjectsOfType<Enemy>();

            foreach (Enemy enemy in enemies)
            {
                if (enemy.gameObject.activeSelf)
                {
                    enemyQueue.Enqueue(enemy, enemy.pathfindingPriority);
                }
            }

            while (!enemyQueue.IsEmpty())
            {
                if (!searching)
                {
                    Enemy enemy = enemyQueue.Dequeue();
                    enemy.FindPath();
                    while (!enemy.HasSetPath())
                    {
                        if (!enemy.IsFindingPath())
                        {
                            enemy.FindPath();
                        }
                        yield return null;
                    }
                }
                yield return null;
            }
            iter++;
            yield return null;
        }
    }

    /// <summary>
    /// Function to run from editor to test A* search one step at a time
    /// </summary>
    [ContextMenu("Test AStar Search")]
    public void TestAStarSearch()
    {
        createdObjects = new List<GameObject>();

        if (costlyOrigin)
        {
            List<List<int>> costlyNodes = GetNodesInRadius(costlyOrigin, costlyRadius);
            foreach (List<int> positions in costlyNodes)
            {
                nodeDictionary[positions[0]][positions[1]][positions[2]].AddCost(costlyAreaCost);
            }
        }

        lineRenderer.positionCount = 0;

        StartCoroutine(SequentialAStar(testingEnemy, testDestinationObj.transform.position));
    }

    /// <summary>
    /// A* search except waits half a second between node jumps and instantiates objects
    /// </summary>
    /// <param name="enemy"> Enemy looking for path </param>
    /// <param name="destination"> Destination point </param>
    /// <returns> Time </returns>
    public IEnumerator SequentialAStar(Enemy enemy, Vector3 destination)
    {
        int numSearched = 0;
        Debug.Log("called");
        PriorityQueue<Node> openSet = new PriorityQueue<Node>();
        Node origin = FindClosestNode(enemy.transform.position);

        Node targetNode = FindClosestNode(destination);

        if (origin == null || targetNode == null)
        {
            CleanupTestWaste();
            yield break;
        }

        NavPath path = new NavPath();
        path.SetOrigin(origin);
        openSet.Enqueue(origin, 0);
        List<Node> closedSet = new List<Node>();
        Dictionary<Vector3, float> gscore = new Dictionary<Vector3, float>();

        gscore[origin.GetPosition()] = 0;

        Dictionary<Vector3, float> fscore = new Dictionary<Vector3, float>();
        fscore[origin.GetPosition()] = Vector3.Distance(origin.GetPosition(), destination);

        while (!openSet.IsEmpty())
        {
            Node current = openSet.Dequeue();
            closedSet.Add(current);
            numSearched++;

            Debug.Log("Current Node: " + current.GetPosition(enemy.gameObject).ToString() + " and hash code: " + current.GetHashCode().ToString() + " and cost: " + current.GetCost());
            Debug.Log("It's y position: " + current.GetYPos());

            GameObject testNode = Instantiate(testSearchedNode);
            testNode.transform.position = current.GetPosition(enemy.gameObject);
            testNode.transform.position = new Vector3(testNode.transform.position.x, testNode.transform.position.y + 1, testNode.transform.position.z);

            float costMagnitude = current.GetCost() / 1000;
            Renderer objRenderer = testNode.GetComponent<Renderer>();
            objRenderer.material.color = new Color(costMagnitude, costMagnitude, costMagnitude);

            createdObjects.Add(testNode);

            if (targetNode == current) // If in range, add node to path and end
            {
                path.SetDestination(current);
                path.CalculatePath();
                enemy.SetPath(path);
                testNode.GetComponent<MeshRenderer>().material = greenMat;
                Debug.Log("Path found in: " + numSearched.ToString() + " nodes");
                Debug.Log("Path corners: " + path.GetCornerNodes().Count);
                enemy.StartPath(false);

                lineRenderer.positionCount = path.GetCornerNodes().Count + 1;
                lineRenderer.SetPosition(0, enemy.transform.position);

                for (int i = 0; i < path.GetCornerNodes().Count; i++)
                {
                    lineRenderer.SetPosition(i+1, new Vector3(path.GetCornerNodes()[i].GetPosition().x, enemy.transform.position.y, path.GetCornerNodes()[i].GetPosition().z));
                }

                yield return new WaitForSeconds(5);
                enemy.DestroyPath();
                CleanupTestWaste();
                yield break;
            }

            foreach (Vertex vertex in current.GetVertices())
            {
                Node neighbor = nodeDictionary[vertex.GetNode(current).Item1][vertex.GetNode(current).Item2][vertex.GetNode(current).Item3];

                if (closedSet.Contains(neighbor))
                    continue;

                float tentativeGScore = gscore[current.GetPosition()] +
                        Vector3.Distance(current.GetPosition(), neighbor.GetPosition()) + neighbor.GetCost();

                float neighborGScore;
                if (!gscore.TryGetValue(neighbor.GetPosition(), out neighborGScore))
                    neighborGScore = float.PositiveInfinity;

                if (tentativeGScore < neighborGScore)
                {
                    path.SetPathVertex(neighbor, vertex);
                    gscore[neighbor.GetPosition()] = tentativeGScore;
                    fscore[neighbor.GetPosition()] = gscore[neighbor.GetPosition()] + Vector3.Distance(neighbor.GetPosition(), destination);

                    openSet.Enqueue(neighbor, (int)fscore[neighbor.GetPosition()]);
                }
            }
            yield return new WaitForSecondsRealtime(0.05f);
        }

        CleanupTestWaste();
        yield break;
    }

    /// <summary>
    /// Cleans up waste from the tests
    /// </summary>
    public void CleanupTestWaste()
    {
        foreach (GameObject obj in createdObjects)
        {
            Destroy(obj);
        }

        if (costlyOrigin)
        {
            List<List<int>> costlyNodes = GetNodesInRadius(costlyOrigin, costlyRadius);
            foreach (List<int> positions in costlyNodes)
            {
                nodeDictionary[positions[0]][positions[1]][positions[2]].AddCost(-costlyAreaCost);
            }
        }
        lineRenderer.positionCount = 0;
    }

    /// <summary>
    /// Runs the pathfinder again
    /// </summary>
    /// <param name="enemy"> Enemy finding a path </param>
    /// <returns></returns>
    private IEnumerator RetryPath(Enemy enemy)
    {
        yield return null;
        enemy.FindPath();
    }

    /// <summary>
    /// Gets all nodes within a certain radius of a position
    /// </summary>
    /// <param name="position"> Center of circle </param>
    /// <param name="radius"> Radius of circle </param>
    /// <returns> All nodes in the circle </returns>
    public List<List<int>> GetNodesInRadius(GameObject user, float radius)
    {
        List<List<int>> includedNodes = new List<List<int>>();

        int xPos = (int)(user.transform.position.x * 10);
        int zPos = (int)(user.transform.position.z * 10);

        int convRadius = (int)(radius * 10);

        for (int x = xPos - convRadius; x <= xPos + convRadius; x++)
        {
            if (nodeDictionary.ContainsKey(x))
            {
                for (int z = zPos - convRadius; z <= zPos + convRadius; z++)
                {
                    if (nodeDictionary[x].ContainsKey(z))
                    {
                        foreach (int y in nodeDictionary[x][z].Keys)
                        {
                            Vector3 dist = nodeDictionary[x][z][y].GetPosition(user) - user.transform.position;

                            if (dist.sqrMagnitude < radius)
                            {
                                List<int> positions = new List<int>();
                                positions.Add(x);
                                positions.Add(z);
                                positions.Add(y);
                                includedNodes.Add(positions);
                            }
                        }
                    }
                }
            }
        }
        return includedNodes;
    }

    /// <summary>
    /// Adds a cost to a node based on the node position
    /// </summary>
    /// <param name="position"> Position values of node </param>
    /// <param name="cost"> Cost to add to node </param>
    public void AddNodeCost(List<int> position, int cost)
    {
        nodeDictionary[position[0]][position[1]][position[2]].AddCost(cost);
    }

    /// <summary>
    /// Resets all node costs
    /// </summary>
    [ContextMenu("Reset Node Costs")]
    public void ResetAllNodes()
    {
        foreach (SerializableDictionary<int, SerializableDictionary<int, Node>> val1 in nodeDictionary.Values)
        {
            foreach (SerializableDictionary<int, Node> val2 in val1.Values)
            {
                foreach (Node node in val2.Values)
                {
                    node.ResetCost();
                }
            }
        }
    }
}
