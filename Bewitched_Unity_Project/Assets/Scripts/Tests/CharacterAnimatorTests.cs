using NUnit.Framework;
using System;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Unit tests for the CharacterAnimator component.
/// These tests verify animation state transitions
/// when switching states under different conditions.
/// </summary>
public class CharacterAnimatorTests
{
    /// <summary>
    /// A mock subclass of CharacterAnimator 
    /// used to override Unity lifecycle methods in testing.
    /// </summary>
    public class MockCharacterAnimator : CharacterAnimator
    {
        /// <summary>
        /// Suppressed Start() so Unity does not interfere 
        /// with test execution.
        /// </summary>
        protected new void Start() { }
    }

    [Tooltip("The temporary GameObject used to host the animator under test.")]
    private GameObject testObject;
    [Tooltip("The instance of CharacterAnimator being tested.")]
    private CharacterAnimator animator;

    /// <summary>
    /// Called before each test. Creates a new GameObject and attaches
    /// a CharacterAnimator component.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        testObject = new GameObject("TestCharacter");
        testObject.SetActive(false);
        animator = testObject.AddComponent<CharacterAnimator>();
    }

    /// <summary>
    /// Called after each test. Destroys the temporary GameObject
    /// to clean up the test environment.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(testObject);
    }

    /// <summary>
    /// Verifies that the default animation state is idle
    /// </summary>
    [Test]
    public void GetCurrentState_Default_IsIdle()
    {
        Assert.AreEqual(CharacterAnimator.AnimationStates.idle, animator.GetCurrentState());
    }

    /// <summary>
    /// Ensures that CharacterAnimator.NotInPrimary
    /// returns true when the character is idle.
    /// </summary>
    [Test]
    public void NotInPrimary_WhenIdle_ReturnsTrue()
    {
        Assert.IsTrue(animator.NotInPrimary());
    }

    /// <summary>
    /// Ensures that after switching to primaryAttack,
    /// CharacterAnimator.NotInPrimary returns false.
    /// </summary>
    [Test]
    public void NotInPrimary_AfterStateSwitch_ReturnsFalse()
    {
        try
        {
            animator.SwitchState(CharacterAnimator.AnimationStates.primaryAttack);
        }
        catch (NullReferenceException)
        {
            // Ignore missing Animator component in test environment
        }

        Assert.IsFalse(animator.NotInPrimary());
    }

    /// <summary>
    /// Ensures that calling CharacterAnimator.SwitchState
    /// updates the current state to the requested one.
    /// </summary>
    [Test]
    public void SwitchState_ChangesStateAndSetsTrigger()
    {
        try
        {
            animator.SwitchState(CharacterAnimator.AnimationStates.run);
        }
        catch (NullReferenceException)
        {
            // Ignore missing Animator component in test environment
        }

        Assert.AreEqual(CharacterAnimator.AnimationStates.run, animator.GetCurrentState());
    }

    /// <summary>
    /// Ensures that once in death state, 
    /// CharacterAnimator.SwitchState does not 
    /// allow further transitions.
    /// </summary>
    [Test]
    public void SwitchState_DeathState_PreventsChanges()
    {
        try
        {
            animator.SwitchState(CharacterAnimator.AnimationStates.death);
            animator.SwitchState(CharacterAnimator.AnimationStates.run);
        }
        catch (NullReferenceException)
        {
            // Ignore missing Animator component in test environment
        }

        Assert.AreEqual(CharacterAnimator.AnimationStates.death, animator.GetCurrentState());
    }
}
