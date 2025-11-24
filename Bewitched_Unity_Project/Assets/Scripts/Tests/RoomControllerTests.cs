using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Unit tests for the RoomController class
/// This will ttest room state management,enemy detection, door control, and the player interaction (entering and clearing rooms)
/// </summary>
public class RoomControllerTests
{

    // This is a sample door class for testing purposes
    public class TestDoor : MonoBehaviour, IDoor
    {
        public bool IsLocked;
        public void Lock()
        {
            IsLocked = true;
        }
        public void Unlock()
        {
            IsLocked = false;
        }
    }
    /// <summary>
    /// Testing class for the enemy
    /// </summary>
    private class TestEnemy : Enemy
    {

    }
    private GameObject roomControllerObject;
    private RoomController roomController;
    private GameObject mockPlayer;
    private GameObject mockEnemy1;
    private GameObject mockEnemy2;
    private GameObject mockDoor1;
    private GameObject mockDoor2;

    [SetUp]
    public void Setup()
    {
        // Create the room controller system
        roomControllerObject = new GameObject("RoomController");
        roomController = roomControllerObject.AddComponent<RoomController>();

        // Create mock enemies
        mockEnemy1 = new GameObject("Enemy1");
        mockEnemy1.AddComponent<TestEnemy>();
        mockEnemy1.tag = "Enemy";
        mockEnemy1.layer = 0; // Default layer for testing

        mockEnemy2 = new GameObject("Enemy2");
        mockEnemy2.AddComponent<TestEnemy>();
        mockEnemy2.tag = "Enemy";
        mockEnemy2.layer = 0;

        // Create mock doors
        mockDoor1 = new GameObject("Door1");
        mockDoor1.AddComponent<TestDoor>();

        mockDoor2 = new GameObject("Door2");
        mockDoor2.AddComponent<TestDoor>();



        // Disable the MonoBehaviour to prevent Update from running during most tests
        roomController.enabled = false;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(roomControllerObject);
        Object.DestroyImmediate(mockPlayer);
        Object.DestroyImmediate(mockEnemy1);
        Object.DestroyImmediate(mockEnemy2);
        Object.DestroyImmediate(mockDoor1);
        Object.DestroyImmediate(mockDoor2);
    }
    /// <summary>
    /// Test that the room controller returns the initial state (Inactive)
    /// </summary>
    [Test]
    public void GetCurrentState_InitialState()
    {
        RoomState currentState = roomController.GetCurrentState();

        Assert.AreEqual(RoomState.Inactive, currentState);
    }

    /// <summary>
    /// Test that the room controller return zero when there are no enemies
    /// </summary>
    [Test]
    public void GetActiveEnemyCount_NoEnemies()
    {
        int enemyCount = roomController.GetActiveEnemyCount();

        Assert.AreEqual(0, enemyCount);
    }


    /// <summary>
    /// Test that the room controller changes the room bounds
    /// </summary>
    [Test]
    public void SetRoomBounds_ChangToNewBounds()
    {
        Bounds newBounds = new Bounds(new Vector3(1, 2, 3), new Vector3(5, 6, 7));

        roomController.SetRoomBounds(newBounds);
        Bounds result = roomController.GetRoomBounds();

        Assert.AreEqual(newBounds.center, result.center);
        Assert.AreEqual(newBounds.size, result.size);
    }
    /// <summary>
    /// Test that the room controller changes the entry trigger bounds
    /// </summary>
    [Test]
    public void SetEntryTriggerBounds_ChangeToNewBounds()
    {
        Bounds newBounds = new Bounds(new Vector3(2, 3, 4), new Vector3(6, 7, 8));

        roomController.SetEntryTriggerBounds(newBounds);
        Bounds result = roomController.GetEntryTriggerBounds();

        Assert.AreEqual(newBounds.center, result.center);
        Assert.AreEqual(newBounds.size, result.size);
    }
    /// <summary>
    /// Test that the room controller returns the default enemy tag
    /// </summary>
    [Test]
    public void GetEnemyTag_DefaultTag()
    {
        string enemyTag = roomController.GetEnemyTag();
        Assert.AreEqual("Enemy", enemyTag);
    }

    /// <summary>
    /// Tests that the room controller enters the room and changes state to active
    /// </summary>
    [Test]
    public void EnterRoom_ChangeToActive()
    {
        Assert.AreEqual(RoomState.Inactive, roomController.GetCurrentState());

        roomController.EnterRoom();

        Assert.AreEqual(RoomState.Active, roomController.GetCurrentState());
    }
    /// <summary>
    /// Double entering the room should not change the state as it's already active!!
    /// </summary>
    [Test]
    public void EnterRoom_DoesNotChangeStateAlreadyActiveRoom()
    {
        roomController.EnterRoom(); 
        Assert.AreEqual(RoomState.Active, roomController.GetCurrentState());

        roomController.EnterRoom(); // Second entry attempt
        Assert.AreEqual(RoomState.Active, roomController.GetCurrentState());
    }
    /// <summary>
    /// Test that the room controller triggers the player entered event when entering the room
    /// </summary>
    [Test]
    public void EnterRoom_TriggersPlayerEnteredEventInactiveRoom()
    {
        bool eventTriggered = false;
        RoomController eventRoomController = null;
        roomController.OnPlayerEntered += (rc) => { eventTriggered = true;eventRoomController = rc; };

        roomController.EnterRoom();

        Assert.IsTrue(eventTriggered);
        Assert.AreEqual(roomController, eventRoomController);
    }
    /// <summary>
    /// Test that the room controller triggers the state changed event when entering the room
    /// </summary>
    [Test]
    public void EnterRoom_TriggersStateChangedEventInactiveRoom()
    {
        bool eventTriggered = false;
        RoomState eventState = RoomState.Inactive;
        roomController.OnStateChanged += (rc, state) => { eventTriggered = true;eventState = state; };

        roomController.EnterRoom();

        Assert.IsTrue(eventTriggered);
        Assert.AreEqual(RoomState.Active, eventState);
    }

    /// <summary>
    /// Test that the room controller enters the room and changes state to cleared
    /// </summary>
    [Test]
    public void ClearRoom_ChangesStateToClearedActiveRoom()
    {
        roomController.EnterRoom();
        Assert.AreEqual(RoomState.Active, roomController.GetCurrentState());

        roomController.ClearRoom();

        Assert.AreEqual(RoomState.Cleared, roomController.GetCurrentState());
    }

    /// <summary>
    /// Test that the room controller does not change state when clearing an inactive room
    /// </summary>
    [Test]
    public void ClearRoom_DoesNotChangeStateInactiveRoom()
    {
        Assert.AreEqual(RoomState.Inactive, roomController.GetCurrentState());

        roomController.ClearRoom();

        Assert.AreEqual(RoomState.Inactive, roomController.GetCurrentState());
    }

    /// <summary>
    /// Double clearing the room should not change the state as it's already cleared!!
    /// </summary>
    [Test]
    public void ClearRoom_DoesNotChangeStateClearedRoom()
    {
        roomController.EnterRoom();
        roomController.ClearRoom();
        Assert.AreEqual(RoomState.Cleared, roomController.GetCurrentState());

        roomController.ClearRoom(); // Try to clear again

        Assert.AreEqual(RoomState.Cleared, roomController.GetCurrentState());
    }
    /// <summary>
    /// Test that the room controller triggers the room cleared event when clearing the room
    /// </summary>
    [Test]
    public void ClearRoom_TriggersRoomClearedEventActiveRoom()
    {
        roomController.EnterRoom();
        bool eventTriggered = false;
        RoomController eventRoomController = null;
        roomController.OnRoomCleared += (rc) => { eventTriggered = true;eventRoomController = rc; };

        roomController.ClearRoom();

        Assert.IsTrue(eventTriggered);
        Assert.AreEqual(roomController, eventRoomController);
    }
    /// <summary>
    /// Test that the room controller triggers the state changed event when clearing the room
    /// </summary>
    [Test]
    public void ClearRoom_TriggersStateChangedEventActiveRoom()
    {
        roomController.EnterRoom();
        bool eventTriggered = false;
        RoomState eventState = RoomState.Active;
        roomController.OnStateChanged += (rc, state) => { eventTriggered = true; eventState = state; };

        roomController.ClearRoom();

        Assert.IsTrue(eventTriggered);
        Assert.AreEqual(RoomState.Cleared, eventState);
    }
    /// <summary>
    /// Test that the room controller return the correct enemy count when there are active enemies
    /// </summary>
    [UnityTest]
    public IEnumerator GetActiveEnemyCount_ReturnsCorrectCountWithActiveEnemies()
    {
        // add mock enemies manually to the room 
        roomController.AddEnemy(mockEnemy1.GetComponent<Enemy>());
        roomController.AddEnemy(mockEnemy2.GetComponent<Enemy>());

        mockEnemy1.SetActive(true);
        mockEnemy2.SetActive(true);

        yield return null; // Wait for frame

        int activeCount = roomController.GetActiveEnemyCount();

        Assert.AreEqual(2, activeCount);
    }

    /// <summary>
    /// Test that the room controller return zero when there are inactive enemies
    /// </summary>
    [UnityTest]
    public IEnumerator GetActiveEnemyCount_ReturnsZeroWithInactiveEnemies()
    {
        // add mock enemies manually to the room 
        roomController.AddEnemy(mockEnemy1.GetComponent<Enemy>());
        roomController.AddEnemy(mockEnemy2.GetComponent<Enemy>());

        mockEnemy1.SetActive(false);
        mockEnemy2.SetActive(false);

        yield return null; // Wait for frame

        int activeCount = roomController.GetActiveEnemyCount();

        Assert.AreEqual(0, activeCount);
    }
    /// <summary>
    /// Test that the room controller return the correct enemy count when there are mixed enemies (active and inactive)
    /// </summary>
    [UnityTest] 
    public IEnumerator GetActiveEnemyCount_ReturnsCorrectCountWithMixedEnemies()
    {
        // add mock enemies manually to the room 
        roomController.AddEnemy(mockEnemy1.GetComponent<Enemy>());
        roomController.AddEnemy(mockEnemy2.GetComponent<Enemy>());

        mockEnemy1.SetActive(true);
        mockEnemy2.SetActive(false);

        yield return null; // Wait for frame

        int activeCount = roomController.GetActiveEnemyCount();

        Assert.AreEqual(1, activeCount);
    }

    /// <summary>
    /// Test that the room controller call the doors lock method when entering the room
    /// </summary>
    [UnityTest]
    public IEnumerator EnterRoom_DoorsLockWHenEnteringRoom()
    {
        // add mock doors manually to the system 
        roomController.AddDoor(mockDoor1);
        roomController.AddDoor(mockDoor2);

        // To initialize the doors 
        roomController.InitializeDoors();

        yield return null; // Wait for frame

        roomController.EnterRoom(); // Lock doors first

        TestDoor door1Component = mockDoor1.GetComponent<TestDoor>();
        TestDoor door2Component = mockDoor2.GetComponent<TestDoor>();
        Assert.IsTrue(door1Component.IsLocked);
        Assert.IsTrue(door2Component.IsLocked);
    }

    /// <summary>
    /// Test tthat the room controller triggers clear room event only once when multiple calls are made rapidly
    /// </summary>
    [Test]
    public void ClearRoom_MultipleCallsShouldTriggerEventOnlyOnce()
    {
        roomController.EnterRoom();
        int clearEventCount = 0;
        roomController.OnRoomCleared += (rc) => clearEventCount++;

        roomController.ClearRoom();
        roomController.ClearRoom();
        roomController.ClearRoom();

        Assert.AreEqual(RoomState.Cleared, roomController.GetCurrentState());
        Assert.AreEqual(1, clearEventCount); 
    }
}
