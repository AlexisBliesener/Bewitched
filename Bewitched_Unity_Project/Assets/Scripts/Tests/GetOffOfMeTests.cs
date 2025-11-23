using NUnit.Framework;
using UnityEngine;
using System.Reflection;

/// <summary>
/// Test cases for the GetOffOfMe upgrade
/// </summary>
public class GetOffOfMeTests
{
    private GetOffOfMe getOffOfMe;
    private GameObject gameObject;

    [SetUp]
    public void SetUp()
    {
        gameObject = new GameObject("GetOffOfMeTestObject");
        getOffOfMe = gameObject.AddComponent<GetOffOfMe>();

        GetOffOfMe.instance = getOffOfMe;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(gameObject);
    }

    /// <summary>
    /// Ensures that when Adrenaline's Awake() runs, the singleton instance
    /// is correctly set to the current Adrenaline component.
    /// </summary>
    [Test]
    public void GetOffOfMe_SetsSingletonInstance_OnAwake()
    {
        Assert.AreEqual(getOffOfMe, GetOffOfMe.instance);
    }

    /// <summary>
    /// When the upgrade is not active, it returns the base knockback
    /// </summary>
    [Test]
    public void GetModifiedKnockback_ReturnsBaseKnockback_WhenInactive()
    {
        float baseKnockback = 10f;
        getOffOfMe.stackNum = 1;

        float result = getOffOfMe.GetModifiedKnockback(baseKnockback);

        Assert.AreEqual(baseKnockback, result, 0.001f);
    }

    /// <summary>
    /// Stack 0 uses the first knockback multiplier
    /// </summary>
    [Test]
    public void GetModifiedKnockback_Stack0_UsesFirstMultiplier()
    {
        float baseKnockback = 10f;

        getOffOfMe.Activate();
        getOffOfMe.stackNum = 0;

        float result = getOffOfMe.GetModifiedKnockback(baseKnockback);

        Assert.AreEqual(12.5f, result, 0.001f); // 10 * 1.25
    }

    /// <summary>
    /// Stack 1 uses the second knockback multiplier
    /// </summary>
    [Test]
    public void GetModifiedKnockback_Stack1_UsesSecondMultiplier()
    {
        float baseKnockback = 10f;

        getOffOfMe.Activate();
        getOffOfMe.stackNum = 1;

        float result = getOffOfMe.GetModifiedKnockback(baseKnockback);

        Assert.AreEqual(15f, result, 0.001f); // 10 * 1.5
    }

    /// <summary>
    /// Stack 2 uses the third knockback multiplier
    /// </summary>
    [Test]
    public void GetModifiedKnockback_Stack2_UsesThirdMultiplier()
    {
        float baseKnockback = 10f;

        getOffOfMe.Activate();
        getOffOfMe.stackNum = 2;

        float result = getOffOfMe.GetModifiedKnockback(baseKnockback);

        Assert.AreEqual(17.5f, result, 0.001f); // 10 * 1.75
    }

    /// <summary>
    /// Stack 3 (not exist) uses the last knockback multiplier
    /// </summary>
    [Test]
    public void GetModifiedKnockback_StackTooHigh_UsesLastMultiplier()
    {
        float baseKnockback = 10f;

        getOffOfMe.Activate();
        getOffOfMe.stackNum = 99; // higher than knockBackMultiplier length

        float result = getOffOfMe.GetModifiedKnockback(baseKnockback);

        Assert.AreEqual(17.5f, result, 0.001f); // 10 * 1.75 (last multiplier)
    }

    /// <summary>
    /// it should stops modifying knockback when deactivated
    /// </summary>
    [Test]
    public void Deactivate_StopsModifyingKnockback()
    {
        float baseKnockback = 10f;

        getOffOfMe.Activate();
        getOffOfMe.stackNum = 0;
        
        float modified = getOffOfMe.GetModifiedKnockback(baseKnockback);
        Assert.AreNotEqual(baseKnockback, modified);

        getOffOfMe.Deactivate();
        float result = getOffOfMe.GetModifiedKnockback(baseKnockback);

        Assert.AreEqual(baseKnockback, result, 0.001f);
    }
}
