using NUnit.Framework;
using UnityEngine;
using System.Reflection;

/// <summary>
/// Unit tests for the SmarterControl upgrade.
/// </summary>
public class SmarterControlTests
{
    public class MockCharacter : Character
    {
        public override void Die() { }
    }
    private SmarterControl smarterControl;
    private HealthController health;
    private Character dummyChar;
    private float minDecayRate;

    [SetUp]
    public void SetUp()
    {
        GameObject mockCharacter = new GameObject("DummyChar");
        dummyChar = mockCharacter.AddComponent<MockCharacter>();
        health = mockCharacter.GetComponent<HealthController>();
        health.SetDecay(10f);
        GameObject smarterControlGO = new GameObject("SmarterControl");
        smarterControl = smarterControlGO.AddComponent<SmarterControl>();
        FieldInfo minDecayRateField = typeof(SmarterControl).GetField("minDecayRate", BindingFlags.NonPublic | BindingFlags.Instance);
        minDecayRate = (float)minDecayRateField.GetValue(smarterControl);
        smarterControl.stackNum = 1;
        smarterControl.Activate();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(dummyChar.gameObject);
        Object.DestroyImmediate(smarterControl.gameObject);
    }

    /// <summary>
    /// Ensures that when SmarterControl's Awake() runs, the singleton instance
    /// is correctly set to the current Vampirism component.
    /// </summary>
    [Test]
    public void Awake_SetsSingletonInstance()
    {
        Assert.AreEqual(smarterControl, SmarterControl.instance);
    }
    /// <summary>
    /// Test that activating the upgrade halves the health decay rate.
    /// </summary>
    [Test]
    public void ApplyDecayReduction_HalvesDecayRate()
    {
        float before = health.GetDecayRate();
        smarterControl.ApplyDecayReduction(dummyChar);
        float after = health.GetDecayRate();

        Assert.Less(after, before);
        Assert.Greater(after, 0f);
    }
    /// <summary>
    /// Test that the decay rate is clamped at the minimum allowed value.
    /// </summary>
    [Test]
    public void ApplyDecayReduction_ClampsDecayRateAtMin()
    {
        float before = health.GetDecayRate();
        for (int i = 0; i < 4; i++)
        {
            smarterControl.ApplyDecayReduction(dummyChar);
        }
        float after = health.GetDecayRate();

        Assert.Less(after, before);
        Assert.AreEqual(minDecayRate, after, 0.0001f);
    }
}
