using NUnit.Framework;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Contains unit tests for the Character class.
/// Validates core gameplay functionality such as health management, attacks,
/// animation states, and save/load behavior.
/// </summary>
public class CharacterTests
{
    /// <summary>
    /// A lightweight concrete subclass Character for testing purposes.
    /// This class overrides abstract members and exposes protected values so
    /// they can be verified during testing.
    /// </summary>
    private class TestCharacter : Character
    {
        [Tooltip("Tracks whether Die() was called.")]
        public bool dieCalled = false;
        [Tooltip("Tracks whether PrimaryAttack() was called.")]
        public bool primaryCalled = false;
        [Tooltip("Tracks whether SecondaryAttack() was called.")]
        public bool secondaryCalled = false;
        [Tooltip("Tracks whether Explode() was called.")]
        public bool explodeCalled = false;
        [Tooltip("Tracks whether SetControlled() was invoked.")]
        public bool controlledSet = false;
        [Tooltip("Provides access to the protected hitStunActual instance for verification.")]
        public GameObject HitStun => hitStunActual;
        [Tooltip("Exposes the releasePrimaryImm flag.")]
        public bool ReleasePrimaryImmFlag => releasePrimaryImm;
        [Tooltip("Exposes the releaseSecondaryImm flag.")]
        public bool ReleaseSecondaryImmFlag => releaseSecondaryImm;
        [Tooltip("Gets the character's current health.")]
        public float CurrentHealth => health.GetHealth();

        /// <summary>
        /// Overrides Character.Die() for testing; marks that Die() was called.
        /// </summary>
        public override void Die()
        {
            dieCalled = true;
        }

        /// <summary>
        /// Overrides Character.PrimaryAttack() for testing; marks that PrimaryAttack() was called.
        /// </summary>
        public override void PrimaryAttack()
        {
            primaryCalled = true;
        }

        /// <summary>
        /// Overrides Character.SecondaryAttack() for testing; marks that SecondaryAttack() was called.
        /// </summary>
        public override void SecondaryAttack()
        {
            secondaryCalled = true;
        }

        /// <summary>
        /// Overrides Character.Explode() for testing; marks that Explode() was called.
        /// </summary>
        public override void Explode()
        {
            explodeCalled = true;
        }

        /// <summary>
        /// Overrides Character.SetControlled() for testing; records the controlled state.
        /// </summary>
        /// <param name="v">The value to set for controlled state.</param>
        public override void SetControlled(bool v)
        {
            controlledSet = v;
        }

        /// <summary>
        /// Set the health controller instance for this character.
        /// </summary>
        public void SetHealthController(HealthController hc)
        {
            health = hc;
            SubscribeHealth(hc);
        }
        /// <summary>
        /// Subscribe to events from a given HealthController instance.
        /// </summary>
        public void SubscribeHealth(HealthController hc)
        {
            health.OnDamaged += OnDamaged;
            health.OnDeath += OnDeath;
        }
        /// <summary>
        /// Unsubscribe to events from a given HealthController instance.
        /// </summary>
        public void UnsubscribeHealth(HealthController hc)
        {
            health.OnDamaged -= OnDamaged;
            health.OnDeath -= OnDeath;
        }

        /// <summary>
        /// Overrides Character.OnDamaged() for testing; marks that CreateHitStun() was called.
        /// </summary>
        protected override void OnDamaged(float amount)
        {
            CreateHitStun();
        }
        /// <summary>
        /// Overrides Character.OnDeath() for testing; marks that Die() was called.
        /// </summary>
        protected override void OnDeath()
        {
            Die();
        }

        /// <summary>
        /// Creates a dummy hitstun GameObject to simulate hitstun logic.
        /// </summary>
        public override void CreateHitStun() { hitStunActual = new GameObject("HitStun"); }
    }

    [Tooltip("Reference to the GameObject that holds the TestCharacter component.")]
    private GameObject testCharacterGameObject;

    [Tooltip("Reference to the TestCharacter instance used in tests.")]
    private TestCharacter testCharacter;


    /// <summary>
    /// Initializes a fresh TestCharacter before each test.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        testCharacterGameObject = new GameObject("Character");
        testCharacter = testCharacterGameObject.AddComponent<TestCharacter>();

        // Add and initialize HealthController for the character
        testCharacter.SetHealthController(testCharacterGameObject.AddComponent<HealthController>()); 

        // Setup default values
        testCharacter.characterName = "TestChar";
        testCharacter.health.SetMaxHealth(100f);
        testCharacter.health.SetCurrentHealth(100f);
        testCharacter.movementSpeed = 10;
        testCharacter.primaryCooldown = 1f;
        testCharacter.secondaryCooldown = 2f;
    }

    /// <summary>
    /// Cleans up the created GameObject after each test.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        testCharacter.UnsubscribeHealth(testCharacter.health);
        Object.Destroy(testCharacterGameObject);
    }

    #region Health 

    /// <summary>Health getter should return the current health.</summary>
    [Test]
    public void GetHealth_ReturnsCurrentHealth()
    {
        Assert.AreEqual(100, testCharacter.health.GetHealth());
    }

    /// <summary>Adding health should never exceed the maximum health.</summary>
    [Test]
    public void AddHealth_CapsAtMax()
    {
        testCharacter.health.AddHealth(50f);
        Assert.AreEqual(100f, testCharacter.health.GetHealth());
    }

    /// <summary>Subtracting more than current health should trigger Die.</summary>
    [Test]
    public void SubHealth_ReducesAndDies()
    {
        testCharacter.health.SubHealth(200);
        Assert.IsTrue(testCharacter.dieCalled);
    }

    /// <summary>Subtracting health should create a hitstun object.</summary>
    [Test]
    public void SubHealth_CreatesHitStun()
    {
        testCharacter.health.SubHealth(10);
        Assert.IsNotNull(testCharacter.HitStun);
    }

    /// <summary>DrainLife should kill without generating hitstun.</summary>
    [Test]
    public void DrainLife_KillsWithoutHitStun()
    {
        testCharacter.health.DrainLife(200);
        Assert.IsTrue(testCharacter.dieCalled);
        Assert.IsNull(testCharacter.HitStun);
    }

    /// <summary>SetHealthToMax should fully restore health.</summary>
    [Test]
    public void SetHealthToMax_ResetsHealth()
    {
        testCharacter.health.SubHealth(20);
        testCharacter.health.SetHealthToMax();
        Assert.AreEqual(100, testCharacter.health.GetHealth());
    }

    /// <summary>Max health getter should return the configured maximum.</summary>
    [Test]
    public void GetMaxHealth_ReturnsMax()
    {
        Assert.AreEqual(100, testCharacter.health.GetMaxHealth());
    }

    #endregion
    #region Alive/Team

    /// <summary>Character should start alive by default.</summary>
    [Test]
    public void IsDead_FalseInitially()
    {
        Assert.IsFalse(testCharacter.health.IsDead);
    }

    /// <summary>Setting team ID should update the property.</summary>
    [Test]
    public void SetTeamID_SetsValue()
    {
        testCharacter.SetTeamID(5);
        Assert.AreEqual(5, testCharacter.teamID);
    }

    #endregion
    #region Attacks

    /// <summary>BeginSecondary should eventually call Character.SecondaryAttack.</summary>
    [UnityTest]
    public IEnumerator BeginSecondary_CallsSecondaryAttack()
    {
        yield return testCharacter.StartCoroutine(testCharacter.BeginSecondary());
        Assert.IsTrue(testCharacter.secondaryCalled);
    }


    #endregion
    #region Status setters 

    /// <summary>Setting primary status should mark secondary unusable.</summary>
    [Test]
    public void SetPrimaryStatus_SetsFlag()
    {
        testCharacter.SetPrimaryStatus(true);
        Assert.IsFalse(testCharacter.CheckSecondaryUsable());
    }

    /// <summary>Setting secondary status should mark primary unusable.</summary>
    [Test]
    public void SetSecondaryStatus_SetsFlag()
    {
        testCharacter.SetSecondaryStatus(true);
        Assert.IsFalse(testCharacter.CheckPrimaryUsable());
    }

    #endregion
    #region Animations


    #endregion
    #region JSON

    /// <summary>
    /// Saving should create a JSON file, and loading should restore prior values.
    /// </summary>
    [Test]
    public void SaveAndLoadJson_WritesAndReads()
    {
        // Save JSON
        testCharacter.SaveToJson();
        string path = Path.Combine(Application.dataPath, "JSON/CharacterStats", testCharacter.characterName + ".json");
        Assert.IsTrue(File.Exists(path));

        // Modify and reload; value should revert to saved state
        testCharacter.movementSpeed = 123;
        testCharacter.LoadFromJson();
        Assert.AreNotEqual(123, testCharacter.movementSpeed);
    }

    #endregion
}
