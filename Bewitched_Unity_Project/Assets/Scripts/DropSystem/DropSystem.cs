using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
/// <summary>
/// DropSystem is a singleton class that manage the drops in the game.
/// It have one action to subscribe to: OnDropRandomDrop.
/// OnDropRandomDrop is triggered when a drop is picked up by the player and the system will select a random drop from the available drops.
/// It returns two parameters: the first is the random drop from the available drops, and the second is the second random drop.
/// </summary>
[RequireComponent(typeof(PitySystem))]
public class DropSystem : MonoBehaviour
{
    // Singleton instance
    public static DropSystem Instance { get; private set; }
    [Header("Player Upgrades")]
    [Tooltip("List of upgrades that the player has acquired.")]
    public List<DropData> playerUpgrades = new List<DropData>();
    [Header("Drop Settings")]
    [Tooltip("The chance of dropping an item from enemies")]
    [SerializeField] public int dropChance = 50;
    [Tooltip("The amount of health restored to the player when salvaging a drop.")]
    [SerializeField] public float salvageAmount = 10;
    [Tooltip("The UI screen for selecting upgrades.")]
    public GameObject upgradeSelectionUI;
    [Tooltip("The UI screen for swapping upgrades when player hits limit of upgrades.")]
    public GameObject swapUpgradeUI;
    [Tooltip("Chosen upgrade that will be added after swap is done")]
    public DropData pendingSwap;
    [Tooltip("The list of the rarities in the game")]
    [SerializeField] public List<ItemRarity> availableRarities = new List<ItemRarity>();
    [Tooltip("The list of the drops in the game")]
    public List<DropData> availableDrops = new List<DropData>();
    [Tooltip("The prefab for the drop pickup")]
    [SerializeField]
    public GameObject dropPickupPrefab;
    [Tooltip("Do you want to use the pity system?")]
    [SerializeField] private bool usePitySystem = true;
    [Tooltip("The action that is triggered when a drop is picked up")]
    public Action<DropData, DropData, DropData> OnDropRandomDrop;
    [Tooltip("The action that is triggered when the player interacts with the shop alter. This is will be called on PlayerController")]
    public Action<List<DropData>>  OnShopAlterInteract;
    [Tooltip("The number of items dropped this run")]
    private int droppedItemThisRun = 0;
    [Tooltip("A reference to the pity system")]

    private PitySystem pitySystem;
    /// <summary> Get the number of items dropped this run </summary>
    public int GetDroppedItemThisRun() => droppedItemThisRun;
    /// <summary> Get the chance of dropping an item from enemies </summary>
    public int GetDropChance() => dropChance;
    // <summary> Set the chance of dropping an item from enemies </summary>
    public void SetDropChance(int val) => dropChance = val;
    // <summary> Get whether to use the pity system </summary>
    public bool GetUsePitySystem() => usePitySystem;
    // <summary> Set whether to use the pity system </summary>
    public void SetUsePitySystem(bool val) => usePitySystem = val;

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
    /// Initialize the pity system if it is enabled
    /// </summary>
    private void Start()
    {
        if (usePitySystem)
        {
            pitySystem = GetComponent<PitySystem>();
            // Add all rarities to the pity system
            pitySystem.Initialize(availableRarities.Distinct().ToList());
        }
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
        if (UnityEngine.Random.Range(1, 101) > dropChance)
        { 
            if (usePitySystem)
            {
                // no offered rarities - increase all pity 
                pitySystem.OnUpgradesOffered(new ItemRarity());
            }
            return;
        }
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
    /// It will trigger the OnDropRandomDrop action with the three random drops
    /// This is called from DropPickup.cs when the player picks up the drop.
    /// </summary>
    public void ShowDropSelection(Vector3 pickupPosition)
    {
        // Get three random drops
        DropData option1 = GetRandomDrop();
        DropData option2 = GetRandomDrop(new List<DropData>() { option1 });
        DropData option3 = GetRandomDrop(new List<DropData>() { option1, option2 });
        if (option1 == null || option2 == null || option3 == null) return;

        if(upgradeSelectionUI != null)
        {
            upgradeSelectionUI.SetActive(true);
        }
        else
        {
            Debug.LogWarning("The upgrade selection UI is null!");
        }

        OnDropRandomDrop?.Invoke(option1, option2, option3);

        //Debug.Log($"Drop picked up! {option1.GetDropName()} vs {option2.GetDropName()} vs {option3.GetDropName()}");

    }
    /// <summary>
    /// This is a helper function to get a random drop from the available drops.
    /// It will return null if there are no drops in the available drops list.
    /// </summary>
    /// <param name="previousDrops">A list of drops that have already been picked up</param>
    public DropData GetRandomDrop(List<DropData> previousDrops = null)
    {
        if (availableDrops.Count == 0)
            return null;

        int randomRange = UnityEngine.Random.Range(1, 101);
        List<DropData> possibleDrops;
        if (usePitySystem)
        {
            possibleDrops = availableDrops.Where(drop => 
                pitySystem.GetModifiedDropChance(availableRarities[drop.GetRarityIndex()]) >= randomRange).ToList();
        }
        else
        {
            possibleDrops = availableDrops.Where(drop => 
                availableRarities[drop.GetRarityIndex()].dropChance >= randomRange).ToList();
        }
        // exclude drops that are already in the previous drops
        if (previousDrops != null)
        {
            foreach (DropData drop in previousDrops)
            {
                possibleDrops.Remove(drop);
            }
        }
        if (possibleDrops.Count == 0)
        {
            // if for some reason we don't have any drops with the chance we want, it will fallback to the highest chance drop so it gurantees this returns a drop 
            List<DropData> fallbackList = new List<DropData>(availableDrops);
            if (previousDrops != null){
                foreach (DropData drop in previousDrops)
                {
                    fallbackList.Remove(drop);
                }

                if (fallbackList.Count == 0)
                {
                    // return a dupliacte drop since all previous drops are in the fallback list
                    return availableDrops[UnityEngine.Random.Range(0, availableDrops.Count)];
                }
            }
            DropData fallbackDrop;
            if (usePitySystem)
            {
                fallbackDrop = fallbackList.OrderByDescending(drop => pitySystem.GetModifiedDropChance(availableRarities[drop.GetRarityIndex()])).First();
            }
            else
            {
                fallbackDrop = fallbackList.OrderByDescending(drop => availableRarities[drop.GetRarityIndex()].dropChance).First();
            }

            return fallbackDrop;
        }

        

        // if we have drops with the chance we want, return a random from the possible drops
        return possibleDrops[UnityEngine.Random.Range(0, possibleDrops.Count)];
    }
    /// <summary>
    /// This function will be used to select a drop from the available drops.
    /// It will activate the drop 
    /// This often called from the ui to select a drop.
    /// </summary>
    public void SelectDropsOption(DropData drop)
    {
        
        if (drop == null) return;
        if (drop.GetDropScript() == null) {
            Debug.LogError($"No drop script found for drop {drop.GetDropName()}");
            return;
        };
        if (drop.GetDropScript().GetComponent<IDrop>() == null) {
            Debug.LogError($"Drop script {drop.GetDropScript().name} does not implement IDrop");
            return;
        }

        if(HUDManager.Instance != null)
        {
            bool isNewUnique = !HUDManager.Instance.HasExactUpgrade(drop.GetID());

            // If player already has 5 upgrades, activate the swap screen.
            if (isNewUnique && HUDManager.Instance.uniqueUpgradesCount == 5)
            {
                if (swapUpgradeUI != null)
                {
                    pendingSwap = drop;
                    swapUpgradeUI.SetActive(true);
                    upgradeSelectionUI.SetActive(false);
                    return;
                }
                else
                {
                    Debug.LogWarning("The swap upgrade UI is null!");
                }
            }
            playerUpgrades.Add(drop);
            HUDManager.Instance.AddUpgrade(drop);
        }
        else
        {
            Debug.LogWarning("HUD Manager instance is not assigned!");
        }


        // for now we will simple just activate the drop
        if (usePitySystem)
        {
            pitySystem.OnUpgradesOffered(availableRarities[drop.GetRarityIndex()]);
        }
        drop.Activate();
    }
    /// <summary>
    /// Buy an upgrade and it takes two parameters: the drop to buy and the slot number to buy it in
    /// If the slot number was not passed then it will add the drop in the first empty slot
    /// </summary>
    /// <param name="drop"> Drop to buy </param>
    public bool BuyUpgrade(DropData drop)
    {
        if (drop == null) return false;
        if (SoulSystem.Instance == null) return false;
        // check the value of the souls 
        int currentSouls = SoulSystem.Instance.GetSoulCurrency();

        if (currentSouls < drop.GetBuyAmount())
        {
            // Not enough souls to buy this drop
            return false;
        }

        // Subtract souls from the player
        SoulSystem.Instance.UseSoulCurrency(drop.GetBuyAmount());
        SelectDropsOption(drop);
        return true;
    }

    /// <summary>
    /// Sell an upgrade and it takes two parameters: the drop to sell and if there is a refund (for swapping)
    /// </summary>
    /// <returns> True if the upgrade was sold, false if the upgrade was not sold </returns>
    public bool SellUpgrade(DropData drop, bool refundSoul)
    {
        if (drop == null) return false;
        List<DropData> toRemove = playerUpgrades.FindAll(u => u != null && u.GetID() == drop.GetID());
        if (toRemove.Count == 0) return false;

        if (!playerUpgrades.Contains(drop))
        {
            // Upgrade does not exist in player's inventory
            return false;
        }

        if (refundSoul)
        {
            int totalRefund = 0;
            foreach (var upgrade in toRemove)
            {
                totalRefund += upgrade.GetSellAmount();
            }
            SoulSystem.Instance.AddSouls(totalRefund);
        }
        playerUpgrades.RemoveAll(u => u != null && u.GetID() == drop.GetID());

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.RefreshHUD();
        }
        else
        {
            Debug.LogWarning("HUDManager not assigned.");
        }

        return true;
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
    /// <summary>
    /// This function will deactives the whole stack and activates the new upgrade
    /// </summary>
    /// <param name="drop">The new drop to swap</param>
    /// <param name="slotNumber">The slot number to swap the drop in</param>
    public void SwapDrop(DropData drop, int slotNumber)
    {
        if (drop == null) return;
        // Swap only if there is a real upgrade in the slot
        if (playerUpgrades[slotNumber] == null) return;
        // Deactivate the current upgrade
        playerUpgrades[slotNumber].Deactivate();
        // Reset the stack count of the current upgrade
        playerUpgrades[slotNumber].ResetStack();
        // Activate the new upgrade
        playerUpgrades[slotNumber] = drop;
        drop.Activate();
    }


        #region Saving/Loading

    [ContextMenu("Save to JSON")]
    /// <summary>
    /// Save the data of the health into json
    /// </summary>
    public void SaveToJson()
    {
        string healthStatsStr = JsonUtility.ToJson(this, true);

        string folderPath = Path.Combine(Application.dataPath, "JSON");
        folderPath = Path.Combine(folderPath, "DropSystem");
        SeeFilePath();
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        string filePath = Path.Combine(folderPath, "DropSystem.json");
        File.WriteAllText(filePath, healthStatsStr);


#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    [ContextMenu("See File Path")]
    /// <summary>
    /// To see the file path of json 
    /// </summary>
    public void SeeFilePath()
    {
        string folderPath = Path.Combine(Application.persistentDataPath, "JSON");
        folderPath = Path.Combine(folderPath, "DropSystem");
        Debug.Log("Path To JSON File:");
        Debug.Log(folderPath);
    }

    [ContextMenu("Load From JSON")]
    /// <summary>
    /// Load the data of the health into json
    /// </summary>
    public void LoadFromJson()
    {

        string folderPath = Path.Combine(Application.dataPath, "JSON");
        folderPath = Path.Combine(folderPath, "DropSystem");
        string filePath = Path.Combine(folderPath, "DropSystem.json");

        string jsonStr = File.ReadAllText(filePath);

        string[] jsons = jsonStr.Split("|");

        JsonUtility.FromJsonOverwrite(jsons[0], this);



#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    #endregion
}
