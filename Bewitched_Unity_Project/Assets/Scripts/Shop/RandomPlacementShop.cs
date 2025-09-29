using System.Collections.Generic;
using UnityEngine;


/// This class will spawn a shop alter at a random point in the map and create a graph builder
public class RandomPlacementShop : MonoBehaviour
{
    [Header("Shop Settings")]
    [SerializeField, Tooltip("The prefab of the shop alter model.")]
    private GameObject shopAlterPrefab;
    [SerializeField, Tooltip("The list of the shop alters points to get a random point and then spawn the shop alter.")]
    private List<GameObject> shopAltersPoints = new List<GameObject>();
    /// <summary>
    /// Choose a random shop alter point and spawn the shop alter
    /// Then create a graph builder
    /// </summary>
    private void Start()
    {
        // Select a random shop alter point
        GameObject shopAlterPoint = shopAltersPoints[Random.Range(0, shopAltersPoints.Count)];
        // Spawn the shop alter
        Instantiate(shopAlterPrefab, shopAlterPoint.transform.position, shopAlterPoint.transform.rotation);
        // Then create a graph builder
        GraphBuilder.instance.CreateGraph();
    }
}
