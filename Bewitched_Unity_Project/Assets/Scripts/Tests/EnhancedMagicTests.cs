using NUnit.Framework;
using UnityEngine;
using System.Reflection;

/// <summary>
/// Unit tests for the EnhancedMagic upgrade.
/// </summary>
public class EnhancedMagicTests
{
    /// <summary>
    /// Mock version of PossessionAbility to test EnhancedMagic upgrade.
    /// </summary>
    public class MockPossessionAbility : PossessionAbility
    {
        private void Awake()
        {
            instance = this;
        }
    }

    private GameObject go;
    private GameObject possessionGo;
    private EnhancedMagic enhancedMagic;
    private MockPossessionAbility mockPossession;

    [SetUp]
    public void SetUp()
    {
        // Create mock PossessionAbility
        possessionGo = new GameObject("MockPossessionAbility");
        mockPossession = possessionGo.AddComponent<MockPossessionAbility>();

        // Set base distances
        mockPossession.SetPossessionDistance(10f, 20f);

        // Create EnhancedMagic object
        go = new GameObject("EnhancedMagicTest");
        enhancedMagic = go.AddComponent<EnhancedMagic>();
        enhancedMagic.stackNum = 0;

        // Reset singleton
        PossessionAbility.instance = mockPossession;

        // call Start manually
        typeof(EnhancedMagic).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(enhancedMagic, null);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(mockPossession.gameObject);
    }

    /// <summary>
    /// Make sure that activating the upgrade set the active field to true and applied the upgrade.
    /// </summary>
    [Test]
    public void Activate_SetsActiveTrue_AndAppliesUpgrade()
    {
        float baseStart = mockPossession.GetStartingPossessionDistance();
        float baseEnd = mockPossession.GetEndingPossessionDistance();
        enhancedMagic.stackNum = 0;
        enhancedMagic.Activate();
        FieldInfo activeField = typeof(EnhancedMagic).GetField("active", BindingFlags.NonPublic | BindingFlags.Instance);
        bool isActive = (bool)activeField.GetValue(enhancedMagic);
        float newStart = mockPossession.GetStartingPossessionDistance();
        float newEnd = mockPossession.GetEndingPossessionDistance();
        float expectedStart = baseStart * 1.15f; // 15% increase
        float expectedEnd = baseEnd * 1.15f; // 15% increase
        Assert.IsTrue(isActive);
        Assert.AreEqual(expectedStart, newStart, 0.001f);
        Assert.AreEqual(expectedEnd, newEnd, 0.001f);
    }

    /// <summary>
    /// Make sure that deactivating the upgrade set the active field to false.
    /// </summary>
    [Test]
    public void Deactivate_SetsActiveFalse()
    {
        float baseStart = mockPossession.GetStartingPossessionDistance();
        float baseEnd = mockPossession.GetEndingPossessionDistance();

        enhancedMagic.stackNum = 1;
        enhancedMagic.Activate();
        enhancedMagic.Deactivate();

        FieldInfo activeField = typeof(EnhancedMagic).GetField("active", BindingFlags.NonPublic | BindingFlags.Instance);
        bool isActive = (bool)activeField.GetValue(enhancedMagic);

        float restoredStart = mockPossession.GetStartingPossessionDistance();
        float restoredEnd = mockPossession.GetEndingPossessionDistance();

        Assert.IsFalse(isActive);
        Assert.AreEqual(baseStart, restoredStart, 0.001f);
        Assert.AreEqual(baseEnd, restoredEnd, 0.001f);
    }

    /// <summary>
    /// Make sure that the range is increased based on the stack number.
    /// </summary>
    [Test]
    public void ApplyUpgrade_AppliesRangeIncrease()
    {
        float baseStart = mockPossession.GetStartingPossessionDistance();
        float baseEnd = mockPossession.GetEndingPossessionDistance();

        // stack 0 = 15
        enhancedMagic.stackNum = 0;
        enhancedMagic.Activate();
        float startStack0 = mockPossession.GetStartingPossessionDistance();
        float endStack0 = mockPossession.GetEndingPossessionDistance();

        // stack 1 = 30%
        enhancedMagic.stackNum = 1;
        enhancedMagic.Activate();
        float startStack1 = mockPossession.GetStartingPossessionDistance();
        float endStack1 = mockPossession.GetEndingPossessionDistance();

        // if stack is too high it should clamped to last stack = 45%
        enhancedMagic.stackNum = 99;
        enhancedMagic.Activate();
        float startStackMax = mockPossession.GetStartingPossessionDistance();
        float endStackMax = mockPossession.GetEndingPossessionDistance();

        Assert.Greater(startStack0, baseStart);
        Assert.Greater(endStack0, baseEnd);

        Assert.Greater(startStack1, startStack0);
        Assert.Greater(endStack1, endStack0);

        Assert.Greater(startStackMax, startStack1);
        Assert.Greater(endStackMax, endStack1);
    }
}
