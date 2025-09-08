using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
/// <summary>
/// DropSystem is a singleton class that manage the drops in the game.
/// It have one action to subscribe to: OnDropPickedUp.
/// OnDropPickedUp is triggered when a drop is picked up by the player.
/// It takes two parameters: the first is the random drop from the available drops, and the second is the second drop.
/// </summary>
public class DropSystem : MonoBehaviour
{
    // Singleton instance
    public static DropSystem Instance { get; private set; }
    [Header("Drop Settings")]
    [Tooltip("The chance of dropping an item from enemies")]
    [SerializeField] public int dropChance = 50;
    [Tooltip("The amount of health restored to the player when salvaging a drop.")]
    [SerializeField] public float salvageAmount = 10;
    [Tooltip("The list of the drops in the game")]
    public List<DropItemBase> availableDrops = new List<DropItemBase>();
    [Tooltip("The prefab for the drop pickup")]
    [SerializeField]
    public GameObject dropPickupPrefab;
    [Tooltip("The action that is triggered when a drop is picked up")]
    public Action<DropItemBase, DropItemBase> OnDropPickedUp;

    // List of drops that have been picked up (For later use)
    private List<DropItemBase> dropPickedup = new List<DropItemBase>();
    [Tooltip("The number of items dropped this run")]
    private int droppedItemThisRun = 0;
    /// <summary> Get the number of items dropped this run </summary>
    public int GetDroppedItemThisRun() => droppedItemThisRun;
    /// <summary> Get the chance of dropping an item from enemies </summary>
    public int GetDropChance() => dropChance;
    // <summary> Set the chance of dropping an item from enemies </summary>
    public void SetDropChance(int val) => dropChance = val;

    /// <summary>
    /// It sets the instance of the DropSystem class. And allow only one instance of the class.
    /// </summary>
    private void Awake()
    {
        // Only one instance of DropSystem should be there
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    /// <summary>
    /// It tries to drop an item from enemies.
    /// If the chance is not met, it will not drop an item.
    /// This is called when an enemy is defeated.
    /// </summary>
    public void TryDropItem(Vector3 position)
    {
        if (availableDrops.Count == 0) return;
        // Check the chance of dropping an drop
        if (UnityEngine.Random.Range(1, 101) > dropChance) return;
        SpawnDropPickup(position);

    }
    /// <summary>
    /// Spawns the drop pickup at the given position.
    /// It will spawn the drop pickup prefab and increment the dropped item count.
    /// </summary>
    private void SpawnDropPickup(Vector3 position)
    {
        if (dropPickupPrefab == null || availableDrops.Count == 0)
            return;

        Vector3 spawnPos = position + Vector3.up * 0.5f;
        Instantiate(dropPickupPrefab, spawnPos, Quaternion.identity);

        droppedItemThisRun++;
    }
    /// <summary>
    /// it selects a random drop from the available drops and shows it to the player.
    /// It will trigger the OnDropPickedUp action with the selected drop and the second drop.
    /// This is called from DropPickup.cs when the player picks up the drop.
    /// </summary>
    public void ShowDropSelection(Vector3 pickupPosition)
    {
        // Get two random drops
        DropItemBase option1 = GetRandomDrop();
        DropItemBase option2 = GetRandomDrop();
        if (option1 == null || option2 == null) return;
        // track the drops
        dropPickedup.Add(option1);
        dropPickedup.Add(option2);

        OnDropPickedUp?.Invoke(option1, option2);

        Debug.Log($"Drop picked up! {option1.GetDropName()} vs {option2.GetDropName()}");
        Debug.Log($"At this point, the ui should react to this event and then call the function DropSystem.Instance.SelectDropsOption(option1, option2); to activate the drops or salvage the drops by calling DropSystem.Instance.SalvageDrop();");
    }
    /// <summary>
    /// This is a helper function to get a random drop from the available drops.
    /// It will return null if there are no drops in the available drops list.
    /// </summary>
    private DropItemBase GetRandomDrop()
    {
        if (availableDrops.Count == 0)
            return null;

        int randomRange = UnityEngine.Random.Range(1, 101);
        List<DropItemBase> possibleDrops = availableDrops.Where(drop => drop.GetRarity().dropChance >= randomRange).ToList();
        if (possibleDrops.Count == 0)
        {
            // if for some reason we don't have any drops with the chance we want,return null as we don't have a drop 
            Debug.Log("No drops with the chance we want found!");
            return null;
        }

        // if we have drops with the chance we want, return a random from the possible drops
        return possibleDrops[UnityEngine.Random.Range(0, possibleDrops.Count)];
    }
    /// <summary>
    /// This function will be used to select a drop from the available drops.
    /// It will activate the drop 
    /// This often called from the ui to select a drop.
    /// </summary>
    public void SelectDropsOption(DropItemBase firstDrop, DropItemBase secondDrop = null)
    {
        // for now we will simple just activate the drop

        firstDrop.Activate();
        if (secondDrop != null) secondDrop.Activate();
    }
    /// <summary>
    /// Salvage a drop from the player.
    /// </summary>
    public void SalvageDrop()
    {
        if (PlayerController.instance == null || PlayerController.instance.GetHag() == null) return;
        // Add specified amount of health to the player
        PlayerController.instance.GetHag().health.AddHealth(salvageAmount);
    }
}
