using NUnit.Framework;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Unit tests for the Ogre class.
/// </summary>
public class OgreTests
{
    private GameObject ogreGameObject;
    private MockOgre testOgre;
    private GameObject playerGameObject;
    private GameObject attackIndicatorPrefab;
    private GameObject mockPlayer;
    private GameObject batHitboxPrefab;
    private GameObject batPivot;

    /// <summary>
    /// Creates a simple mock character used as a target for the ogre.
    /// </summary>
    private class MockCharacter : Character
    {
        public bool setAttackerCalled = false;
        public Character lastAttacker;

        public override void Die() { }
        public override void PrimaryAttack() { }
        public override void SecondaryAttack() { }

        public override void SetControlled(bool v) { }

        public new void SetAttacker(Character attacker)
        {
            setAttackerCalled = true;
            lastAttacker = attacker;
        }
        public bool GetAttackingPrimary()
        {
            return attackingPrimary;
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
    /// Mock Ogre class 
    /// </summary>
    public class MockOgre : Ogre
    {
        public void Start()
        {

        }
        public void Update()
        {

        }
        public void FixedUpdate()
        {

        }

        public new IEnumerator BatApproach()
        {
            yield return null;
        }

    }
    /// <summary>
    /// Initializes Ogre and a mock player before each test.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        ogreGameObject = new GameObject("TestOgre");
        testOgre = ogreGameObject.AddComponent<MockOgre>();
        testOgre.health = ogreGameObject.AddComponent<HealthController>();
        batHitboxPrefab = new GameObject("BatHitbox");
        batHitboxPrefab.AddComponent<DefaultHitbox>();
        batPivot = new GameObject("BatPivot");
        batPivot.AddComponent<DefaultHitbox>();

        FieldInfo hitboxField = typeof(Ogre).GetField("batHitboxPrefab", BindingFlags.Instance | BindingFlags.NonPublic);
        hitboxField.SetValue(testOgre, batHitboxPrefab);

        FieldInfo pivotField = typeof(Ogre).GetField("batPivot", BindingFlags.Instance | BindingFlags.NonPublic);
        pivotField.SetValue(testOgre, batPivot);

        attackIndicatorPrefab = new GameObject("AttackIndicator");
        testOgre.counterIndicatorVFXPrefab = attackIndicatorPrefab;

        // Create mock player
        mockPlayer = new GameObject("MockPlayer");
        mockPlayer.AddComponent<CharacterController>();
        Character character = mockPlayer.AddComponent<MockCharacter>();

        FieldInfo playerControllerField = typeof(Ogre).GetField("playerController", BindingFlags.Instance | BindingFlags.NonPublic);
        // Mock PlayerController singleton
        MockPlayerController mockPlayerController = mockPlayer.AddComponent<MockPlayerController>();
        mockPlayerController.currentCharacter = character;
        playerControllerField.SetValue(testOgre, mockPlayerController);
        
        PropertyInfo instanceProperty3 = typeof(PlayerController).GetProperty("instance", BindingFlags.Static | BindingFlags.Public);
        instanceProperty3.SetValue(null, mockPlayerController);


        PlayerController.instance.currentCharacter = character;
    }

    /// <summary>
    /// Destroys test GameObjects after each test.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(ogreGameObject);
        Object.DestroyImmediate(playerGameObject);
        Object.DestroyImmediate(mockPlayer);
        Object.DestroyImmediate(batHitboxPrefab);
        Object.DestroyImmediate(batPivot);
        Object.DestroyImmediate(attackIndicatorPrefab);
    }

    /// <summary>
    /// Verifies that the ogre initializes its health in Start().
    /// </summary>
    [Test]
    public void Start_InitializesHealth()
    {
        testOgre.SendMessage("Start");
        Assert.AreEqual(testOgre.health.GetHealth(), testOgre.health.GetMaxHealth());
    }

    /// <summary>
    /// When PrimaryAttack is called it should start BatWindup coroutine and assign it to attackStateCoroutine.
    /// </summary>
    [UnityTest]
    public IEnumerator PrimaryAttack_StartsBatWindup()
    {
        testOgre.PrimaryAttack();
        yield return new WaitForSeconds(0.1f);
        // Get attackStateCoroutine
        FieldInfo attackStateCoroutineField = typeof(Ogre).GetField("attackStateCoroutine", BindingFlags.Instance | BindingFlags.NonPublic);
        Coroutine attackStateCoroutine = (Coroutine)attackStateCoroutineField.GetValue(testOgre);
        Assert.IsNotNull(attackStateCoroutine);
    }


    /// <summary>
    /// When SecondaryAttack is triggered ogre should start scream windup.
    /// </summary>
    [UnityTest]
    public IEnumerator SecondaryAttack_StartsScreamWindup()
    {
        testOgre.SecondaryAttack();
        yield return new WaitForSeconds(0.1f);
        FieldInfo attackingSecondaryField = typeof(Ogre).GetField("attackingSecondary", BindingFlags.Instance | BindingFlags.NonPublic);
        bool attackingSecondary = (bool)attackingSecondaryField.GetValue(testOgre);
        Assert.IsTrue(attackingSecondary);
    }

    /// <summary>
    /// ScreamWindup should set attack state and call HandleScream after some delay.
    /// </summary>
    [UnityTest]
    public IEnumerator ScreamWindup_TransitionsToHandleScream()
    {
        yield return testOgre.StartCoroutine(testOgre.ScreamWindup());
        Character.AttackState attackState = testOgre.attackState;
        Assert.AreEqual(Character.AttackState.Windup, attackState);
    }



    /// <summary>
    /// CheckPrimaryUsable should return false if attacking or stunned 
    /// </summary>
    [Test]
    public void CheckPrimaryUsable_ReturnsFalseWhenBusy()
    {
        testOgre.SetPrimaryStatus(true);
        Assert.IsFalse(testOgre.CheckPrimaryUsable());
    }

    /// <summary>
    /// ValidatePoint should return false if no current path exists.
    /// </summary>
    [Test]
    public void ValidatePoint_ReturnsFalseWithoutPath()
    {
        FieldInfo currentPathField = typeof(Ogre).GetField("currentPath", BindingFlags.Instance | BindingFlags.NonPublic);
        currentPathField.SetValue(testOgre, null);
        bool result = testOgre.ValidatePoint();
        Assert.IsFalse(result);
    }

}
