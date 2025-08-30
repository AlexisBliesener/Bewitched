using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Contains unit tests for the HealthController class.
/// Validates health functionality such as damage, healing, decay,
/// event notifications, and state management
/// </summary>
public class HealthControllerTests
{
    [Tooltip("Reference to the game object that hold the HealthController component")]
    private GameObject testHealthControllerGameObject;

    [Tooltip("Reference to the HealthController instance that is used in tests")]
    private HealthController testHealthController;

    [Tooltip("Tracks whether OnHealthChanged was called.")]
    private bool healthChangedCalled = false;

    [Tooltip("Tracks whether OnDamaged was called.")]
    private bool damagedCalled = false;

    [Tooltip("Tracks whether OnHealed was called.")]
    private bool healedCalled = false;

    [Tooltip("Tracks whether OnDeath was called.")]
    private bool deathCalled = false;

    [Tooltip("Store the last health values from OnHealthChanged.")]
    private float lastCurrentHealth = 0f;
    private float lastMaxHealth = 0f;

    [Tooltip("Store the last damage/heal amount from events.")]
    private float lastDamageAmount = 0f;
    private float lastHealAmount = 0f;

    /// <summary>
    /// Initializes a fresh HealthController before each test.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        testHealthControllerGameObject = new GameObject("HealthController");
        testHealthController = testHealthControllerGameObject.AddComponent<HealthController>();

        ResetEventFlags();

        testHealthController.OnHealthChanged += OnHealthChangedHandler;
        testHealthController.OnDamaged += OnDamagedHandler;
        testHealthController.OnHealed += OnHealedHandler;
        testHealthController.OnDeath += OnDeathHandler;

        testHealthController.SetMaxHealth(100f);
        ResetEventFlags();
    }

    /// <summary>
    /// Cleans up the created GameObject and unsubscribes from events after each test.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        if (testHealthController != null)
        {
            testHealthController.OnHealthChanged -= OnHealthChangedHandler;
            testHealthController.OnDamaged -= OnDamagedHandler;
            testHealthController.OnHealed -= OnHealedHandler;
            testHealthController.OnDeath -= OnDeathHandler;
        }

        Object.Destroy(testHealthControllerGameObject);
    }


    private void OnHealthChangedHandler(float current, float max)
    {
        healthChangedCalled = true;
        lastCurrentHealth = current;
        lastMaxHealth = max;
    }

    private void OnDamagedHandler(float amount)
    {
        damagedCalled = true;
        lastDamageAmount = amount;
    }

    private void OnHealedHandler(float amount)
    {
        healedCalled = true;
        lastHealAmount = amount;
    }

    private void OnDeathHandler()
    {
        deathCalled = true;
    }

    private void ResetEventFlags()
    {
        healthChangedCalled = false;
        damagedCalled = false;
        healedCalled = false;
        deathCalled = false;
        lastCurrentHealth = 0f;
        lastMaxHealth = 0f;
        lastDamageAmount = 0f;
        lastHealAmount = 0f;
    }

    /// <summary>The current health should start at max health after initialization.</summary>
    [Test]
    public void GetCurrent_ReturnsMaxHealthOnInitialization()
    {
        Assert.AreEqual(100f, testHealthController.GetCurrent());
        Assert.AreEqual(100f, testHealthController.GetMax());
    }

    /// <summary>Character should start alive obviously</summary>
    [Test]
    public void IsDead_ReturnsFalseOnInitialization()
    {
        Assert.IsFalse(testHealthController.IsDead);
    }

    /// <summary>TimeLastHit should start at negative infinity.</summary>
    [Test]
    public void TimeLastHit_StartsAtNegativeInfinity()
    {
        Assert.AreEqual(-Mathf.Infinity, testHealthController.TimeLastHit);
    }

    /// <summary>Initial decay rate should be zero</summary>
    [Test]
    public void GetDecay_ReturnsZeroOnInitialization()
    {
        Assert.AreEqual(0f, testHealthController.GetDecay());
    }

    #region Health Getting/Setting

    /// <summary>GetCurrent should return the current health value.</summary>
    [Test]
    public void GetCurrent_ReturnsCurrentHealthValue()
    {
        Assert.AreEqual(100f, testHealthController.GetCurrent());
    }

    /// <summary>GetMax should return the maximum health value.</summary>
    [Test]
    public void GetMax_ReturnsMaxHealthValue()
    {
        Assert.AreEqual(100f, testHealthController.GetMax());
    }

    /// <summary>SetCurrentHealth should update current health and trigger the events.</summary>
    [Test]
    public void SetCurrentHealth_UpdatesHealthAndTriggersHealthChangedEvent()
    {
        ResetEventFlags();
        
        testHealthController.SetCurrentHealth(50f);
        
        Assert.AreEqual(50f, testHealthController.GetCurrent());
        Assert.IsTrue(healthChangedCalled);
        Assert.AreEqual(50f, lastCurrentHealth);
        Assert.AreEqual(100f, lastMaxHealth);
    }

    /// <summary>SetCurrentHealth should clamp values between 0 and max and shouldn't exceed maximum</summary>
    [Test]
    public void SetCurrentHealth_ClampsValuesBetweenZeroAndMax()
    {
        testHealthController.SetCurrentHealth(150f);
        Assert.AreEqual(100f, testHealthController.GetCurrent());

        testHealthController.SetCurrentHealth(-50f);
        Assert.AreEqual(0f, testHealthController.GetCurrent());
    }

    /// <summary>SetCurrentHealth to zero should trigger death action.</summary>
    [Test]
    public void SetCurrentHealth_TriggersDeathEventWhenSetToZero()
    {
        ResetEventFlags();
        
        testHealthController.SetCurrentHealth(0f);
        
        Assert.IsTrue(testHealthController.IsDead);
        Assert.IsTrue(deathCalled);
    }

    /// <summary>SetToMax should restore full the health.</summary>
    [Test]
    public void SetToMax_RestoresFullHealth()
    {
        testHealthController.SetCurrentHealth(25f);
        ResetEventFlags();
        
        testHealthController.SetToMax();
        
        Assert.AreEqual(100f, testHealthController.GetCurrent());
        Assert.IsTrue(healthChangedCalled);
    }

    /// <summary>SetMaxHealth should update maximum and clamp current health.</summary>
    [Test]
    public void SetMaxHealth_UpdatesMaxAndClampsCurrentHealth()
    {
        testHealthController.SetCurrentHealth(80f);
        ResetEventFlags();
        
        testHealthController.SetMaxHealth(50f);
        
        Assert.AreEqual(50f, testHealthController.GetMax());
        Assert.AreEqual(50f, testHealthController.GetCurrent());
        Assert.IsTrue(healthChangedCalled);
    }

    /// <summary>SetMaxHealth should enforce minimum value of 1.</summary>
    [Test]
    public void SetMaxHealth_EnforcesMinimumValueOfOne()
    {
        testHealthController.SetMaxHealth(0f);
        Assert.AreEqual(1f, testHealthController.GetMax());
        
        testHealthController.SetMaxHealth(-10f);
        Assert.AreEqual(1f, testHealthController.GetMax());
    }

    #endregion

    #region Damage System

    /// <summary>TakeDamage should reduces health and trigger damage event.</summary>
    [Test]
    public void TakeDamage_ReducesHealthAndTriggersDamageEvent()
    {
        ResetEventFlags();
        float initialTime = Time.time;
        
        testHealthController.TakeDamage(30f);
        
        Assert.AreEqual(70f, testHealthController.GetCurrent());
        Assert.IsTrue(damagedCalled);
        Assert.IsTrue(healthChangedCalled);
        Assert.AreEqual(30f, lastDamageAmount);
        Assert.GreaterOrEqual(testHealthController.TimeLastHit, initialTime);
    }

    /// <summary>TakeDamage should not go below zero health.</summary>
    [Test]
    public void TakeDamage_ClampsHealthAtZero()
    {
        testHealthController.TakeDamage(150f);
        
        Assert.AreEqual(0f, testHealthController.GetCurrent());
        Assert.IsTrue(testHealthController.IsDead);
    }

    /// <summary>TakeDamage with fatal damage should trigger death event.</summary>
    [Test]
    public void TakeDamage_TriggersDeathEventWhenFatal()
    {
        ResetEventFlags();
        
        testHealthController.TakeDamage(100f);
        
        Assert.IsTrue(testHealthController.IsDead);
        Assert.IsTrue(deathCalled);
    }

    /// <summary>TakeDamage should ignore zero or negative damage.</summary>
    [Test]
    public void TakeDamage_IgnoresZeroAndNegativeDamage()
    {
        ResetEventFlags();
        
        testHealthController.TakeDamage(0f);
        
        Assert.IsFalse(damagedCalled);
        Assert.IsFalse(healthChangedCalled);
        
        testHealthController.TakeDamage(-10f);
        
        Assert.IsFalse(damagedCalled);
        Assert.IsFalse(healthChangedCalled);
        Assert.AreEqual(100f, testHealthController.GetCurrent());
    }

    /// <summary>TakeDamage should do nothing when already dead.</summary>
    [Test]
    public void TakeDamage_DoesNothingWhenAlreadyDead()
    {
        testHealthController.SetCurrentHealth(0f);
        ResetEventFlags();
        
        testHealthController.TakeDamage(50f);
        
        Assert.IsFalse(damagedCalled);
        Assert.IsFalse(healthChangedCalled);
        Assert.AreEqual(0f, testHealthController.GetCurrent());
    }

    #endregion

    #region DrainLife System

    /// <summary>DrainLife should reduce health without triggering damage events.</summary>
    [Test]
    public void DrainLife_ReducesHealthWithoutDamageEvent()
    {
        ResetEventFlags();
        float initialTimeLastHit = testHealthController.TimeLastHit;
        
        testHealthController.DrainLife(25f);
        
        Assert.AreEqual(75f, testHealthController.GetCurrent());
        Assert.IsFalse(damagedCalled);
        Assert.IsTrue(healthChangedCalled);
        Assert.AreEqual(initialTimeLastHit, testHealthController.TimeLastHit);
    }

    /// <summary>DrainLife with fatal amount should trigger death without damage event.</summary>
    [Test]
    public void DrainLife_TriggersDeathWithoutDamageEventWhenFatal()
    {
        ResetEventFlags();
        
        testHealthController.DrainLife(100f);
        
        Assert.IsTrue(testHealthController.IsDead);
        Assert.IsTrue(deathCalled);
        Assert.IsFalse(damagedCalled);
        Assert.AreEqual(0f, testHealthController.GetCurrent());
    }

    /// <summary>DrainLife should ignore zero or the negative amounts.</summary>
    [Test]
    public void DrainLife_IgnoresZeroAndNegativeAmounts()
    {
        ResetEventFlags();
        
        testHealthController.DrainLife(0f);
        testHealthController.DrainLife(-10f);
        
        Assert.IsFalse(healthChangedCalled);
        Assert.AreEqual(100f, testHealthController.GetCurrent());
    }

    /// <summary>DrainLife should do nothing when the character already dead.</summary>
    [Test]
    public void DrainLife_DoesNothingWhenAlreadyDead()
    {
        testHealthController.SetCurrentHealth(0f);
        ResetEventFlags();
        
        testHealthController.DrainLife(50f);
        
        Assert.IsFalse(healthChangedCalled);
        Assert.AreEqual(0f, testHealthController.GetCurrent());
    }

    #endregion

    #region Healing System

    /// <summary>Heal should increase health and trigger heal event.</summary>
    [Test]
    public void Heal_IncreasesHealthAndTriggersHealEvent()
    {
        testHealthController.SetCurrentHealth(50f);
        ResetEventFlags();
        
        testHealthController.Heal(25f);
        
        Assert.AreEqual(75f, testHealthController.GetCurrent());
        Assert.IsTrue(healedCalled);
        Assert.IsTrue(healthChangedCalled);
        Assert.AreEqual(25f, lastHealAmount);
    }

    /// <summary>Heal should not exceed maximum health.</summary>
    [Test]
    public void Heal_ClampsAtMaxHealth()
    {
        testHealthController.SetCurrentHealth(90f);
        
        testHealthController.Heal(25f);
        
        Assert.AreEqual(100f, testHealthController.GetCurrent());
    }

    /// <summary>Heal should ignore zero or negative amounts.</summary>
    [Test]
    public void Heal_IgnoresZeroAndNegativeAmounts()
    {
        testHealthController.SetCurrentHealth(50f);
        ResetEventFlags();
        
        testHealthController.Heal(0f);
        testHealthController.Heal(-10f);
        
        Assert.IsFalse(healedCalled);
        Assert.IsFalse(healthChangedCalled);
        Assert.AreEqual(50f, testHealthController.GetCurrent());
    }

    /// <summary>Heal should do nothing when already dead.</summary>
    [Test]
    public void Heal_DoesNothingWhenAlreadyDead()
    {
        testHealthController.SetCurrentHealth(0f);
        ResetEventFlags();
        
        testHealthController.Heal(50f);
        
        Assert.IsFalse(healedCalled);
        Assert.IsFalse(healthChangedCalled);
        Assert.AreEqual(0f, testHealthController.GetCurrent());
    }

    #endregion

    #region Decay System

    /// <summary>SetDecay should update decay rate.</summary>
    [Test]
    public void SetDecay_UpdatesDecayRate()
    {
        testHealthController.SetDecay(5f);
        Assert.AreEqual(5f, testHealthController.GetDecay());
    }

    /// <summary>SetDecay should enforce minimum value of zero.</summary>
    [Test]
    public void SetDecay_EnforcesMinimumZero()
    {
        testHealthController.SetDecay(-5f);
        Assert.AreEqual(0f, testHealthController.GetDecay());
    }

    /// <summary>Decay should reduce health over time when enabled.</summary>
    [UnityTest]
    public IEnumerator Update_ReducesHealthOverTimeWithDecay()
    {
        testHealthController.SetDecay(10f);
        testHealthController.EnableUpdateModel(true);
        ResetEventFlags();
        
        yield return new WaitForSeconds(0.5f);
        
        Assert.Less(testHealthController.GetCurrent(), 100f);
        Assert.IsTrue(healthChangedCalled);
    }

    /// <summary>Decay should trigger death when health reaches zero.</summary>
    [UnityTest]
    public IEnumerator Update_TriggersDeathWhenDecayReachesZero()
    {
        testHealthController.SetCurrentHealth(1f);
        testHealthController.SetDecay(10f);
        testHealthController.EnableUpdateModel(true);
        ResetEventFlags();
        
        yield return new WaitForSeconds(0.2f);
        
        Assert.IsTrue(testHealthController.IsDead);
        Assert.IsTrue(deathCalled);
    }

    /// <summary>Decay should not occur when update model is disabled.</summary>
    [UnityTest]
    public IEnumerator Update_DoesNotDecayWhenUpdateModelDisabled()
    {
        testHealthController.SetDecay(10f);
        testHealthController.EnableUpdateModel(false);
        float initialHealth = testHealthController.GetCurrent();
        
        yield return new WaitForSeconds(0.5f);
        
        Assert.AreEqual(initialHealth, testHealthController.GetCurrent());
    }

    /// <summary>Decay should not occur when already dead.</summary>
    [UnityTest]
    public IEnumerator Update_DoesNotDecayWhenAlreadyDead()
    {
        testHealthController.SetCurrentHealth(0f);
        testHealthController.SetDecay(10f);
        testHealthController.EnableUpdateModel(true);
        
        yield return new WaitForSeconds(0.2f);
        
        Assert.AreEqual(0f, testHealthController.GetCurrent());
    }

    #endregion

    #region Update Model Control

    /// <summary>EnableUpdateModel should control whether decay occurs.</summary>
    [Test]
    public void EnableUpdateModel_ControlsDecayBehavior()
    {
        testHealthController.EnableUpdateModel(false);
        testHealthController.SetDecay(10f);
        
        Assert.AreEqual(10f, testHealthController.GetDecay());
    }

    #endregion

    #region Edge Cases and Error Handling

    /// <summary>Multiple damage calls should all be processed correctly.</summary>
    [Test]
    public void TakeDamage_ProcessesMultipleRapidCallsCorrectly()
    {
        int damageEventCount = 0;
        testHealthController.OnDamaged += (amount) => damageEventCount++;
        
        testHealthController.TakeDamage(10f);
        testHealthController.TakeDamage(20f);
        testHealthController.TakeDamage(15f);
        
        Assert.AreEqual(55f, testHealthController.GetCurrent());
        Assert.AreEqual(3, damageEventCount);
    }

    /// <summary>Events should fire in correct order for fatal damage.</summary>
    [Test]
    public void TakeDamage_FiresEventsInCorrectOrderForFatalDamage()
    {
        bool damagedFiredBeforeDeath = false;
        bool healthChangedFiredBeforeDeath = false;
        
        testHealthController.OnDamaged += (amount) => 
        {
            if (!deathCalled) damagedFiredBeforeDeath = true;
        };
        
        testHealthController.OnHealthChanged += (current, max) => 
        {
            if (!deathCalled) healthChangedFiredBeforeDeath = true;
        };
        
        testHealthController.TakeDamage(100f);
        
        Assert.IsTrue(damagedFiredBeforeDeath);
        Assert.IsTrue(healthChangedFiredBeforeDeath);
        Assert.IsTrue(deathCalled);
    }

    #endregion
}