using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Test cases for the EventSystemRoom1 class
/// </summary>
public class EventSystemRoom1Tests
{
    private GameObject roomObject;
    private EventSystemRoom1 eventSystemRoom;
    private EventEnemy enemyEvent;
    private EnemySpawner enemySpawner;
    private GameObject cutScene;
    private PlayableDirector director;
    private GameObject hud;
    private TestDoor testDoor;
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
    public class MockEnemy : Enemy
    {
        protected override void Awake() { }
        public override void Die() { }
        public override void PrimaryAttack() { }
        public override void SecondaryAttack() { }
        protected override void OnDestroy() { }

        public override void SetControlled(bool val) { }
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
    /// Simple test door mock that implement IDoor
    /// </summary>
    public class TestDoor : MonoBehaviour, IDoor
    {
        public bool IsLocked = true;

        public void Unlock()
        {
            IsLocked = false;
        }
        public void Lock()
        {
            IsLocked = true;
        }
    }
    [SetUp]
    public void Setup()
    {
        roomObject = new GameObject("EventSystemRoom1");
        eventSystemRoom = roomObject.AddComponent<EventSystemRoom1>();

        // Mock enemy
        GameObject enemyObj = new GameObject("EnemyEvent");
        MockEnemy enemy = enemyObj.AddComponent<MockEnemy>();
        enemyEvent = enemyObj.AddComponent<EventEnemy>();
        typeof(EventEnemy).GetField("enemyForEvent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(enemyEvent, enemy);
        enemyEvent.GetEnemy().health = enemyObj.AddComponent<EventHealth>();
        enemyEvent.GetEnemy().health.SetMaxHealth(200);

        // Mock spawner
        GameObject spawnerObj = new GameObject("Spawner");
        enemySpawner = spawnerObj.AddComponent<EnemySpawner>();

        // Mock cutscene
        cutScene = new GameObject("Cutscene");
        director = cutScene.AddComponent<PlayableDirector>();

        // Mock HUD
        hud = new GameObject("HUD");

        // Mock Door
        testDoor = new GameObject("Door").AddComponent<TestDoor>();


        typeof(EventSystemRoom1).GetField("enemyEvent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(eventSystemRoom, enemyEvent);

        typeof(EventSystemRoom1).GetField("enemySpawner", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(eventSystemRoom, enemySpawner);

        typeof(EventSystemRoom1).GetField("cutScene", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(eventSystemRoom, cutScene);

        typeof(EventSystemRoom1).GetField("director", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(eventSystemRoom, director);

        typeof(EventSystemRoom1).GetField("hud", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(eventSystemRoom, hud);

        typeof(EventSystemRoom1).GetField("door", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(eventSystemRoom, testDoor);

        // Create mock player
        mockPlayer = new GameObject("MockPlayer");
        mockPlayer.transform.position = Vector3.zero;

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
        Object.DestroyImmediate(roomObject);
        Object.DestroyImmediate(enemyEvent.gameObject);
        Object.DestroyImmediate(enemySpawner.gameObject);
        Object.DestroyImmediate(cutScene);
        Object.DestroyImmediate(hud);
        Object.DestroyImmediate(testDoor.gameObject);
    }

    /// <summary>
    /// Test that StartCutScene disables HUD and activate the cutscene
    /// </summary>
    [Test]
    public void StartCutScene_DisablesHudAndPlaysCutscene()
    {
        hud.SetActive(true);
        cutScene.SetActive(false);

        roomObject.SendMessage("StartCutScene");

        Assert.IsFalse(hud.activeSelf);
        Assert.IsTrue(cutScene.activeSelf);
    }

    /// <summary>
    /// Test that OnCutsceneFinished enables HUD and changes fight state to Fighting
    /// </summary>
    [Test]
    public void OnCutsceneFinished_StartsFightAndEnablesHud()
    {
        hud.SetActive(false);
        cutScene.SetActive(true);

        roomObject.SendMessage("OnCutsceneFinished", director);

        Assert.IsTrue(hud.activeSelf);
        Assert.IsFalse(cutScene.activeSelf);
    }

    /// <summary>
    /// Test that Update sets enemy to Dizzy state after enough damage
    /// </summary>
    [Test]
    public void Update_SetsEnemyToDizzyAfterEnoughDamage()
    {
        enemyEvent.GetEnemy().health.SubHealth(120); // damage > damageToPossess

        System.Enum newValue = (System.Enum)System.Enum.Parse(typeof(EventSystemRoom1).GetNestedType("FightState", System.Reflection.BindingFlags.NonPublic), "Fighting");
        typeof(EventSystemRoom1).GetField("fightState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(eventSystemRoom, newValue);

        eventSystemRoom.SendMessage("Update");
        Assert.AreEqual(EventEnemy.EventEnemyState.Dizzy, enemyEvent.GetState());
    }

    /// <summary>
    /// Test that EndFight set the enemy to Possessed and unlocks the door
    /// </summary>
    [Test]
    public void EndFight_SetsEnemyPossessedAndUnlocksDoor()
    {
        testDoor.IsLocked = true;

        eventSystemRoom.EndFight();

        Assert.AreEqual(EventEnemy.EventEnemyState.Possessed, enemyEvent.GetState());
        Assert.IsFalse(testDoor.IsLocked);
    }
}



