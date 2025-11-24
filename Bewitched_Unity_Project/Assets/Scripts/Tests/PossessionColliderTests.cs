using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A simple concrete stub used for testing because the abstract Character class 
/// </summary>
public class TestCharacter : Character
{
    public override void Die()
    {
    }
}

/// <summary>
/// Unit tests for the PossessionCollider component,
/// verifying that it properly tracks which characters enter and exit.
/// </summary>
public class PossessionColliderTests
{
    [Tooltip("GameObject that holds the PossessionCollider component under test.")]
    private GameObject possessionColliderGO;
    [Tooltip("The PossessionCollider instance being tested.")]
    private PossessionCollider possessionCollider;
    [Tooltip("Stub GameObject representing the player, created for completeness in tests.")]
    private GameObject playerGO;
    [Tooltip("Stub GameObject representing the enemy character in possession tests.")]
    private GameObject enemyGO;
    [Tooltip("Concrete TestCharacter component attached to the enemy GameObject.")]
    private TestCharacter enemyCharacter;

    /// <summary>
    /// Sets up a fresh test environment before each test,
    /// including an enemy, a player stub, and a PossessionCollider instance.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        // Enemy setup
        enemyGO = new GameObject("Enemy");
        enemyCharacter = enemyGO.AddComponent<TestCharacter>();

        // Player setup (not directly used in tests but created for completeness)
        playerGO = new GameObject("Player");
        playerGO.AddComponent<TestCharacter>();

        // PossessionCollider setup
        possessionColliderGO = new GameObject("PossessionCollider");
        possessionColliderGO.AddComponent<BoxCollider>();
        possessionCollider = possessionColliderGO.AddComponent<PossessionCollider>();
    }

    /// <summary>
    /// Cleans up all created GameObjects after each test
    /// to ensure a clean Unity scene state.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(possessionColliderGO);
        Object.DestroyImmediate(playerGO);
        Object.DestroyImmediate(enemyGO);
    }

    /// <summary>
    /// Verifies that when an enemy enters the collider,
    /// it is added to the possession list.
    /// </summary>
    [Test]
    public void OnTriggerEnter_AddsEnemyCharacter()
    {
        var collider = enemyGO.AddComponent<BoxCollider>();
        possessionCollider.SendMessage("OnTriggerEnter", collider);

        List<Character> characters = possessionCollider.GetCharactersInPossession();

        Assert.Contains(enemyCharacter, characters);
    }

    /// <summary>
    /// Verifies that when an enemy exits the collider,
    /// it is removed from the possession list.
    /// </summary>
    [Test]
    public void OnTriggerExit_RemovesEnemyCharacter()
    {
        var collider = enemyGO.AddComponent<BoxCollider>();
        possessionCollider.SendMessage("OnTriggerEnter", collider);
        possessionCollider.SendMessage("OnTriggerExit", collider);

        List<Character> characters = possessionCollider.GetCharactersInPossession();

        Assert.IsEmpty(characters);
    }

    /// <summary>
    /// Verifies that <see cref="PossessionCollider.GetCharactersInPossession"/>
    /// cleans up null references if characters are destroyed after entering.
    /// </summary>
    [Test]
    public void GetCharactersInPossession_RemovesNullReferences()
    {
        var collider = enemyGO.AddComponent<BoxCollider>();
        possessionCollider.SendMessage("OnTriggerEnter", collider);

        // Destroy enemy and leave null reference behind
        Object.DestroyImmediate(enemyGO);

        List<Character> characters = possessionCollider.GetCharactersInPossession();

        Assert.IsEmpty(characters);
    }
}
