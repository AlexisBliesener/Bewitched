using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class AttackStatusEffectsTest
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
        /// Sets the character's current health to a specified value.
        /// </summary>
        /// <param name="healthAmt">The new health value to assign.</param>
        public void SetCurrentHealth(float healthAmt)
        {
            health.SetCurrentHealth(healthAmt);
        }

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
    }

    /// <summary>
    /// A testing class so values can be changed
    /// </summary>
    private class TestingStatusEffects : AttackStatusEffects
    {
    }

    TestingStatusEffects effects;
    TestCharacter user;
    TestCharacter enemy;

    /// <summary>
    /// Sets up the scene for testing
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        user = new GameObject("user").AddComponent<TestCharacter>();
        enemy = new GameObject("enemy").AddComponent<TestCharacter>();
        effects = new GameObject("effects").AddComponent<TestingStatusEffects>();
    }

    /// <summary>
    /// Cleans up the created GameObject after each test.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        Object.Destroy(effects.gameObject);
        Time.timeScale = 1;
    }

    /// <summary>
    /// Tests to ensure the object saves to json and loads the same
    /// </summary>
    [Test]
    public void SaveAndLoad_ConfirmTheSame()
    {
        string json = effects.SaveToJson();
        Debug.Log(json);

        TestingStatusEffects dupe = new GameObject("dupe").AddComponent<TestingStatusEffects>();
        dupe.LoadFromJson(json);

        Assert.AreEqual(effects.GetKnockbackRange(), dupe.GetKnockbackRange());
        Assert.AreEqual(effects.GetKnockbackType(), dupe.GetKnockbackType());
    }

    /// <summary>
    /// Tests to ensure the object saves to json and changes are applied when loaded
    /// </summary>
    [Test]
    public void SaveAndLoad_MakeChanges()
    {
        string json = effects.SaveToJson();
        Debug.Log(json);

        json = json.Replace('0', '1');

        TestingStatusEffects dupe = new GameObject("dupe").AddComponent<TestingStatusEffects>();
        dupe.LoadFromJson(json);

        Assert.AreNotEqual(effects.GetKnockbackRange(), dupe.GetKnockbackRange());
        Assert.AreNotEqual(effects.GetKnockbackType(), dupe.GetKnockbackType());
    }

    /// <summary>
    /// Tests to see if knockback is applied to enemies
    /// </summary>
    [Test]
    public void TestKnockback_OnEnemy()
    {
        enemy.gameObject.AddComponent<KnockbackControl>();
        enemy.gameObject.AddComponent<CharacterController>();
        DefaultHitbox hitbox = new GameObject("hitbox").AddComponent<DefaultHitbox>();

        effects.SetKnockback(AttackStatusEffects.KnockbackType.BasicForward, 10);

        effects.ApplyKnockback(user, enemy, hitbox);

        Assert.IsTrue(enemy.GetComponent<KnockbackControl>().gettingKnockback);
        Object.DestroyImmediate(hitbox);
    }

    [UnityTest]
    public IEnumerator TestTimeStop_Basic()
    {
        DefaultHitbox hitbox = new GameObject("hitbox").AddComponent<DefaultHitbox>();

        hitbox.Init(user, attackDuration: 3);

        Assert.AreEqual(1, Time.timeScale);
        effects.SetTimeStop(0.15f);
        effects.ApplyTimeStop(user, enemy, hitbox);

        Assert.AreEqual(0, Time.timeScale);

        yield return new WaitForSecondsRealtime(0.25f);
        Assert.AreEqual(1, Time.timeScale);

        Object.DestroyImmediate(hitbox.gameObject);
    }
}
