using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Test cases for the OffGuard upgrade
/// </summary>
public class OffGuardTests
{
    private OffGuard offGuard;
    private GameObject gameObject;

    [SetUp]
    public void SetUp()
    {
        gameObject = new GameObject("OffGuardTestObject");
        offGuard = gameObject.AddComponent<OffGuard>();

        // Reset singleton to this instance
        OffGuard.instance = offGuard;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(gameObject);
    }

    /// <summary>
    /// Ensures that when OffGuard's Awake() runs, the singleton instance
    /// is correctly set to the current OffGuard component.
    /// </summary>
    [Test]
    public void OffGuard_SetsSingletonInstance_OnAwake()
    {
        Assert.AreEqual(offGuard, OffGuard.instance);
    }

    /// <summary>
    /// When the upgrade is not active, GetModifiedDamage returns the base damage.
    /// </summary>
    [Test]
    public void GetModifiedDamage_ReturnsBaseDamage_WhenInactive()
    {
        float baseDamage = 100f;
        offGuard.stackNum = 1;

        float result = offGuard.GetModifiedDamage(baseDamage);

        Assert.AreEqual(baseDamage, result, 0.001f);
    }

    /// <summary>
    /// When the upgrade is not active, GetModifiedStunDuration returns the base stun duration.
    /// </summary>
    [Test]
    public void GetModifiedStunDuration_ReturnsBaseStun_WhenInactive()
    {
        float baseStun = 1.0f;
        offGuard.stackNum = 1;

        float result = offGuard.GetModifiedStunDuration(baseStun);

        Assert.AreEqual(baseStun, result, 0.001f);
    }

    /// <summary>
    /// Stack 0 uses the first damage percent (10%)
    /// </summary>
    [Test]
    public void GetModifiedDamage_Stack0_UsesFirstDamagePercent()
    {
        float baseDamage = 100f;

        offGuard.Activate();
        offGuard.stackNum = 0;

        float result = offGuard.GetModifiedDamage(baseDamage);

        Assert.AreEqual(110f, result, 0.001f);
    }

    /// <summary>
    /// Stack 1 uses the second damage percent (15%)
    /// </summary>
    [Test]
    public void GetModifiedDamage_Stack1_UsesSecondDamagePercent()
    {
        float baseDamage = 100f;

        offGuard.Activate();
        offGuard.stackNum = 1;

        float result = offGuard.GetModifiedDamage(baseDamage);

        Assert.AreEqual(115f, result, 0.001f);
    }

    /// <summary>
    /// Stack 2 uses the third damage percent (20%)
    /// </summary>
    [Test]
    public void GetModifiedDamage_Stack2_UsesThirdDamagePercent()
    {
        float baseDamage = 100f;

        offGuard.Activate();
        offGuard.stackNum = 2;

        float result = offGuard.GetModifiedDamage(baseDamage);

        Assert.AreEqual(120f, result, 0.001f);
    }

    /// <summary>
    /// Stack 0 uses the first stun duration percent (10%)
    /// </summary>
    [Test]
    public void GetModifiedStunDuration_Stack0_UsesFirstStunPercent()
    {
        float baseStun = 1.0f;

        offGuard.Activate();
        offGuard.stackNum = 0;

        float result = offGuard.GetModifiedStunDuration(baseStun);

        Assert.AreEqual(1.1f, result, 0.001f);
    }

    /// <summary>
    /// Stack 2 uses the third stun duration percent (20%)
    /// </summary>
    [Test]
    public void GetModifiedStunDuration_Stack2_UsesThirdStunPercent()
    {
        float baseStun = 1.0f;

        offGuard.Activate();
        offGuard.stackNum = 2;

        float result = offGuard.GetModifiedStunDuration(baseStun);

        Assert.AreEqual(1.2f, result, 0.001f);
    }

    /// <summary>
    /// if the stack is higher than the length of the arrays, it uses the last percent value
    /// </summary>
    [Test]
    public void GetModifiedValues_StackTooHigh_UsesLastPercent()
    {
        float baseDamage = 100f;
        float baseStun = 1.0f;

        offGuard.Activate();
        offGuard.stackNum = 99; 

        float modifiedDamage = offGuard.GetModifiedDamage(baseDamage);
        float modifiedStun = offGuard.GetModifiedStunDuration(baseStun);

        Assert.AreEqual(120f, modifiedDamage, 0.001f);
        Assert.AreEqual(1.2f, modifiedStun, 0.001f);
    }

    /// <summary>
    /// Deactivating the upgrade stops modifying damage and stun duration
    /// </summary>
    [Test]
    public void Deactivate_StopsModifyingDamageAndStun()
    {
        float baseDamage = 100f;
        float baseStun = 1.0f;

        offGuard.Activate();
        offGuard.stackNum = 1;

        float modifiedDamage = offGuard.GetModifiedDamage(baseDamage);
        float modifiedStun = offGuard.GetModifiedStunDuration(baseStun);

        Assert.AreNotEqual(baseDamage, modifiedDamage);
        Assert.AreNotEqual(baseStun, modifiedStun);

        offGuard.Deactivate();

        float resultDamage = offGuard.GetModifiedDamage(baseDamage);
        float resultStun = offGuard.GetModifiedStunDuration(baseStun);

        Assert.AreEqual(baseDamage, resultDamage, 0.001f);
        Assert.AreEqual(baseStun, resultStun, 0.001f);
    }
}
