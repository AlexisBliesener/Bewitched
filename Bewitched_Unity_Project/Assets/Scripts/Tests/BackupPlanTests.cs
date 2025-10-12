using NUnit.Framework;
using UnityEngine;
using System.Reflection;

/// <summary> 
/// Unit tests for the BackupPlan upgrade.
/// </summary>
public class BackupPlanTests
{
    /// <summary>
    /// Mock version of PossessionAbility to test BackupPlan upgrade.
    /// </summary>
    public class MockPossessionAbility : PossessionAbility
    {
        private void Awake()
        {
            instance = this;
        }
    }

    private GameObject go;
    private BackupPlan backupPlan;
    private MockPossessionAbility mockPossession;

    [SetUp]
    public void SetUp()
    {
        // Create mock PossessionAbility
        GameObject possessionGo = new GameObject("MockPossessionAbility");
        mockPossession = possessionGo.AddComponent<MockPossessionAbility>();

        // Create BackupPlan object
        go = new GameObject("BackupPlanTest");
        backupPlan = go.AddComponent<BackupPlan>();
        backupPlan.stackNum = 1;

        // Reset singleton
        PossessionAbility.instance = mockPossession;

        // call Start manually 
        typeof(BackupPlan).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(backupPlan, null);
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
        int baseHits = mockPossession.GetHitsToCharge();
        backupPlan.Activate();
        FieldInfo activeField = typeof(BackupPlan).GetField("active", BindingFlags.NonPublic | BindingFlags.Instance);
        bool isActive = (bool)activeField.GetValue(backupPlan);

        Assert.IsTrue(isActive);
        Assert.Less(mockPossession.GetHitsToCharge(), baseHits);
    }

    /// <summary>
    /// Make sure that deactivating the upgrade set the active field to false.
    /// </summary>
    [Test]
    public void Deactivate_SetsActiveFalse()
    {
        backupPlan.Activate();
        backupPlan.Deactivate();

        FieldInfo activeField = typeof(BackupPlan).GetField("active", BindingFlags.NonPublic | BindingFlags.Instance);
        bool isActive = (bool)activeField.GetValue(backupPlan);

        Assert.IsFalse(isActive);
    }

    /// <summary>
    /// Make sure that the cooldown is reduced to the stack number.
    /// </summary>
    [Test]
    public void ApplyUpgrade_AppliesReduction()
    {
        backupPlan.stackNum = 0;
        backupPlan.Activate();

        int reducdedHits = mockPossession.GetHitsToCharge();

        // Act with higher stack (if multiple values exist in array)
        int[] hitsNeeded = (int[])typeof(BackupPlan).GetField("hitsNeeded", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(backupPlan);
        if (hitsNeeded.Length > 0)
        {
            backupPlan.stackNum = Mathf.Min(1, hitsNeeded.Length - 1);
        }
        else
        {
            backupPlan.stackNum = 1;
        }

        backupPlan.Activate();
        float reducdedHits2 = mockPossession.GetHitsToCharge();

        Assert.LessOrEqual(reducdedHits2, reducdedHits);
    }
}
