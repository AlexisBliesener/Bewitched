using NUnit.Framework;
using UnityEngine;
using System.Reflection;

/// <summary> 
/// Unit tests for the ClearFocus upgrade.
/// </summary>
public class ClearFoucsTests
{
    /// <summary>
    /// Mock version of PossessionAbility to test ClearFocus upgrade.
    /// </summary>
    public class MockPossessionAbility : PossessionAbility
    {
        private void Awake()
        {
            instance = this;
        }
    }
    private GameObject go;
    private ClearFocus clearFocus;
    private MockPossessionAbility mockPossession;

    [SetUp]
    public void SetUp()
    {

        // Create mock PossessionAbility
        GameObject possessionGo = new GameObject("MockPossessionAbility");
        mockPossession = possessionGo.AddComponent<MockPossessionAbility>();
        mockPossession.SetFocusTime(1f);
        // Create ClearFocus object
        go = new GameObject("ClearFocusTest");
        clearFocus = go.AddComponent<ClearFocus>();
        clearFocus.stackNum = 1;

        // Reset singleton
        PossessionAbility.instance = mockPossession;
        ClearFocus.instance = clearFocus;

        // call Start manually 
        typeof(ClearFocus).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(clearFocus, null);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(mockPossession.gameObject);
    }

    /// <summary>
    /// Make sure that the singleton instance is set correctly.
    /// </summary>
    [Test]
    public void Awake_SetsSingletonInstance()
    {
        Assert.AreEqual(clearFocus, ClearFocus.instance);
    }
    /// <summary>
    /// Make sure that activating the upgrade set the active field to true and applied the upgrade.
    /// </summary>
    [Test]
    public void Activate_SetsActiveTrue_AndAppliesUpgrade()
    {

        clearFocus.Activate();
        FieldInfo activeField = typeof(ClearFocus).GetField("active", BindingFlags.NonPublic | BindingFlags.Instance);
        bool isActive = (bool)activeField.GetValue(clearFocus);

        Assert.IsTrue(isActive);
        Assert.Greater(mockPossession.GetFocusTime(), 0.89f);
    }

    /// <summary>
    /// Make sure that deactivating the upgrade set the active field to false.
    /// </summary>
    [Test]
    public void Deactivate_SetsActiveFalse()
    {
        clearFocus.Activate();
        clearFocus.Deactivate();

        FieldInfo activeField = typeof(ClearFocus).GetField("active", BindingFlags.NonPublic | BindingFlags.Instance);
        bool isActive = (bool)activeField.GetValue(clearFocus);

        Assert.IsFalse(isActive);
    }
    /// <summary>
    /// Make sure that the focus time is reduced by the stack number.
    /// </summary>
    [Test]
    public void ApplyUpgrade_MultipliesFocusTimeReduction()
    {
        clearFocus.stackNum = 0;
        clearFocus.Activate();

        float reducedTime = mockPossession.GetFocusTime();

        // Act with higher stack (if multiple values exist in array)
        float[] focusTimeReduction = (float[])typeof(ClearFocus).GetField("focusTimeReduction", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(clearFocus);
        if (focusTimeReduction.Length > 0)
        {
            clearFocus.stackNum = Mathf.Min(1, focusTimeReduction.Length - 1);
        }
        else
        {
            clearFocus.stackNum = 1;
        }

        clearFocus.Activate();
        float reducedTime2 = mockPossession.GetFocusTime();

        Assert.LessOrEqual(reducedTime2, reducedTime);
    }
}
