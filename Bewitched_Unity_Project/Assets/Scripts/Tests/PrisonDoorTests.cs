using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Unit tests for the PrisonDoor component.
/// These tests validate door locking/unlocking behavior, including:
/// - Collider enabling/disabling
/// - Door movement direction
/// - Correct timing during animations
/// </summary>
public class PrisonDoorTests
{
    [Tooltip("The root GameObject that holds the PrisonDoor component.")]
    private GameObject doorObj;
    [Tooltip("Reference to the PrisonDoor script under test.")]
    private PrisonDoor prisonDoor;
    [Tooltip("The BoxCollider used to simulate the door's physical lock.")]
    private BoxCollider boxCollider;
    [Tooltip("The visual model of the prison door.")]
    private GameObject doorModel;

    /// <summary>
    /// Sets up a new PrisonDoor instance and assigns a door model and collider
    /// before each test.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        // Create parent GameObject with PrisonDoor script
        doorObj = new GameObject("PrisonDoor");
        doorObj.SetActive(false);
        prisonDoor = doorObj.AddComponent<PrisonDoor>();

        // Create child door model
        doorModel = new GameObject("DoorModel");
        doorModel.transform.parent = doorObj.transform;

        // Create collider object and assign it
        var collider = new GameObject("ColliderObject");
        collider.AddComponent<BoxCollider>();

        // Inject private fields using reflection
        var doorField = typeof(PrisonDoor).GetField("doorModel",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        doorField.SetValue(prisonDoor, doorModel);

        var colliderField = typeof(PrisonDoor).GetField("boxCollider",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        colliderField.SetValue(prisonDoor, collider.GetComponent<BoxCollider>());

        boxCollider = collider.GetComponent<BoxCollider>();

        // Activate object after setup
        doorObj.SetActive(true);
    }

    /// <summary>
    /// Cleans up created objects after each test.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(doorObj);
    }

    /// <summary>
    /// Verifies that calling PrisonDoor.Lock:
    /// - Enables the door's collider
    /// - Moves the door downwards
    /// </summary>
    [UnityTest]
    public IEnumerator Lock_ShouldEnableCollider_AndMoveDoorDown()
    {
        Vector3 initialPos = doorModel.transform.position;

        prisonDoor.Lock();

        // Wait longer than total coroutine time (50 steps * 0.01s = 0.5s)
        yield return new WaitForSeconds(0.6f);

        Assert.IsTrue(boxCollider.enabled, "Collider should be enabled after Lock()");
        Assert.AreNotEqual(initialPos, doorModel.transform.position, "Door position should change after Lock()");
        Assert.Less(doorModel.transform.position.y, initialPos.y, "Door should have moved downwards");
    }

    /// <summary>
    /// Verifies that calling PrisonDoor.Unlock:
    /// - Disables the collider halfway through animation
    /// - Moves the door upwards
    /// </summary>
    [UnityTest]
    public IEnumerator Unlock_ShouldDisableColliderHalfway_AndMoveDoorUp()
    {
        // Start with collider enabled
        boxCollider.enabled = true;
        Vector3 initialPos = doorModel.transform.position;

        prisonDoor.Unlock();

        // Wait halfway through animation
        yield return new WaitForSeconds(0.5f);
        Assert.IsFalse(boxCollider.enabled, "Collider should be disabled halfway through Unlock()");

        // Wait full duration
        yield return new WaitForSeconds(0.5f);
        Assert.AreNotEqual(initialPos, doorModel.transform.position, "Door position should change after Unlock()");
        Assert.Greater(doorModel.transform.position.y, initialPos.y, "Door should have moved upwards");
    }
}
