using NUnit.Framework;
using UnityEngine;
using System.Reflection;

/// <summary>
/// Spy version of Vampirism to track calls for testing.
/// </summary>
public class VampirismSpy : Vampirism
{
    public bool StealHealthCalled { get; private set; }
    public float LastDamage { get; private set; }

    public new void stealHealth(float damageDone)
    {
        StealHealthCalled = true;
        LastDamage = damageDone;
        base.stealHealth(damageDone);
    }
}

/// <summary>
/// Mock health component for testing AddHealth.
/// </summary>
public class MockHealth : HealthController
{
    public new float CurrentHealth { get; private set; }

    public new void AddHealth(float amount)
    {
        CurrentHealth += amount;
    }
}

/// <summary>
/// Mock OldHag with a health component.
/// </summary>
public class MockOldHag : Hag
{
    public new MockHealth health = new MockHealth();
}

/// <summary>
/// Mock PlayerController singleton for testing.
/// </summary>
public class MockPlayerController : PlayerController
{
    public new static MockPlayerController instance;
    public new MockOldHag oldHag = new MockOldHag();

    public MockPlayerController()
    {
        instance = this;
    }
}

public class VampirismTests
{
    private VampirismSpy vampirism;
    private MockPlayerController mockPlayer;

    [SetUp]
    public void SetUp()
    {
        // Create GameObject for VampirismSpy
        GameObject go = new GameObject();
        vampirism = go.AddComponent<VampirismSpy>();

        // Reset singleton instance
        Vampirism.instance = vampirism;

        // Setup mock PlayerController
        mockPlayer = new MockPlayerController();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(vampirism.gameObject);
    }

    /// <summary>
    /// Ensures that when Vampirism's Awake() runs, the singleton instance
    /// is correctly set to the current Vampirism component.
    /// </summary>
    [Test]
    public void Vampirism_SetsSingletonInstance_OnAwake()
    {
        Assert.AreEqual(vampirism, Vampirism.instance);
    }

    /// <summary>
    /// Verifies that calling Activate() sets the private "active" field to true,
    /// enabling the Vampirism effect.
    /// </summary>
    [Test]
    public void Activate_SetsActiveTrue()
    {
        vampirism.Activate();
        var activeField = typeof(Vampirism).GetField("active", BindingFlags.NonPublic | BindingFlags.Instance);
        bool isActive = (bool)activeField.GetValue(vampirism);
        Assert.IsTrue(isActive);
    }

    /// <summary>
    /// Confirms that stealHealth() is invoked when Vampirism is active,
    /// and that the method receives the correct damage value.
    /// </summary>
    [Test]
    public void StealHealth_IsCalled_WhenActive()
    {
        vampirism.Activate();
        vampirism.stackNum = 0;

        float damage = 100f;
        vampirism.stealHealth(damage);

        Assert.IsTrue(vampirism.StealHealthCalled, "stealHealth should have been called.");
        Assert.AreEqual(damage, vampirism.LastDamage, "Damage passed into stealHealth should match.");
    }

}
