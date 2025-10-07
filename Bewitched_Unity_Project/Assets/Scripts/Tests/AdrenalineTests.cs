using NUnit.Framework;
using UnityEngine;
using System.Reflection;
using UnityEngine.TestTools;
using System.Collections;

/// <summary>
/// Unit tests for the Adrenaline upgrade.
/// </summary>
public class AdrenalineTests
{
    private Adrenaline adrenaline;
    private GameObject gameObject;
    private GameObject mockPlayer;
    /// <summary>
    /// Mock Character class to create a non abstract character class.
    /// </summary>
    public class MockCharacter : Character
    {

        void Update() { }
        void FixedUpdate()
        {

        }
        protected override void OnDestroy() { }

        protected override void Awake() { }
        public override void Die() { }
        public override void PrimaryAttack() { }
        public override void SecondaryAttack() { }
    }
    /// <summary>
    /// Mock PlayerController that skips FixedUpdate during tests.
    /// </summary>
    public class MockPlayerController : PlayerController
    {
        void Start() { } // skip Start in tests
        void Update() { } // skip updating in tests
        void Awake() // skip Awake in tests
        {

        }
        void FixedUpdate() { } // skip updating in tests
    }
    [SetUp]
    public void SetUp()
    {
        // Create GameObject for AdrenalineSpy
        gameObject = new GameObject();
        adrenaline = gameObject.AddComponent<Adrenaline>();
        // Reset singleton instance
        Adrenaline.instance = adrenaline;


        // Create mock player
        mockPlayer = new GameObject("MockPlayer");
        mockPlayer.AddComponent<CharacterController>();

        // Mock PlayerController singleton
        MockPlayerController mockPlayerController = mockPlayer.AddComponent<MockPlayerController>();
        PropertyInfo instanceProperty = typeof(PlayerController).GetProperty("instance", BindingFlags.Static | BindingFlags.Public);
        instanceProperty.SetValue(null, mockPlayerController);

        Character character = mockPlayer.AddComponent<MockCharacter>();

        PlayerController.instance.currentCharacter = character;
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
    public void Adrenaline_SetsSingletonInstance_OnAwake()
    {
        Assert.AreEqual(adrenaline, Adrenaline.instance);
    }


    /// <summary>
    /// Confirms that ApplyAdrenalineRush() is activates the buff.
    /// </summary>
    [Test]
    public void ApplyAdrenalineRush_ActivatesBuff_WhenActive()
    {
        adrenaline.Activate();
        adrenaline.stackNum = 0;

        adrenaline.ApplyAdrenalineRush();

        Assert.IsTrue(adrenaline.IsBuffActive());
    }

    /// <summary>
    /// Verifies that ApplyAdrenalineRush() does not activate buff when inactive.
    /// </summary>
    [Test]
    public void ApplyAdrenalineRush_DoesNotActivateBuff_WhenInactive()
    {
        // Don't activate
        adrenaline.stackNum = 0;

        adrenaline.ApplyAdrenalineRush();

        Assert.IsFalse(adrenaline.IsBuffActive());
    }

    /// <summary>
    /// Verifies that when Adrenaline buff is active with stack 0,
    /// the damage is multiplied by 1.25
    /// </summary>
    [Test]
    public void GetModifiedDamage_Multiplies1Point25()
    {
        adrenaline.Activate();
        adrenaline.stackNum = 0;
        adrenaline.ApplyAdrenalineRush();
        float baseDamage = 100f;

        float result = adrenaline.GetModifiedDamage(baseDamage);

        Assert.AreEqual(125f, result, 0.001f);
    }

    /// <summary>
    /// Verifies that when buff is not active,
    /// the damage is not modified.
    /// </summary>
    [Test]
    public void GetModifiedDamage_ReturnsBaseDamage_WhenBuffNotActive()
    {
        adrenaline.Activate();
        adrenaline.stackNum = 1;
        // Don't trigger buff
        float baseDamage = 100f;

        float result = adrenaline.GetModifiedDamage(baseDamage);

        Assert.AreEqual(baseDamage, result, 0.001f);
    }

    /// <summary>
    /// Verifies that Deactivate() also deactivates the buff.
    /// </summary>
    [Test]
    public void Deactivate_AlsoDeactivatesBuff()
    {
        adrenaline.Activate();
        adrenaline.ApplyAdrenalineRush();
        Assert.IsTrue(adrenaline.IsBuffActive());

        adrenaline.Deactivate();

        Assert.IsFalse(adrenaline.IsBuffActive());
    }

    /// <summary>
    /// Make sure the buff deactivates after exceeding its duration.
    /// </summary>
    [Test]
    public void Update_DisablesBuff_AfterDuration()
    {
        adrenaline.Activate();  
        adrenaline.ApplyAdrenalineRush();

        FieldInfo buffActivatedField = typeof(Adrenaline).GetField("buffActivatedTime", BindingFlags.NonPublic | BindingFlags.Instance);
        buffActivatedField.SetValue(adrenaline, Time.time - 10f);

        adrenaline.SendMessage("Update");
        Assert.IsFalse(adrenaline.IsBuffActive());
    }

    /// <summary>
    /// Ensure that possessing a new enemy resets the buff duration timer.
    /// </summary>
    [UnityTest]
    public IEnumerator OnCharacterControlChange_ResetsBuffTimer_WhenNewEnemyPossessed()
    {
        adrenaline.Activate();
        adrenaline.ApplyAdrenalineRush();

        FieldInfo buffActivatedField = typeof(Adrenaline).GetField("buffActivatedTime", BindingFlags.NonPublic | BindingFlags.Instance);
        float oldTime = Time.time - 10f;
        buffActivatedField.SetValue(adrenaline, oldTime);

        // Simulate possessing a new enemy
        Character newEnemy = new GameObject("Enemy").AddComponent<MockCharacter>();
        adrenaline.OnCharacterControlChange(newEnemy);
        // go to the next frame
        yield return null;
        // Get the new activation time
        float newTime = (float)buffActivatedField.GetValue(adrenaline);

        Assert.Greater(newTime, oldTime);
        Object.DestroyImmediate(newEnemy.gameObject);
    }
}
