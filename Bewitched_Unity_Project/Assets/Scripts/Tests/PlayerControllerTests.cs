using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// Unit tests for the PlayerController class.
/// Mocks dependencies like Hag, HealthBar, CooldownDisplay, and Character for isolated testing.
/// </summary>
public class PlayerControllerTests
{
    [Tooltip("The Player GameObject under test")]
    private GameObject playerObj;
    [Tooltip("The PlayerController instance under test")]
    private PlayerController controller;

    #region Mock Classes

    /// <summary>
    /// Mock version of Hag that skips Awake logic.
    /// </summary>
    public class MockHag : Hag
    {
        protected new void Awake() { }
    }

    /// <summary>
    /// Mock HealthBar that avoids null references in tests.
    /// </summary>
    public class MockHealthBar : HealthBar
    {
        public new void SetCharacter(Character character) { }
        public new void SetValues() { }
        public new void OnEnable() { }
        public new void Update() { }
    }

    /// <summary>
    /// Mock CooldownDisplay with a dummy Image to prevent null references.
    /// </summary>
    public class MockCooldownDisplay : CooldownDisplay
    {
        [Tooltip("Mock UI Image for ability display")]
        public Image abilityImage;

        public void SetCooldownCover(float val) { }
        public void SetAbleToUse(bool val) { }
    }

    /// <summary>
    /// Mock PlayerController that skips FixedUpdate during tests.
    /// </summary>
    public class MockPlayerController : PlayerController
    {
        void FixedUpdate() { } // skip updating in tests
    }

    /// <summary>
    /// Mock Character class to create a non abstract character class.
    /// </summary>
    public class MockCharacter : Character
    {
        public override void Die() { }
        public override void PrimaryAttack() { }
        public override void SecondaryAttack() { }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a mocked cooldown display with an Image component.
    /// </summary>
    /// <param name="name">Name of the GameObject.</param>
    /// <returns>A MockCooldownDisplay with a valid Image component.</returns>
    private MockCooldownDisplay CreateCooldownDisplay(string name)
    {
        var coolDown = new GameObject(name).AddComponent<MockCooldownDisplay>();
        coolDown.abilityImage = new GameObject("Image").AddComponent<Image>();
        return coolDown;
    }

    /// <summary>
    /// Creates a mocked HealthBar.
    /// </summary>
    /// <param name="name">Name of the GameObject.</param>
    /// <returns>A MockHealthBar instance.</returns>
    private MockHealthBar CreateHealthBar(string name)
    {
        return new GameObject(name).AddComponent<MockHealthBar>();
    }

    #endregion

    #region Test Setup / Teardown

    /// <summary>
    /// Sets up the test environment and initializes PlayerController with mocks.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        playerObj = new GameObject("Player");
        playerObj.SetActive(false);

        // Create Hag
        var hagObj = new GameObject("Hag");
        var hag = hagObj.AddComponent<MockHag>();

        // Create HealthBars
        var hagHealthBar = CreateHealthBar("HagHealthBar");
        var secondaryHealthBar = CreateHealthBar("SecondaryHealthBar");

        // Create Pause Menu
        var pauseMenu = new GameObject("PauseMenu");

        // Create Cooldown Displays
        controller = playerObj.AddComponent<MockPlayerController>();
        controller.primaryCooldownDisplay = CreateCooldownDisplay("PrimaryCooldown");
        controller.secondaryCooldownDisplay = CreateCooldownDisplay("SecondaryCooldown");

        // Assign dependencies
        controller.oldHag = hag;
        controller.currentCharacter = hag;
        controller.hagHealthBar = hagHealthBar.gameObject;

        controller.pauseMenu = pauseMenu;

        playerObj.SetActive(true);
    }

    /// <summary>
    /// Cleans up the test GameObject after each test.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        Object.Destroy(playerObj);
    }

    #endregion

    #region Lifecycle Tests

    /// <summary>
    /// Tests that PlayerController.Start() sets the static instance property.
    /// </summary>
    [UnityTest]
    public IEnumerator Start_SetsInstance()
    {
        controller.SendMessage("Start");
        yield return null;
        Assert.AreEqual(controller, PlayerController.instance);
    }

    /// <summary>
    /// Tests that Awake() initializes the current character and enables the Hag health bar.
    /// </summary>
    [UnityTest]
    public IEnumerator Awake_InitializesCharacter()
    {
        controller.SendMessage("Awake");
        yield return null;
        Assert.AreEqual(controller.oldHag, controller.currentCharacter);
        Assert.IsTrue(controller.hagHealthBar.activeSelf);
    }

    #endregion

    #region Utility Tests

    /// <summary>
    /// Tests that GetHag() returns the old Hag.
    /// </summary>
    [Test]
    public void GetHag_ReturnsOldHag()
    {
        Assert.AreEqual(controller.oldHag, controller.GetHag());
    }

    /// <summary>
    /// Tests that GetCurrentCharacter() returns the currently controlled character.
    /// </summary>
    [Test]
    public void GetCurrentCharacter_ReturnsCurrent()
    {
        Assert.AreEqual(controller.currentCharacter, controller.GetCurrentCharacter());
    }
    #endregion
}
