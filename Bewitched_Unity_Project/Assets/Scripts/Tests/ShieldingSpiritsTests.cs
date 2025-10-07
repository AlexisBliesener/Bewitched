using NUnit.Framework;
using UnityEngine;
using System.Reflection;
using UnityEngine.TestTools;
using System.Collections;

/// <summary>
/// Unit tests for the ShieldingSpirits upgrade.
/// </summary>
public class ShieldingSpiritsTests
{
    private ShieldingSpirits shieldingSpirits;
    private GameObject gameObject;
    private GameObject mockPlayer;
    private GameObject enemyObject;
    private Character mockEnemy;

    /// <summary>
    /// Mock Character class to create a non abstract character class.
    /// </summary>
    public class MockCharacter : Character
    {
        void Update() { }
        void FixedUpdate() { }
        protected override void OnDestroy() { }
        protected override void Awake() { }
        public override void Die() { }
        public override void PrimaryAttack() { }
        public override void SecondaryAttack() { }
    }
    public class MockHag : Hag
    {
        void Update() { }
        void FixedUpdate() { }
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
        void Awake() { } // skip Awake in tests
        void FixedUpdate() { } // skip updating in tests
    }

    [SetUp]
    public void SetUp()
    {
        // Create GameObject for ShieldingSpirits
        gameObject = new GameObject();
        shieldingSpirits = gameObject.AddComponent<ShieldingSpirits>();

        // Create mock player
        mockPlayer = new GameObject("MockPlayer");
        mockPlayer.AddComponent<CharacterController>();

        // Mock PlayerController singleton
        MockPlayerController mockPlayerController = mockPlayer.AddComponent<MockPlayerController>();
        PropertyInfo instanceProperty = typeof(PlayerController).GetProperty("instance", BindingFlags.Static | BindingFlags.Public);
        instanceProperty.SetValue(null, mockPlayerController);

        Character character = mockPlayer.AddComponent<MockCharacter>();
        PlayerController.instance.currentCharacter = character;

        // Create mock enemy
        enemyObject = new GameObject("Enemy");
        MockHealth mockHealth = enemyObject.AddComponent<MockHealth>();
        mockEnemy = enemyObject.AddComponent<MockCharacter>();
        mockEnemy.health = mockHealth;
        mockEnemy.health.SetMaxHealth(100f);
        mockEnemy.health.AddHealth(100f);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(gameObject);
        Object.DestroyImmediate(mockPlayer);
        Object.DestroyImmediate(enemyObject);
    }

    /// <summary>
    /// Confirms that Activate() enables the shield effect
    /// </summary>
    [Test]
    public void Activate_EnablesShieldEffect()
    {
        shieldingSpirits.Activate();

        FieldInfo activeField = typeof(ShieldingSpirits).GetField("active", BindingFlags.NonPublic | BindingFlags.Instance);
        bool isActive = (bool)activeField.GetValue(shieldingSpirits);

        Assert.IsTrue(isActive);
    }

    /// <summary>
    /// Verifies that Deactivate() disables the shield effect
    /// </summary>
    [Test]
    public void Deactivate_DisablesShieldEffect()
    {
        shieldingSpirits.Activate();
        shieldingSpirits.Deactivate();

        FieldInfo activeField = typeof(ShieldingSpirits).GetField("active", BindingFlags.NonPublic | BindingFlags.Instance);
        bool isActive = (bool)activeField.GetValue(shieldingSpirits);

        Assert.IsFalse(isActive);
    }

    /// <summary>
    /// Verifies that OnCharacterControlChange() does not apply shield when inactive.
    /// </summary>
    [Test]
    public void OnCharacterControlChange_DoesNotApplyShield_WhenInactive()
    {
        // Don't activate
        shieldingSpirits.stackNum = 0;

        shieldingSpirits.OnCharacterControlChange(mockEnemy);

        FieldInfo shieldCoroutineField = typeof(ShieldingSpirits).GetField("shieldCoroutine", BindingFlags.NonPublic | BindingFlags.Instance);
        Coroutine coroutine = (Coroutine)shieldCoroutineField.GetValue(shieldingSpirits);

        Assert.IsNull(coroutine);
    }

    /// <summary>
    /// Verifies that OnCharacterControlChange() does not apply shield when returning to Hag
    /// </summary>
    [Test]
    public void OnCharacterControlChange_DoesNotApplyShield_WhenReturningToHag()
    {
        shieldingSpirits.Activate();
        shieldingSpirits.stackNum = 0;

        GameObject hagObject = new GameObject("Hag");
        MockHag hag = hagObject.AddComponent<MockHag>();
        PlayerController.instance.oldHag = hag;

        shieldingSpirits.OnCharacterControlChange(hag);

        FieldInfo shieldCoroutineField = typeof(ShieldingSpirits).GetField("shieldCoroutine", BindingFlags.NonPublic | BindingFlags.Instance);
        Coroutine coroutine = (Coroutine)shieldCoroutineField.GetValue(shieldingSpirits);

        Assert.IsNull(coroutine);
        Object.DestroyImmediate(hagObject);
    }

    /// <summary>
    /// Ensures that possessing a new enemy apply a shield with correct amount.
    /// </summary>
    [UnityTest]
    public IEnumerator OnCharacterControlChange_AppliesShield_WithCorrectAmount()
    {
        shieldingSpirits.Activate();
        shieldingSpirits.stackNum = 0;

        float initialHealth = mockEnemy.health.GetHealth();
        float expectedShieldAmount = (initialHealth / 2f) * 1f; // stackNum 0, multiplier 1f

        shieldingSpirits.OnCharacterControlChange(mockEnemy);
        yield return null;

        float currentHealth = mockEnemy.health.GetHealth();
        float expectedTotalHealth = initialHealth + expectedShieldAmount;

        Assert.AreEqual(expectedTotalHealth, currentHealth, 0.001f);
    }

    /// <summary>
    /// Ensures that possessing a new enemy while shield is active it cancel previous shield
    /// </summary>
    [UnityTest]
    public IEnumerator OnCharacterControlChange_CancelsPreviousShield_WhenPossessingNewEnemy()
    {
        shieldingSpirits.Activate();
        shieldingSpirits.stackNum = 0;

        GameObject enemy1Object = new GameObject("Enemy1");
        MockHealth mockHealth = enemy1Object.AddComponent<MockHealth>();
        Character enemy1 = enemy1Object.AddComponent<MockCharacter>();
        enemy1.health = mockHealth;
        enemy1.health.SetMaxHealth(100f);
        enemy1.health.AddHealth(100f);

        shieldingSpirits.OnCharacterControlChange(enemy1);
        yield return null;

        FieldInfo shieldCoroutineField = typeof(ShieldingSpirits).GetField("shieldCoroutine", BindingFlags.NonPublic | BindingFlags.Instance);
        Coroutine firstCoroutine = (Coroutine)shieldCoroutineField.GetValue(shieldingSpirits);

        GameObject enemy2Object = new GameObject("Enemy2");
        MockHealth mockHealth2 = enemy2Object.AddComponent<MockHealth>();
        Character enemy2 = enemy2Object.AddComponent<MockCharacter>();
        enemy2.health = mockHealth2;
        enemy2.health.SetMaxHealth(100f);
        enemy2.health.AddHealth(100f);

        shieldingSpirits.OnCharacterControlChange(enemy2);
        yield return null;

        Coroutine secondCoroutine = (Coroutine)shieldCoroutineField.GetValue(shieldingSpirits);

        Assert.IsNotNull(secondCoroutine);
        Assert.AreNotEqual(firstCoroutine, secondCoroutine);

        Object.DestroyImmediate(enemy1Object);
        Object.DestroyImmediate(enemy2Object);
    }

    /// <summary>
    /// Verifies that Deactivate() stops the shield coroutine and restores max health.
    /// </summary>
    [UnityTest]
    public IEnumerator Deactivate_StopsShieldCoroutine_AndRestoresMaxHealth()
    {
        shieldingSpirits.Activate();
        shieldingSpirits.stackNum = 0;

        float originalMaxHealth = mockEnemy.health.GetMaxHealth();

        shieldingSpirits.OnCharacterControlChange(mockEnemy);
        yield return null;

        shieldingSpirits.Deactivate();
        yield return null;

        FieldInfo shieldCoroutineField = typeof(ShieldingSpirits).GetField("shieldCoroutine", BindingFlags.NonPublic | BindingFlags.Instance);
        Coroutine coroutine = (Coroutine)shieldCoroutineField.GetValue(shieldingSpirits);

        Assert.IsNull(coroutine);
        Assert.AreEqual(originalMaxHealth, mockEnemy.health.GetMaxHealth(), 0.001f);
    }

    /// <summary>
    /// Ensures that max health is correctly restored after shield duration expires.
    /// </summary>
    [UnityTest]
    public IEnumerator Shield_RestoresMaxHealth_AfterDurationExpires()
    {
        shieldingSpirits.Activate();
        shieldingSpirits.stackNum = 0;

        float originalMaxHealth = mockEnemy.health.GetMaxHealth();

        shieldingSpirits.OnCharacterControlChange(mockEnemy);
        yield return null;

        // Wait for shield duration to expire (5 seconds for stack 0)
        yield return new WaitForSeconds(5.5f);

        float finalMaxHealth = mockEnemy.health.GetMaxHealth();

        Assert.AreEqual(originalMaxHealth, finalMaxHealth, 0.001f);
    }

    /// <summary>
    /// Verifies that taking damage reduces the shield first before affecting base health.
    /// </summary>
    [UnityTest]
    public IEnumerator Shield_ReducesFirst_BeforeBaseHealth()
    {
        shieldingSpirits.Activate();
        shieldingSpirits.stackNum = 0;

        float initialHealth = 100f;
        float shieldAmount = 50f; // (100 / 2) * 1f

        shieldingSpirits.OnCharacterControlChange(mockEnemy);
        yield return null;

        // Deal 30 damage (should only affect shield)
        mockEnemy.health.SubHealth(30f);
        yield return null;

        float currentHealth = mockEnemy.health.GetHealth();
        float expectedHealth = initialHealth + shieldAmount - 30f; // 120

        Assert.AreEqual(expectedHealth, currentHealth, 0.001f);
    }
}