using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Unit tests for the DropSystem class
/// This will test singleton behavior, drop mechanics, drop selection, and item management
/// </summary>
public class DropSystemTests
{
    /// <summary>
    /// Mock drop item class for testing activation behavior
    /// </summary>
    public class MockActivatableDrop : MonoBehaviour, IDrop
    {
        public bool wasActivated = false;

        public int stackNum { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public void Activate()
        {
            wasActivated = true;
        }
    }
    private GameObject dropSystemObject;
    private DropSystem dropSystem;
    private GameObject mockDropPrefab;
    private DropData mockDrop1;
    private DropData mockDrop2;
    private DropData mockDrop3;
    private GameObject mockDropScript1;
    private GameObject mockDropScript2;
    private GameObject mockDropScript3;
    private ItemRarity commonRarity;
    private ItemRarity rareRarity;

    [SetUp]
    public void Setup()
    {
        // Destroy any existing DropSystem instance to prevent conflicts
        if (DropSystem.Instance != null)
        {
            Object.DestroyImmediate(DropSystem.Instance.gameObject);
        }

        // Create the drop system
        dropSystemObject = new GameObject("DropSystem");
        dropSystem = dropSystemObject.AddComponent<DropSystem>();

        // Create mock drop prefab
        mockDropPrefab = new GameObject("MockDropPrefab");

        // Create mock rarity items
        commonRarity = new ItemRarity();
        commonRarity.displayName = "Common";
        commonRarity.dropChance = 100;
        dropSystem.availableRarities.Add(commonRarity);
        rareRarity = new ItemRarity();
        rareRarity.displayName = "Rare"; 
        rareRarity.dropChance = 6;
        dropSystem.availableRarities.Add(rareRarity);

        // Create mock drop items
        mockDropScript1 = new GameObject("MockDrop1");
        mockDropScript1.AddComponent<MockActivatableDrop>();
        mockDrop1 = new DropData();
        mockDrop1.SetDropScript(mockDropScript1);
        mockDrop1.SetDropName("Health Potion");
        mockDrop1.SetDescription("Restores health");
        mockDrop1.SetRarityIndex(0);
        dropSystem.availableDrops.Add(mockDrop1);

        mockDropScript2 = new GameObject("MockDrop2");
        mockDropScript2.AddComponent<MockActivatableDrop>();
        mockDrop2 = new DropData();
        mockDrop2.SetDropScript(mockDropScript1);
        mockDrop2.SetDropName("Health Potion 2");
        mockDrop2.SetDescription("Restores health 2x");
        mockDrop2.SetRarityIndex(0);
        dropSystem.availableDrops.Add(mockDrop2);

        mockDropScript3 = new GameObject("MockDrop3");
        mockDropScript3.AddComponent<MockActivatableDrop>();
        mockDrop3 = new DropData();
        mockDrop3.SetDropScript(mockDropScript1);
        mockDrop3.SetDropName("Health Potion 3");
        mockDrop3.SetDescription("Restores health 3x");
        mockDrop3.SetRarityIndex(0);
        dropSystem.availableDrops.Add(mockDrop3);

        // Set up drop system with test data
        dropSystem.dropPickupPrefab = mockDropPrefab;

        // Disable pity system since we don't test it here 
        dropSystem.SetUsePitySystem(false);
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up all created objects
        if (dropSystemObject != null)
            Object.DestroyImmediate(dropSystemObject);
        if (mockDropPrefab != null)
            Object.DestroyImmediate(mockDropPrefab);
        if (mockDrop1 != null)
            mockDrop1 = null;
        if (mockDrop2 != null)
            mockDrop2 = null;
        if (mockDrop3 != null)
            mockDrop3 = null;
        if (commonRarity != null)
            commonRarity = null;
        if (rareRarity != null)
            rareRarity = null;
        if (mockDropScript1 != null)
            Object.DestroyImmediate(mockDropScript1);
        if (mockDropScript2 != null)
            Object.DestroyImmediate(mockDropScript2);
        if (mockDropScript3 != null)
            Object.DestroyImmediate(mockDropScript3);
    }

    /// <summary>
    /// Test that DropSystem creates a singleton instance correctly
    /// </summary>
    [Test]
    public void Instance_CreatesSingletonInstance()
    {
        Assert.IsNotNull(DropSystem.Instance);
        Assert.AreEqual(dropSystem, DropSystem.Instance);
    }

    /// <summary>
    /// Test that creating multiple DropSystem objects destroys duplicate instances
    /// As singleton, this should not be possible
    /// </summary>
    [UnityTest]
    public IEnumerator Instance_DestroysDuplicateInstances()
    {
        GameObject duplicateObject = new GameObject("DuplicateDropSystem");
        DropSystem duplicateSystem = duplicateObject.AddComponent<DropSystem>();

        // Start should be called
        yield return null;

        Assert.AreEqual(dropSystem, DropSystem.Instance);
        Assert.IsTrue(duplicateObject == null); // Should be destroyed
    }

    /// <summary>
    /// Test that GetDropChance return the correct drop chance value
    /// </summary>
    [Test]
    public void GetDropChance_ReturnsCorrectValue()
    {
        dropSystem.dropChance = 75;

        int result = dropSystem.GetDropChance();

        Assert.AreEqual(75, result);
    }

    /// <summary>
    /// Test that SetDropChance update the drop chance correctly
    /// </summary>
    [Test]
    public void SetDropChance_UpdatesDropChance()
    {
        dropSystem.SetDropChance(30);

        Assert.AreEqual(30, dropSystem.GetDropChance());
    }

    /// <summary>
    /// Test that GetDroppedItemThisRun return correct count initially
    /// </summary>
    [Test]
    public void GetDroppedItemThisRun_ReturnsZeroInitially()
    {
        int result = dropSystem.GetDroppedItemThisRun();

        Assert.AreEqual(0, result);
    }

    /// <summary>
    /// Test that TryDropItem does not spawn when no available drops exist
    /// </summary>
    [Test]
    public void TryDropItem_DoesNotSpawnWhenNoAvailableDrops()
    {
        dropSystem.availableDrops.Clear();
        dropSystem.dropChance = 100; // Guaranteed drop chance!
        Vector3 testPosition = Vector3.zero;

        dropSystem.TryDropItem(testPosition);

        Assert.AreEqual(0, dropSystem.GetDroppedItemThisRun());
    }

    /// <summary>
    /// Test that TryDropItem spawns drop when chance is met
    /// </summary>
    [Test]
    public void TryDropItem_SpawnsDropWhenChanceMet()
    {
        dropSystem.dropChance = 100; // Guaranteed drop
        Vector3 testPosition = Vector3.zero;

        dropSystem.TryDropItem(testPosition);

        Assert.AreEqual(1, dropSystem.GetDroppedItemThisRun());
    }

    /// <summary>
    /// Test that ShowDropSelection trigger OnDropPickedUp action correctly
    /// </summary>
    [Test]
    public void ShowDropSelection_TriggersOnDropPickedUpAction()
    {

        // Set drop chance to 100%
        // This make sure that the onDropPickedUp action is triggered always trigger when the chance is met
        dropSystem.SetDropChance(100);
        bool actionTriggered = false;
        DropData receivedDrop1 = null;
        DropData receivedDrop2 = null;
        DropData receivedDrop3 = null;


        dropSystem.OnDropRandomDrop += (drop1, drop2, drop3) =>
        {
            actionTriggered = true;
            receivedDrop1 = drop1;
            receivedDrop2 = drop2;
            receivedDrop3 = drop3;
        };

        dropSystem.ShowDropSelection(Vector3.zero);

        Assert.IsTrue(actionTriggered);
        Assert.IsNotNull(receivedDrop1);
        Assert.IsNotNull(receivedDrop2);
        Assert.IsNotNull(receivedDrop3);
    }

    /// <summary>
    /// Test that ShowDropSelection handles empty available drops correctly
    /// </summary>
    [Test]
    public void ShowDropSelection_HandlesEmptyAvailableDrops()
    {
        dropSystem.availableDrops.Clear();
        bool actionTriggered = false;

        dropSystem.OnDropRandomDrop += (drop1, drop2, drop3) => actionTriggered = true;

        dropSystem.ShowDropSelection(Vector3.zero);

        Assert.IsFalse(actionTriggered);
    }

    /// <summary>
    /// Test that SelectDropsOption activate single drop correctly
    /// </summary>
    [Test]
    public void SelectDropsOption_ActivatesSingleDrop()
    {
        // Create mock drop that tracks activation
        DropData newDropData = new DropData();
        GameObject newGameObject = new GameObject();
        newGameObject.AddComponent<MockActivatableDrop>();
        newDropData.SetDropScript(newGameObject);
        
        dropSystem.SelectDropsOption(newDropData);

        Assert.IsTrue(newGameObject.GetComponent<MockActivatableDrop>().wasActivated);
    }

    /// <summary>
    /// Test that SelectDropsOption activate both drops when two are provided
    /// </summary>
    [Test]
    public void SelectDropsOption_ActivatesBothDrops()
    {
        // Create mock drops that track activation
        DropData newDropData1 = new DropData();
        GameObject newGameObject1 = new GameObject();
        newGameObject1.AddComponent<MockActivatableDrop>();
        newDropData1.SetDropScript(newGameObject1);

        dropSystem.SelectDropsOption(newDropData1);

        DropData newDropData2 = new DropData();
        GameObject newGameObject2 = new GameObject();
        newGameObject2.AddComponent<MockActivatableDrop>();
        newDropData2.SetDropScript(newGameObject2);

        dropSystem.SelectDropsOption(newDropData2);

        DropData newDropData3 = new DropData();
        GameObject newGameObject3 = new GameObject();
        newGameObject3.AddComponent<MockActivatableDrop>();
        newDropData3.SetDropScript(newGameObject3);

        dropSystem.SelectDropsOption(newDropData3);

        Assert.IsTrue(newGameObject1.GetComponent<MockActivatableDrop>().wasActivated);
        Assert.IsTrue(newGameObject2.GetComponent<MockActivatableDrop>().wasActivated);
        Assert.IsTrue(newGameObject3.GetComponent<MockActivatableDrop>().wasActivated);
    }

    /// <summary>
    /// Test that SalvageDrop execute without errors as we don't have any code to test yet :) 
    /// </summary>
    [Test]
    public void SalvageDrop_ExecutesWithoutErrors()
    {

        Assert.DoesNotThrow(() => dropSystem.SalvageDrop());
    }

    /// <summary>
    /// Test that multiple drop spawns increment counter correctly
    /// </summary>
    [Test]
    public void MultipleDropSpawns_IncrementCounterCorrectly()
    {
        dropSystem.dropChance = 100; // Guaranteed drops
        Vector3 testPosition = Vector3.zero;

        dropSystem.TryDropItem(testPosition);
        dropSystem.TryDropItem(testPosition);
        dropSystem.TryDropItem(testPosition);

        Assert.AreEqual(3, dropSystem.GetDroppedItemThisRun());
    }

    /// <summary>
    /// Test that available drops list is properly initialized
    /// </summary>
    [Test]
    public void AvailableDrops_ProperlyInitialized()
    {
        Assert.IsNotNull(dropSystem.availableDrops);
        Assert.AreEqual(3, dropSystem.availableDrops.Count);
        Assert.AreEqual("Health Potion", dropSystem.availableDrops[0].GetDropName());
        Assert.AreEqual("Health Potion 2", dropSystem.availableDrops[1].GetDropName());
        Assert.AreEqual("Health Potion 3", dropSystem.availableDrops[2].GetDropName());
    }
}