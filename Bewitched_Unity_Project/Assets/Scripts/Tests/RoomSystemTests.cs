using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Unit tests for the RoomSystem class
/// This will test singleton behavior, room management, event actions, and room controller integration
/// </summary>
public class RoomSystemTests
{
    private GameObject roomSystemObject;
    private RoomSystem roomSystem;
    private GameObject mockRoomController1;
    private GameObject mockRoomController2;
    private RoomController roomController1;
    private RoomController roomController2;

    [SetUp]
    public void Setup()
    {
        // Destroy any existing RoomSystem instance to prevent conflicts
        if (RoomSystem.Instance != null)
        {
            Object.DestroyImmediate(RoomSystem.Instance.gameObject);
        }

        // Create the room system
        roomSystemObject = new GameObject("RoomSystem");
        roomSystem = roomSystemObject.AddComponent<RoomSystem>();

        // Create mock room controllers for testing
        mockRoomController1 = new GameObject("MockRoom1");
        roomController1 = mockRoomController1.AddComponent<RoomController>();

        mockRoomController2 = new GameObject("MockRoom2");
        roomController2 = mockRoomController2.AddComponent<RoomController>();

        // Disable the MonoBehaviour to prevent Update from running during most tests
        roomController1.enabled = false;
        roomController2.enabled = false;
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up all created objects
        if (roomSystemObject != null)
            Object.DestroyImmediate(roomSystemObject);
        if (mockRoomController1 != null)
            Object.DestroyImmediate(mockRoomController1);
        if (mockRoomController2 != null)
            Object.DestroyImmediate(mockRoomController2);
    }

    /// <summary>
    /// Test that RoomSystem creates a singleton instance correctly
    /// </summary>
    [Test]
    public void Instance_CreatesSingletonInstance()
    {

        Assert.IsNotNull(RoomSystem.Instance);
        Assert.AreEqual(roomSystem, RoomSystem.Instance);
    }

    /// <summary>
    /// Test that creating multiple RoomSystem objects destroys duplicate instances
    /// As singleton, this should not be possible
    /// </summary>
    [UnityTest]
    public IEnumerator Instance_DestroysDuplicateInstances()
    {
        GameObject duplicateObject = new GameObject("DuplicateRoomSystem");
        RoomSystem duplicateSystem = duplicateObject.AddComponent<RoomSystem>();

        // Awake should be called
        yield return null;

        Assert.AreEqual(roomSystem, RoomSystem.Instance);
        Assert.IsTrue(duplicateObject == null); // Should be destroyed
    }

    /// <summary>
    /// Test that CreateNewRoom create a new room controller
    /// </summary>
    [Test]
    public void CreateNewRoom_CreatesNewRoomController()
    {

        RoomController newRoom = roomSystem.CreateNewRoom();

        Assert.IsNotNull(newRoom);
        Assert.AreEqual(1, roomSystem.GetRoomCount());
        Assert.AreEqual("Room_01", newRoom.gameObject.name);
    }

    /// <summary>
    /// Test that CreateNewRoom create room as child of RoomSystem
    /// </summary>
    [Test]
    public void CreateNewRoom_CreatesRoomAsChild()
    {
        RoomController newRoom = roomSystem.CreateNewRoom();

        Assert.AreEqual(roomSystem.transform, newRoom.transform.parent);
    }

    /// <summary>
    /// Test that GetRoomCount return correct number of rooms
    /// </summary>
    [Test]
    public void GetRoomCount_ReturnsCorrectCount()
    {
        Assert.AreEqual(0, roomSystem.GetRoomCount());

        roomSystem.CreateNewRoom();
        roomSystem.CreateNewRoom();

        Assert.AreEqual(2, roomSystem.GetRoomCount());
    }

    /// <summary>
    /// Test that RemoveRoom remove room at specified index
    /// </summary>
    [Test]
    public void RemoveRoom_RemovesRoomAtIndex()
    {
        RoomController room1 = roomSystem.CreateNewRoom();

        RoomController room2 = roomSystem.CreateNewRoom();
        Assert.AreEqual(2, roomSystem.GetRoomCount());

        roomSystem.RemoveRoom(0);

        Assert.AreEqual(1, roomSystem.GetRoomCount());
        Assert.AreEqual(room2, roomSystem.GetRoom(0));
    }

    /// <summary>
    /// Test that RemoveRoom handle invalid index correctly
    /// </summary>
    [Test]
    public void RemoveRoom_HandlesInvalidIndex()
    {
        roomSystem.CreateNewRoom();
        int initialCount = roomSystem.GetRoomCount();

        roomSystem.RemoveRoom(-1); // invalid negative index
        roomSystem.RemoveRoom(10); // invalid positive index

        Assert.AreEqual(initialCount, roomSystem.GetRoomCount());
    }

    /// <summary>
    /// Test that GetRoom return correct room by index
    /// </summary>
    [Test]
    public void GetRoom_ReturnsByIndex()
    {
        RoomController room1 = roomSystem.CreateNewRoom();
        RoomController room2 = roomSystem.CreateNewRoom();

        RoomController retrievedRoom1 = roomSystem.GetRoom(0);
        RoomController retrievedRoom2 = roomSystem.GetRoom(1);

        Assert.AreEqual(room1, retrievedRoom1);
        Assert.AreEqual(room2, retrievedRoom2);
    }

    /// <summary>
    /// Test that GetRoom return null for invalid index
    /// </summary>
    [Test]
    public void GetRoom_ReturnsNullForInvalidIndex()
    {
        roomSystem.CreateNewRoom();

        RoomController invalidRoom1 = roomSystem.GetRoom(-1);
        RoomController invalidRoom2 = roomSystem.GetRoom(10);

        Assert.IsNull(invalidRoom1);
        Assert.IsNull(invalidRoom2);
    }

    /// <summary>
    /// Test that GetRoom return correct room by name
    /// </summary>
    [Test]
    public void GetRoom_ReturnsByName()
    {
        RoomController room1 = roomSystem.CreateNewRoom();
        RoomController room2 = roomSystem.CreateNewRoom();
        RoomController retrievedRoom1 = roomSystem.GetRoom("Room_01");
        RoomController retrievedRoom2 = roomSystem.GetRoom("Room_02");
        Assert.AreEqual(room1, retrievedRoom1);

        Assert.AreEqual(room2, retrievedRoom2);
    }

    /// <summary>
    /// Test that OnAnyRoomEntered event is triggered when any room is entered
    /// </summary>
    [Test]
    public void OnAnyRoomEntered_TriggersWhenRoomEntered()
    {
        RoomController newRoom = roomSystem.CreateNewRoom();
        bool eventTriggered = false;
        RoomController eventRoom = null;
        
        RoomSystem.OnAnyRoomEntered += (room) => {eventTriggered = true; eventRoom = room;};

        newRoom.EnterRoom();
        Assert.IsTrue(eventTriggered);
        Assert.AreEqual(newRoom, eventRoom);
    }

    /// <summary>
    /// Test that OnAnyRoomCleared event is triggered when any room is cleared
    /// </summary>
    [Test]
    public void OnAnyRoomCleared_TriggersWhenRoomCleared()
    {
        RoomController newRoom = roomSystem.CreateNewRoom();
        newRoom.EnterRoom(); // Must enter before clearing
        bool eventTriggered = false;
        RoomController eventRoom = null;
        
        RoomSystem.OnAnyRoomCleared += (room) => {eventTriggered = true; eventRoom = room;};
        newRoom.ClearRoom();

        Assert.IsTrue(eventTriggered);
        Assert.AreEqual(newRoom, eventRoom);
    }

    /// <summary>
    /// Test that OnAnyRoomStateChanged event is triggered when any state changes
    /// </summary>
    [Test]
    public void OnAnyRoomStateChanged_TriggersWhenRoomStateChanges()
    {
        RoomController newRoom = roomSystem.CreateNewRoom();
        bool eventTriggered = false;
        RoomController eventRoom = null;
        RoomState eventState = RoomState.Inactive;
        
        RoomSystem.OnAnyRoomStateChanged += (room, state) => {eventTriggered = true; eventRoom = room; eventState = state;};
        newRoom.EnterRoom();

        Assert.IsTrue(eventTriggered);
        Assert.AreEqual(newRoom, eventRoom);
        Assert.AreEqual(RoomState.Active, eventState);
    }

    /// <summary>
    /// Test that event are not triggered after room is removed
    /// </summary>
    [Test]
    public void Events_DoNotTriggerAfterRoomRemoved()
    {
        RoomController room1 = roomSystem.CreateNewRoom();
        bool eventTriggered = false;
        
        RoomSystem.OnAnyRoomEntered += (room) => eventTriggered = true;

        roomSystem.RemoveRoom(0); // Remove the room
        
        // Trying to trigger event on removed room (this should not trigger the global event)
        if (room1 != null) // Room might be destroyed
        {
            room1.EnterRoom();
        }

        Assert.IsFalse(eventTriggered);
    }

    /// <summary>
    /// Test that GetRooms return the correct room list
    /// </summary>
    [Test]
    public void GetRooms_ReturnsCorrectRoomList()
    {
        RoomController room1 = roomSystem.CreateNewRoom();
        RoomController room2 = roomSystem.CreateNewRoom();

        List<RoomData> rooms = roomSystem.GetRooms();
        Assert.AreEqual(2, rooms.Count);
        Assert.AreEqual(room1, rooms[0].roomController);
        Assert.AreEqual(room2, rooms[1].roomController);
        Assert.AreEqual("Room_01", rooms[0].roomName);
        Assert.AreEqual("Room_02", rooms[1].roomName);
    }

    /// <summary>
    /// Test that room system handle empty room list correctly
    /// </summary>
    [Test]
    public void EmptyRoomList_HandledCorrectly()
    {

        Assert.AreEqual(0, roomSystem.GetRoomCount());
        Assert.IsNull(roomSystem.GetRoom(0));
        Assert.IsNull(roomSystem.GetRoom("AnyName"));
        Assert.IsNotNull(roomSystem.GetRooms());
        Assert.AreEqual(0, roomSystem.GetRooms().Count);
    }
}