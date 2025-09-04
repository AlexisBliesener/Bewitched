using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UpgradeSystem : MonoBehaviour
{
    public static UpgradeSystem Instance;
    [Header("Upgrade Settings")]
    [SerializeField] private int upgradeDropChance = 50;
    private List<UpgradeItemBase> availableUpgrades = new List<UpgradeItemBase>();
    [SerializeField]
    private GameObject upgradePickupPrefab;
    public Action<UpgradeItemBase, UpgradeItemBase> OnUpgradePickedUp;

    // List of upgrades that have been picked up (For later use)
    private List<UpgradeItemBase> upgardePickedup = new List<UpgradeItemBase>();
    private int upgradesDroppedThisRun = 0;

    public int GetUpgradeDropChance() => upgradeDropChance;

    public void SetUpgradeDropChance(int val) => upgradeDropChance = val;

    // Start is called before the first frame update
    private void Start()
    {
        // Only one instance of UpgradeSystem should be there
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    public void TryDropUpgrade(Vector3 position)
    {
        if (availableUpgrades.Count == 0) return;
        // Check the chance of dropping an upgrade
        if (UnityEngine.Random.Range(1, 101) > upgradeDropChance) return;
        
        SpawnUpgradePickup(position);
        
    }
    private void SpawnUpgradePickup(Vector3 position)
    {
        if (upgradePickupPrefab == null || availableUpgrades.Count == 0)
            return;

        // Spawn slightly above ground
        Vector3 spawnPos = position + Vector3.up * 0.5f;
        Instantiate(upgradePickupPrefab, spawnPos, Quaternion.identity);

        upgradesDroppedThisRun++;

        Debug.Log($"Upgrade dropped! Total this run: {upgradesDroppedThisRun}");
    }
    public void ShowUpgradeSelection(Vector3 pickupPosition)
    {
        // Get two random upgrades
        UpgradeItemBase option1 = GetRandomUpgrade();
        UpgradeItemBase option2 = GetRandomUpgrade();

        Debug.Log($"Upgrade selection triggered! Options: {option1.GetUpgradeName()} vs {option2.GetUpgradeName()}");
        
        upgardePickedup.Add(option1);
        upgardePickedup.Add(option2);

        OnUpgradePickedUp?.Invoke(option1, option2);
    }
    private UpgradeItemBase GetRandomUpgrade()
    {
        if (availableUpgrades.Count == 0)
            return null;

        int randomRange = UnityEngine.Random.Range(1, 101);
        List<UpgradeItemBase> possibleUpgrades = availableUpgrades.Where(upgrade => upgrade.GetRarity().dropChance >= randomRange).ToList();
        if (possibleUpgrades.Count == 0)
            // if for some reason we don't have any upgrades with the chance we want, just return a random from all available upgrades
            return availableUpgrades[UnityEngine.Random.Range(0, availableUpgrades.Count)];

        // if we have upgrades with the chance we want, return a random from the possible upgrades
        return possibleUpgrades[UnityEngine.Random.Range(0, possibleUpgrades.Count)];
    }
}
