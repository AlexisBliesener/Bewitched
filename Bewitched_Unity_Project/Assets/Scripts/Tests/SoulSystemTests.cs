using System.Collections;
using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Unit tests for the SoulSystem and SoulDrop classes
/// This will test singleton behavior, soul currency management,soul spawning, and soul drop attraction and collection.
/// </summary>
public class SoulSystemTests
{
    private GameObject soulSystemObject;
    private SoulSystem soulSystem;
    private GameObject mockPlayer;
    private GameObject soulPrefab;
    private GameObject levelManagerObj;
    private LevelManager levelManager;
    /// <summary>
    /// Mock LevelManager that only overrides scene loading for testing.
    /// </summary>
    public class MockLevelManager : LevelManager
    {
        [Tooltip("Last level name that was loaded")]
        public string lastLoadedLevel;
        [Tooltip("Number of times scene loading was called")]
        public int sceneLoadCallCount = 0;

        /// <summary>
        /// Override the LoadScene method to prevent actual scene loading in tests
        /// </summary>
        protected override void LoadScene(string levelName)
        {
            lastLoadedLevel = levelName;
            sceneLoadCallCount++;
        }
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
    [SetUp]
    public void Setup()
    {
        // Destroy any existing SoulSystem instance to prevent conflicts
        if (SoulSystem.Instance != null)
        {
            Object.DestroyImmediate(SoulSystem.Instance.gameObject);
        }

        // Create the soul system
        soulSystemObject = new GameObject("SoulSystem");
        soulSystem = soulSystemObject.AddComponent<SoulSystem>();

        // Use a simple cube prefab as soul placeholder
        soulPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        soulPrefab.name = "MockSoulPrefab";
        soulPrefab.transform.position = new Vector3(1000, 1000, 1000); // move it out of view so it doesn't get picked up by the player :) 
        soulPrefab.AddComponent<SoulDrop>();
        soulSystem.SetSoulPrefab(soulPrefab);

        // Reset souls
        soulSystem.ResetSouls();

        // Create mock player
        mockPlayer = new GameObject("MockPlayer");
        mockPlayer.transform.position = Vector3.zero;

        // Mock PlayerController singleton
        MockPlayerController mockPlayerController = mockPlayer.AddComponent<MockPlayerController>();
        // I have spent hours (an hour actually) trying to figure out how to set the static instance property without changing the code of the PlayerController class
        // and I found this solution here to accesss the private setter using reflection :) 
        PropertyInfo instanceProperty = typeof(PlayerController).GetProperty("instance", BindingFlags.Static | BindingFlags.Public);
        instanceProperty.SetValue(null, mockPlayerController);

        Character character = mockPlayer.AddComponent<MockCharacter>();
        PlayerController.instance.currentCharacter = character;

        // Create mock level manager for testing that the soul system resets on level start
        levelManagerObj = new GameObject("LevelManager");
        levelManager = levelManagerObj.AddComponent<MockLevelManager>();
        levelManager.levelData = new LevelData(); // avoid JSON loading
    }

    [TearDown]
    public void TearDown()
    {
        if (soulSystemObject != null)
            Object.DestroyImmediate(soulSystemObject);
        if (mockPlayer != null)
            Object.DestroyImmediate(mockPlayer);
        if (soulPrefab != null)
            Object.DestroyImmediate(soulPrefab);
        if (levelManagerObj != null)
            Object.DestroyImmediate(levelManagerObj);
        SoulDrop[] soulDrops = Object.FindObjectsOfType<SoulDrop>();
        foreach (SoulDrop drop in soulDrops)
        {
            Object.DestroyImmediate(drop.gameObject);
        }
    }

    /// <summary>
    /// Test that SoulSystem creates a singleton instance correctly
    /// </summary>
    [Test]
    public void Instance_CreatesSingletonInstance()
    {
        Assert.IsNotNull(SoulSystem.Instance);
        Assert.AreEqual(soulSystem, SoulSystem.Instance);
    }

    /// <summary>
    /// Test that creating multiple SoulSystem objects destroy duplicate instances
    /// </summary>
    [UnityTest]
    public IEnumerator Instance_DestroysDuplicateInstances()
    {
        GameObject duplicateObject = new GameObject("DuplicateSoulSystem");
        SoulSystem duplicateSystem = duplicateObject.AddComponent<SoulSystem>();

        yield return null; // wait for Awake

        Assert.AreEqual(soulSystem, SoulSystem.Instance);
        Assert.IsTrue(duplicateObject == null); // should be destroyed
    }

    /// <summary>
    /// Test that AddSouls increases soul currency obviously 
    /// </summary>
    [Test]
    public void AddSouls_IncreasesCurrency()
    {
        soulSystem.AddSouls(5);
        Assert.AreEqual(5, soulSystem.GetSoulCurrency());
    }

    /// <summary>
    /// Test that UseSoulCurrency decreases soul currency and does not go below zero
    /// </summary>
    [Test]
    public void UseSoulCurrency_DoesNotGoBelowZero()
    {
        soulSystem.AddSouls(3);
        soulSystem.UseSoulCurrency(5);

        Assert.AreEqual(0, soulSystem.GetSoulCurrency());
    }

    /// <summary>
    /// Test that ResetSouls set the currency to zero
    /// </summary>
    [Test]
    public void ResetSouls_SetsCurrencyToZero()
    {
        soulSystem.AddSouls(10);
        soulSystem.ResetSouls();

        Assert.AreEqual(0, soulSystem.GetSoulCurrency());
    }

    /// <summary>
    /// Test that SpawnSoul instantiates new soul prefab in the scene
    /// </summary>
    [UnityTest]
    public IEnumerator SpawnSoul_InstantiatesSoulPrefabs()
    {
        Vector3 spawnPos = Vector3.zero;

        soulSystem.SpawnSoul(spawnPos);
        yield return null;

        SoulDrop[] soulDrops = Object.FindObjectsOfType<SoulDrop>();
        Assert.IsTrue(soulDrops.Length > 0);
    }

    /// <summary>
    /// Test that SoulDrop gets attracted to player when in range
    /// </summary>
    [UnityTest]
    public IEnumerator SoulDrop_AttractedToPlayerInRange()
    {
        // Create soul drop
        GameObject soulDropObj = new GameObject("SoulDrop");
        SoulDrop soulDrop = soulDropObj.AddComponent<SoulDrop>();
        soulDropObj.transform.position = new Vector3(4f, 0f, 0f); // within range

        Vector3 initialPos = soulDropObj.transform.position;

        yield return null; // update one frame to change the position

        Assert.AreNotEqual(initialPos, soulDropObj.transform.position); // moved closer
    }

    /// <summary>
    /// Test that SoulDrop add a soul to SoulSystem when collected
    /// </summary>
    [UnityTest]
    public IEnumerator SoulDrop_AddsSoulOnPickup()
    {
        soulSystem.ResetSouls();

        GameObject soulDropObj = new GameObject("SoulDrop");
        SoulDrop soulDrop = soulDropObj.AddComponent<SoulDrop>();
        soulDropObj.transform.position = mockPlayer.transform.position + Vector3.one * 0.5f; // close to player

        yield return new WaitForSeconds(0.5f);

        Assert.AreEqual(1, soulSystem.GetSoulCurrency()); // pickup add a soul
        Assert.IsTrue(soulDropObj == null); // destroyed after pickup
    }
    /// <summary>
    /// Test that Enemy drops soul on death
    /// </summary>
    [UnityTest]
    public IEnumerator Enemy_DropsSoulOnDeath()
    {
        // Create mock enemy
        GameObject enemyObj = new GameObject("MockEnemy");
        MockCharacter enemy = enemyObj.AddComponent<MockCharacter>(); // Using your MockCharacter for simplicity
        enemyObj.transform.position = Vector3.zero;

        // Ensure soul count is zero initially
        soulSystem.ResetSouls();
        Assert.AreEqual(0, soulSystem.GetSoulCurrency());

        // Simulate enemy death
        enemy.Die();

        yield return null; // wait one frame for SpawnSoul

        // There should be at least one SoulDrop object in the scene
        SoulDrop[] soulDrops = Object.FindObjectsOfType<SoulDrop>();
        Assert.IsTrue(soulDrops.Length > 0, "Soul should be spawned when enemy dies");
    }
    /// <summary>
    /// Test that SoulSystem resets soul currency when level starts from menu
    /// </summary>
    [UnityTest]
    public IEnumerator LevelManager_ResetsSoulSystemOnStartFromMenu()
    {
        // Add some souls
        soulSystem.AddSouls(10);
        Assert.AreEqual(10, soulSystem.GetSoulCurrency());

        // Simulate starting level from menu which will call ResetLevel
        levelManagerObj.SendMessage("ResetLevel", SendMessageOptions.DontRequireReceiver);
        yield return null;

        // SoulSystem should be reset
        Assert.AreEqual(0, soulSystem.GetSoulCurrency());
    }
    /// <summary> 
    /// Test that player can collect multiple souls if they are close enough
    /// </summary>
    [UnityTest]
    public IEnumerator Player_CanCollectMultipleSouls()
    {
        soulSystem.ResetSouls();

        int numSouls = 5;
        for (int i = 0; i < numSouls; i++)
        {
            GameObject soulObj = new GameObject($"Soul N. {i}");
            soulObj.AddComponent<SoulDrop>();
            soulObj.transform.position = mockPlayer.transform.position + Vector3.one * 0.1f * i; // close enough to be collected
        }

        yield return new WaitForSeconds(0.5f); // allow collection

        Assert.AreEqual(numSouls, soulSystem.GetSoulCurrency(), "Player should collect all souls");
    }


}
