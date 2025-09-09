using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Unit tests for the PitySystem class
/// Tests initialization, pity tracking, drop chance modifications, and upgrade handling
/// </summary>
public class PitySystemTests
{
    private GameObject pitySystemObject;
    private PitySystem pitySystem;
    private ItemRarity commonRarity;
    private ItemRarity rareRarity;
    private ItemRarity ultraRareRarity;
    private List<ItemRarity> testRarities;

    [SetUp]
    public void Setup()
    {
        pitySystemObject = new GameObject("PitySystem");
        pitySystem = pitySystemObject.AddComponent<PitySystem>();

        // Create mock rarity items
        commonRarity = new ItemRarity();
        commonRarity.displayName = "Common";
        commonRarity.dropChance = 70;

        rareRarity = new ItemRarity();
        rareRarity.displayName = "Rare";
        rareRarity.dropChance = 25;

        ultraRareRarity = new ItemRarity();
        ultraRareRarity.displayName = "Ultra Rare";
        ultraRareRarity.dropChance = 5;

        testRarities = new List<ItemRarity> { commonRarity, rareRarity, ultraRareRarity };
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up all created objects
        if (pitySystemObject != null)
            Object.DestroyImmediate(pitySystemObject);
        
        commonRarity = null;
        rareRarity = null;
        ultraRareRarity = null;
        testRarities = null;
    }

    /// <summary>
    /// Test that Initialize clear existing pity counters and add all provided rarities
    /// </summary>
    [Test]
    public void Initialize_ClearsExistingCountersAndAddsRarities()
    {
        pitySystem.GetPityCounters()[commonRarity] = 10f;
        pitySystem.Initialize(testRarities);

        Dictionary<ItemRarity, float> pityCounters = pitySystem.GetPityCounters();
        Assert.AreEqual(3, pityCounters.Count); 
        // should have all rarities
        Assert.IsTrue(pityCounters.ContainsKey(commonRarity));
        Assert.IsTrue(pityCounters.ContainsKey(rareRarity));
        Assert.IsTrue(pityCounters.ContainsKey(ultraRareRarity));
        // should have all rarities with zero pity
        Assert.AreEqual(0f, pityCounters[commonRarity]);
        Assert.AreEqual(0f, pityCounters[rareRarity]);
        Assert.AreEqual(0f, pityCounters[ultraRareRarity]);
    }

    /// <summary>
    /// Test that Initialize handles null rarities correctly
    /// </summary>
    [Test]
    public void Initialize_IgnoresNullRarities()
    {
         List<ItemRarity> raritiesWithNull = new List<ItemRarity> { commonRarity, null, rareRarity };

        pitySystem.Initialize(raritiesWithNull);
        Dictionary<ItemRarity, float> pityCounters = pitySystem.GetPityCounters();
        Assert.AreEqual(2, pityCounters.Count);
        Assert.IsTrue(pityCounters.ContainsKey(commonRarity));
        Assert.IsTrue(pityCounters.ContainsKey(rareRarity));
    }

    /// <summary>
    /// Test that Initialize handles empty list correctly
    /// </summary>
    [Test]
    public void Initialize_HandlesEmptyList()
    {
        List<ItemRarity> emptyRarities = new List<ItemRarity>();

        pitySystem.Initialize(emptyRarities);
         Dictionary<ItemRarity, float> pityCounters = pitySystem.GetPityCounters();
        Assert.AreEqual(0, pityCounters.Count);
    }

    /// <summary>
    /// Test that GetModifiedDropChance returns base chance when no pity bonus exists
    /// </summary>
    [Test]
    public void GetModifiedDropChance_ReturnsBaseChanceWithNoPity()
    {
        pitySystem.Initialize(testRarities);

        int result = pitySystem.GetModifiedDropChance(commonRarity);
        // Base chance should be 70 as it's the default drop chance
        Assert.AreEqual(70, result);
    }

    /// <summary>
    /// Test that GetModifiedDropChance adds pity bonus to base chance
    /// </summary>
    [Test]
    public void GetModifiedDropChance_AddsPityBonusToBaseChance()
    {
        pitySystem.Initialize(testRarities);
        pitySystem.GetPityCounters()[rareRarity] = 15f;
        int result = pitySystem.GetModifiedDropChance(rareRarity);

        Assert.AreEqual(40, result);
    }

    /// <summary>
    /// Test that GetModifiedDropChance caps at 100%
    /// </summary>
    [Test]
    public void GetModifiedDropChance_CapsAtOneHundredPercent()
    {
        pitySystem.Initialize(testRarities);
        pitySystem.GetPityCounters()[commonRarity] = 50f; // 70 + 50 = 120 should cap at 100

        Assert.AreEqual(100, pitySystem.GetModifiedDropChance(commonRarity));
    }

    /// <summary>
    /// Test that GetModifiedDropChance handle null rarity
    /// </summary>
    [Test]
    public void GetModifiedDropChance_HandlesNullRarity()
    {
        pitySystem.Initialize(testRarities);

        int result = pitySystem.GetModifiedDropChance(null);
        Assert.AreEqual(0, result);
    }

    /// <summary>
    /// Test that GetModifiedDropChance handle unknown rarity (That doesn't have a name)
    /// </summary>
    [Test]
    public void GetModifiedDropChance_HandlesUnknownRarity()
    {
        // Arrange
        pitySystem.Initialize(testRarities);
        ItemRarity unknownRarity = new ItemRarity();
        unknownRarity.dropChance = 15;

        int result = pitySystem.GetModifiedDropChance(unknownRarity);
        Assert.AreEqual(15, result);
    }

    /// <summary>
    /// Test that OnUpgradesOffered reset pity for offered rarity
    /// </summary>
    [Test]
    public void OnUpgradesOffered_ResetsPityForOfferedRarity()
    {
        pitySystem.Initialize(testRarities);
        pitySystem.GetPityCounters()[rareRarity] = 20f;

        pitySystem.OnUpgradesOffered(rareRarity);

        // Pity should be reset to 0
        Assert.AreEqual(0f, pitySystem.GetPityCounters()[rareRarity]);
    }

    /// <summary>
    /// Test that OnUpgradesOffered increase pity for non offered rarities
    /// </summary>
    [Test]
    public void OnUpgradesOffered_IncreasesPityForNonOfferedRarities()
    {
        pitySystem.Initialize(testRarities);
        float initialPity = 10f;
        pitySystem.GetPityCounters()[ultraRareRarity] = initialPity;
        // Not ultraRareRarity so it should increase by pityIncrement (default 5f)
        pitySystem.OnUpgradesOffered(commonRarity);

        Assert.AreEqual(initialPity + 5f, pitySystem.GetPityCounters()[ultraRareRarity]);
    }

    /// <summary>
    /// Test that multiple OnUpgradesOffered calls work correctly
    /// </summary>
    [Test]
    public void OnUpgradesOffered_HandlesMultipleCalls()
    {
        pitySystem.Initialize(testRarities);

        pitySystem.OnUpgradesOffered(commonRarity);
        pitySystem.OnUpgradesOffered(commonRarity);
        pitySystem.OnUpgradesOffered(commonRarity);

        Assert.AreEqual(0f, pitySystem.GetPityCounters()[commonRarity]);
        Assert.AreEqual(15f, pitySystem.GetPityCounters()[rareRarity]); // 3 * 5f
        Assert.AreEqual(15f, pitySystem.GetPityCounters()[ultraRareRarity]); // 3 * 5f
    }

    /// <summary>
    /// Test that pity system works with alternating offered rarities
    /// </summary>
    [Test]
    public void OnUpgradesOffered_HandlesAlternatingRarities()
    {
        pitySystem.Initialize(testRarities);

        pitySystem.OnUpgradesOffered(commonRarity); // rare +5, ultraRare +5
        pitySystem.OnUpgradesOffered(rareRarity);   // rare reset, ultraRare +5, rare +5
        pitySystem.OnUpgradesOffered(commonRarity); // common reset, rare +5, ultraRare +5

        Assert.AreEqual(0f, pitySystem.GetPityCounters()[commonRarity]); // Reset last
        Assert.AreEqual(5f, pitySystem.GetPityCounters()[rareRarity]);   // It just increased once and then reset
        Assert.AreEqual(15f, pitySystem.GetPityCounters()[ultraRareRarity]);  // Increased 3 times
    }

    /// <summary>
    /// Test that GetPityCounters returns the correct dictionary reference after initialization
    /// </summary>
    [Test]
    public void GetPityCounters_ReturnsCorrectDictionary()
    {
        pitySystem.Initialize(testRarities);
        Dictionary<ItemRarity, float> pityCounters = pitySystem.GetPityCounters();

        Assert.IsNotNull(pityCounters);
        Assert.AreEqual(3, pityCounters.Count);
        Assert.IsTrue(pityCounters.ContainsKey(commonRarity));
        Assert.IsTrue(pityCounters.ContainsKey(rareRarity));
        Assert.IsTrue(pityCounters.ContainsKey(ultraRareRarity));
    }

    /// <summary>
    /// Test complete pity system workflow simulation
    /// </summary>
    [Test]
    public void PitySystemWorkflow_SimulatesCompleteFlow()
    {
        pitySystem.Initialize(testRarities);

        // Offer Common
        pitySystem.OnUpgradesOffered(commonRarity);
        Assert.AreEqual(0f, pitySystem.GetPityCounters()[commonRarity]);
        Assert.AreEqual(5f, pitySystem.GetPityCounters()[rareRarity]);
        Assert.AreEqual(5f, pitySystem.GetPityCounters()[ultraRareRarity]);
        
        // Verify modified chances
        Assert.AreEqual(70, pitySystem.GetModifiedDropChance(commonRarity)); // 70 + 0
        Assert.AreEqual(30, pitySystem.GetModifiedDropChance(rareRarity));   // 25 + 5 = 30
        Assert.AreEqual(10, pitySystem.GetModifiedDropChance(ultraRareRarity));   // 5 + 5 = 10

        // Offer Common again
        pitySystem.OnUpgradesOffered(commonRarity);
        Assert.AreEqual(0f, pitySystem.GetPityCounters()[commonRarity]);
        Assert.AreEqual(10f, pitySystem.GetPityCounters()[rareRarity]);
        Assert.AreEqual(10f, pitySystem.GetPityCounters()[ultraRareRarity]);

        // Finally offer Rare
        pitySystem.OnUpgradesOffered(rareRarity);
        Assert.AreEqual(5f, pitySystem.GetPityCounters()[commonRarity]);
        Assert.AreEqual(0f, pitySystem.GetPityCounters()[rareRarity]); // Reset
        Assert.AreEqual(15f, pitySystem.GetPityCounters()[ultraRareRarity]);
    }
}