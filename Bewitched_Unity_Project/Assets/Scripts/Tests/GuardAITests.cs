using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class GuardAITests
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
        protected override void OnDamaged(float amount, HealthController healthController)
        {
            CreateHitStun();
        }
        /// <summary>
        /// Overrides Character.OnDeath() for testing; marks that Die() was called.
        /// </summary>
        protected override void OnDeath(GameObject enemyGameObject)
        {
            Die();
        }

        /// <summary>
        /// Creates a dummy hitstun GameObject to simulate hitstun logic.
        /// </summary>
        public override void CreateHitStun() { hitStunActual = new GameObject("HitStun"); }
    }

    /// <summary>
    /// Testing class for the guard
    /// Provides functionality for checking AI states and player
    /// </summary>
    private class TestGuard : Guard
    {
        public void SetAIState(AIMovementState state)
        {
            aiState = state;
        }

        public AIMovementState GetAIState()
        {
            return aiState;
        }

        public void SetCurrentPlayer(Character player)
        {
            currentPlayer = player;
            target = player;
        }
    }

    [Tooltip("Reference to the GameObject that holds the current player component.")]
    private GameObject testCharacterGameObject;

    [Tooltip("Reference to the current player instance used in tests.")]
    private TestCharacter testCharacter;

    [Tooltip("Reference to the GameObject that holds the TestGuard component.")]
    private GameObject testGuardGameObject;

    [Tooltip("Reference to the TestGuard instance used in tests.")]
    private TestGuard testGuard;


    /// <summary>
    /// Initializes a fresh Guard and player before each test.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        testCharacterGameObject = new GameObject("Character");
        testCharacter = testCharacterGameObject.AddComponent<TestCharacter>();
        testCharacterGameObject.transform.position = new Vector3(10, 0, 10);

        testGuardGameObject = new GameObject("Guard");
        testGuard = testGuardGameObject.AddComponent<TestGuard>();
        testGuardGameObject.transform.position = new Vector3(0, 0, 0);

        testGuard.SetCurrentPlayer(testCharacter);
        testGuard.SetAIState(Enemy.AIMovementState.Patrolling);

        testGuard.sightRange = 5;
    }

    /// <summary>
    /// Cleans up the created GameObject after each test.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        Object.Destroy(testGuardGameObject);
        Object.Destroy(testCharacterGameObject);
    }

    /// <summary>
    /// This is a test for the state change from patrolling to chasing
    /// </summary>
    [Test]
    public void TestPatrolToChase()
    {
        Assert.AreEqual(testGuard.aiState, Enemy.AIMovementState.Patrolling);

        testCharacterGameObject.transform.position = new Vector3(4, 0, 0);
        testGuard.SetBehavior();

        Assert.AreEqual(testGuard.aiState, Enemy.AIMovementState.Chasing);
    }

    // Chasing to surrounding and vice-versa requires a graph, path, etc so I am not doing allat in here unfortunately


}
