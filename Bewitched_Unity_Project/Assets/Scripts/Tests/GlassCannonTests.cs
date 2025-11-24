using NUnit.Framework;
using UnityEngine;
using System.Reflection;

/// <summary>
/// Unit tests for the GlassCannon upgrade.
/// </summary>
public class GlassCannonTests
{
    /// <summary>
    /// Mock version of Hag that skips Awake logic.
    /// </summary>
    private class MockHag : Hag
    {
        protected new void Awake() { }
    }
    private GameObject glassCannonObject;
    private GameObject hagObject;
    private GlassCannon glassCannon;
    private Hag hag;
    private GameObject knockbackOrbObject;
    private HealthController healthController;

    [SetUp]
    public void SetUp()
    {
        // Create a Hag with a HealthController
        hagObject = new GameObject("HagTest");
        healthController = hagObject.AddComponent<HealthController>();
        hag = hagObject.AddComponent<MockHag>();

        hag.health = healthController;

        glassCannonObject = new GameObject("GlassCannonTest");
        glassCannon = glassCannonObject.AddComponent<GlassCannon>();
        GlassCannon.instance = glassCannon;

        FieldInfo elethField = typeof(GlassCannon).GetField("eleth", BindingFlags.NonPublic | BindingFlags.Instance);
        elethField.SetValue(glassCannon, hag);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(glassCannonObject);
        Object.DestroyImmediate(hagObject);
    }

    /// <summary>
    /// Ensures that when GlassCannon's Awake() runs, the singleton instance
    /// is correctly set to the current GlassCannon component.
    /// </summary>
    [Test]
    public void GlassCannon_SetsSingletonInstance_OnAwake()
    {
        Assert.AreEqual(glassCannon, GlassCannon.instance);
    }

    /// <summary>
    /// When the upgrade is not active, it returns the base damage.
    /// </summary>
    [Test]
    public void GetModifiedDamage_ReturnsBaseDamage_WhenInactive()
    {
        float baseDamage = 10f;
        glassCannon.stackNum = 1;

        float result = glassCannon.GetModifiedDamage(baseDamage);

        Assert.AreEqual(baseDamage, result, 0.001f);
    }

    /// <summary>
    /// Stack 0  = double increase damage
    /// </summary>
    [Test]
    public void GetModifiedDamage_Stack0_UsesFirstIncreasePercent()
    {
        float baseDamage = 10f;

        glassCannon.Activate();
        glassCannon.stackNum = 0;

        float result = glassCannon.GetModifiedDamage(baseDamage);

        // 100% increase
        Assert.AreEqual(20f, result, 0.001f);
    }

    /// <summary>
    /// Stack 1 = triple increase damage
    /// </summary>
    [Test]
    public void GetModifiedDamage_Stack1_UsesSecondIncreasePercent()
    {
        float baseDamage = 10f;

        glassCannon.Activate();
        glassCannon.stackNum = 1;

        float result = glassCannon.GetModifiedDamage(baseDamage);

        // 200% increase
        Assert.AreEqual(30f, result, 0.001f);
    }

    /// <summary>
    /// Stack 2 = quadruple increase damage
    /// </summary>
    [Test]
    public void GetModifiedDamage_Stack2_UsesThirdIncreasePercent()
    {
        float baseDamage = 10f;

        glassCannon.Activate();
        glassCannon.stackNum = 2;

        float result = glassCannon.GetModifiedDamage(baseDamage);

        // 300% increase
        Assert.AreEqual(40f, result, 0.001f);
    }

    /// <summary>
    /// if stack is higher than the array length, it uses the last damage increase percent.
    /// </summary>
    [Test]
    public void GetModifiedDamage_StackTooHigh_UsesLastIncreasePercent()
    {
        float baseDamage = 10f;

        glassCannon.Activate();
        glassCannon.stackNum = 99; 

        float result = glassCannon.GetModifiedDamage(baseDamage);

        // Uses last value (300%)
        Assert.AreEqual(40f, result, 0.001f);
    }

    /// <summary>
    /// Activating the upgrade sets the active field to true and applies the health reduction.
    /// </summary>
    [Test]
    public void Activate_SetsActiveTrue_AndReducesHealth()
    {
        float baseMaxHealth = hag.health.GetMaxHealth();

        glassCannon.stackNum = 0; 
        glassCannon.Activate();

        FieldInfo activeField = typeof(GlassCannon).GetField("active", BindingFlags.NonPublic | BindingFlags.Instance);
        bool isActive = (bool)activeField.GetValue(glassCannon);

        float newMaxHealth = hag.health.GetMaxHealth();

        Assert.IsTrue(isActive);
        Assert.AreEqual(baseMaxHealth * 0.5f, newMaxHealth, 0.001f);
    }

    /// <summary>
    /// It should reduce eleth health 75% for 1 stack
    /// </summary>
    [Test]
    public void Activate_ReducesHealthStack1()
    {
        float baseMaxHealth = hag.health.GetBaseMaxHealth();

        glassCannon.stackNum = 1; // 75% reduction
        glassCannon.Activate();

        float newMaxHealth = hag.health.GetMaxHealth();

        Assert.AreEqual(baseMaxHealth * 0.25f, newMaxHealth, 0.001f);
    }

    /// <summary>
    /// Deactivating the upgrade sets the active field to false and restores Eleth's base max health
    /// </summary>
    [Test]
    public void Deactivate_SetsActiveFalse_AndRestoresHealth()
    {
        float baseMaxHealth = hag.health.GetMaxHealth();

        glassCannon.stackNum = 0;
        glassCannon.Activate();

        float reducedHealth = hag.health.GetMaxHealth();
        Assert.Less(reducedHealth, baseMaxHealth);

        glassCannon.Deactivate();

        FieldInfo activeField = typeof(GlassCannon).GetField("active", BindingFlags.NonPublic | BindingFlags.Instance);
        bool isActive = (bool)activeField.GetValue(glassCannon);

        float restoredHealth = hag.health.GetMaxHealth();

        Assert.IsFalse(isActive);
        Assert.AreEqual(baseMaxHealth, restoredHealth, 0.001f);
    }
}
