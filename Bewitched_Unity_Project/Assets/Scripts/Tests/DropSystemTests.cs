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
    public class MockActivatableDrop : DropItemBase
    {
        public bool wasActivated = false;

        public override void Activate()
        {
            wasActivated = true;
        }
    }
    private GameObject dropSystemObject;
    private DropSystem dropSystem;
    private GameObject mockDropPrefab;
    private DropItemBase mockDrop1;
    private DropItemBase mockDrop2;
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
        commonRarity = ScriptableObject.CreateInstance<ItemRarity>();
        commonRarity.displayName = "Common";
        commonRarity.dropChance = 100;

        rareRarity = ScriptableObject.CreateInstance<ItemRarity>();
        rareRarity.displayName = "Rare"; 
        rareRarity.dropChance = 6;

        // Create mock drop items
        GameObject mockDropObject1 = new GameObject("MockDrop1");
        mockDrop1 = mockDropObject1.AddComponent<DropItemBase>();
        mockDrop1.SetDropName("Health Potion");
        mockDrop1.SetDescription("Restores health");
        mockDrop1.SetRarity(commonRarity);

        GameObject mockDropObject2 = new GameObject("MockDrop2");
        mockDrop2 = mockDropObject2.AddComponent<DropItemBase>();
        mockDrop2.SetDropName("Health Potion 2");
        mockDrop2.SetDescription("Restore health 2x");
        mockDrop2.SetRarity(rareRarity);

        // Set up drop system with test data
        dropSystem.dropPickupPrefab = mockDropPrefab;
        dropSystem.availableDrops.Add(mockDrop1);
        dropSystem.availableDrops.Add(mockDrop2);
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
            Object.DestroyImmediate(mockDrop1.gameObject);
        if (mockDrop2 != null)
            Object.DestroyImmediate(mockDrop2.gameObject);
        if (commonRarity != null)
            Object.DestroyImmediate(commonRarity);
        if (rareRarity != null)
            Object.DestroyImmediate(rareRarity);
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
        DropItemBase receivedDrop1 = null;
        DropItemBase receivedDrop2 = null;

        dropSystem.OnDropPickedUp += (drop1, drop2) => {
            actionTriggered = true;
            receivedDrop1 = drop1;
            receivedDrop2 = drop2;
        };

        dropSystem.ShowDropSelection(Vector3.zero);
        
        Assert.IsTrue(actionTriggered);
        Assert.IsNotNull(receivedDrop1);
        Assert.IsNotNull(receivedDrop2);
    }

    /// <summary>
    /// Test that ShowDropSelection handles empty available drops correctly
    /// </summary>
    [Test]
    public void ShowDropSelection_HandlesEmptyAvailableDrops()
    {
        dropSystem.availableDrops.Clear();
        bool actionTriggered = false;

        dropSystem.OnDropPickedUp += (drop1, drop2) => actionTriggered = true;

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
        GameObject newGameObject = new GameObject();
        MockActivatableDrop mockActivatableDrop = newGameObject.AddComponent<MockActivatableDrop>();
        
        dropSystem.SelectDropsOption(mockActivatableDrop);

        Assert.IsTrue(mockActivatableDrop.wasActivated);
    }

    /// <summary>
    /// Test that SelectDropsOption activate both drops when two are provided
    /// </summary>
    [Test]
    public void SelectDropsOption_ActivatesBothDrops()
    {
        // Create mock drops that track activation
        GameObject gameObject1 = new GameObject();
        MockActivatableDrop mockActivatableDrop1 = gameObject1.AddComponent<MockActivatableDrop>();

        GameObject gameObject2 = new GameObject();
        MockActivatableDrop mockActivatableDrop2 = gameObject2.AddComponent<MockActivatableDrop>();

        dropSystem.SelectDropsOption(mockActivatableDrop1, mockActivatableDrop2);

        Assert.IsTrue(mockActivatableDrop1.wasActivated);
        Assert.IsTrue(mockActivatableDrop2.wasActivated);

    }

    /// <summary>
    /// Test that SelectDropsOption handle null second drop correctly
    /// </summary>
    [Test]
    public void SelectDropsOption_HandlesNullSecondDrop()
    {
        GameObject newGameObject = new GameObject();
        MockActivatableDrop mockActivatableDrop = newGameObject.AddComponent<MockActivatableDrop>();
        
        // This should not throw exception
        Assert.DoesNotThrow(() => dropSystem.SelectDropsOption(mockActivatableDrop, null));
        Assert.IsTrue(mockActivatableDrop.wasActivated);
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
        Assert.AreEqual(2, dropSystem.availableDrops.Count);
        Assert.AreEqual("Health Potion", dropSystem.availableDrops[0].GetDropName());
        Assert.AreEqual("Health Potion 2", dropSystem.availableDrops[1].GetDropName());
    }
}