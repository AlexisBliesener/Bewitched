using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton as a container system for managing room controllers
/// </summary>
public class RoomSystem : MonoBehaviour
{
    [Header("Room System")]
    [Tooltip("The list of rooms in the system")]
    [SerializeField] private List<RoomData> rooms = new List<RoomData>();

    // Singleton instance
    public static RoomSystem Instance { get; private set; }

    // Events for other classes to subscribe to
    [Tooltip("Triggered when any room is entered")]
    public static event Action<RoomController> OnAnyRoomEntered;
    [Tooltip("Triggered when any room is cleared")]
    public static event Action<RoomController> OnAnyRoomCleared;
    [Tooltip("Triggered when any room's state changes")]
    public static event Action<RoomController, RoomState> OnAnyRoomStateChanged;
    [Tooltip("The current active room")]
    private RoomController currentActiveRoom;

    private void Awake()
    {
        // Only one instance of RoomSystem should be there
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SubscribeToExistingRooms();
    }

    public List<Enemy> GetCurrentRoomEnemies()
    {
        if(GetActiveRoomController() != null)
        {
            return GetActiveRoomController().roomEnemies;
        }
        return null;
        
    }

    /// <summary>
    /// Creates a new room controller and adds it to the system, this is used by the editor
    /// </summary>
    public RoomController CreateNewRoom()
    {
        // Create new game object as child
        GameObject roomObject = new GameObject($"Room_{rooms.Count + 1:00}");
        roomObject.transform.SetParent(transform);
        
        RoomController roomController = roomObject.AddComponent<RoomController>();
        // Pre-configure the room controller
        roomController.SetLayerMask(1 << LayerMask.NameToLayer("Character"));
        roomController.SetRoomBounds(new Bounds(new Vector3(0,3,0),new Vector3(15,2,15)));
        roomController.SetEntryTriggerBounds(new Bounds(new Vector3(0,0,-15),new Vector3(3,2,2)));

        // create the room data and add them to the list
        RoomData newRoomData = new RoomData{roomName = roomObject.name,roomController = roomController};
        rooms.Add(newRoomData);

        // Subscribe to the new room's events
        SubscribeToRoomEvents(roomController);

        return roomController;
    }

    /// <summary>
    /// Remove a room from the system
    /// </summary>
    public void RemoveRoom(int index)
    {
        if (index < 0 || index >= rooms.Count) return;

        if (rooms[index].roomController != null)
        {
            // Unsubscribe from events before destroying
            UnsubscribeFromRoomEvents(rooms[index].roomController);

            if (Application.isPlaying)
            {

                Destroy(rooms[index].roomController.gameObject);
            }
            else
            {
                DestroyImmediate(rooms[index].roomController.gameObject);
            }
        }

        rooms.RemoveAt(index);
    }
    /// <summary>
    /// Add a room to the list
    /// </summary>
    public void Add(RoomData roomData)
    {
        rooms.Add(roomData);
    }

    /// <summary>
    /// Get a specific room by index. Returns null if index is out of bounds
    /// </summary>
    public RoomController GetRoom(int index)
    {
        if (index < 0 || index >= rooms.Count) return null;
        return rooms[index].roomController;
    }

    /// <summary>
    /// Get a room by name returns null if no room is found
    /// </summary>
    public RoomController GetRoom(string roomName)
    {
        foreach (RoomData room in rooms)
        {
            if (room.roomName == roomName)
                return room.roomController;
        }
        return null;
    }
    /// <summary>
    /// Clear all rooms
    /// </summary>
    public void ClearRooms()
    {
        rooms.Clear();
    }
    /// <summary>
    /// Get the total number of rooms
    /// </summary>
    public int GetRoomCount() => rooms.Count;

    /// <summary>
    /// Subscribe to existing room events (called on Awake)
    /// </summary>
    private void SubscribeToExistingRooms()
    {
        foreach (RoomData room in rooms)
        {
            if (room.roomController != null)
            {
                SubscribeToRoomEvents(room.roomController);
            }
        }
    }

    /// <summary>
    /// Subscribe to a room's events
    /// </summary>
    private void SubscribeToRoomEvents(RoomController roomController)
    {
        if (roomController == null) return;

        roomController.OnPlayerEntered += HandleRoomEntered;
        roomController.OnRoomCleared += HandleRoomCleared;
        roomController.OnStateChanged += HandleRoomStateChanged;
    }

    /// <summary>
    /// Unsubscribe from a room's events
    /// </summary>
    private void UnsubscribeFromRoomEvents(RoomController roomController)
    {
        if (roomController == null) return;

        roomController.OnPlayerEntered -= HandleRoomEntered;
        roomController.OnRoomCleared -= HandleRoomCleared;
        roomController.OnStateChanged -= HandleRoomStateChanged;
    }

    /// <summary>
    /// Handle when any room is entered
    /// </summary>
    private void HandleRoomEntered(RoomController room)
    {
        OnAnyRoomEntered?.Invoke(room);
    }

    /// <summary>
    /// Handle when any room is cleared
    /// </summary>
    private void HandleRoomCleared(RoomController room)
    {
        OnAnyRoomCleared?.Invoke(room);
    }

    /// <summary>
    /// Handle when any room's state changes
    /// </summary>
    private void HandleRoomStateChanged(RoomController room, RoomState newState)
    {
        OnAnyRoomStateChanged?.Invoke(room, newState);
        switch (newState)
        {
            case RoomState.Active:
                currentActiveRoom = room;
                break;
            default: // Inactive or Cleared
                currentActiveRoom = null;
                break;
        }
    }

    /// <summary>
    /// Gets the rooms list for editor access
    /// </summary>
    public List<RoomData> GetRooms() => rooms;

    /// <summary>
    /// Gets the currently active room if any are active
    /// </summary>
    /// <returns> Currently active room, null if none </returns>
    public RoomController GetActiveRoomController()
    {
        return currentActiveRoom;
    }

    /// <summary>
    /// Gets the RoomController by its coordinates
    /// </summary>
    /// <param name="coords"> Coordinates to search from </param>
    /// <param name="range"> Range of room bounds </param>
    /// <returns> RoomController with bounds containing coords </returns>
    public RoomController GetRoomFromCoordinates(Vector3 coords, float range)
    {
        foreach (RoomData room in rooms)
        {
            if (room.roomController != null)
            {
                if (Vector3.Distance(room.roomController.GetRoomBounds().ClosestPoint(coords), coords) <= range)
                {
                    return room.roomController;
                }
            }
        }
        return null;
    }
    /// <summary>
    /// Gets the RoomController by its coordinates with tolerances
    /// </summary>
    /// <param name="coords"> Coordinates to search from </param>
    /// <param name="xTolerance">X tolerance</param>
    /// <param name="yTolerance">Y tolerance</param>
    /// <param name="zTolerance">Z tolerance</param>
    /// <returns> RoomController with bounds containing coords </returns>
    public RoomController GetRoomFromCoordinates(Vector3 coords, float xTolerance = 0f, float yTolerance = 0f, float zTolerance = 0f)
    {
        foreach (RoomData room in rooms)
        {
            if (room.roomController != null)
            {
                if (room.roomController.IsObjectInsideRoom(coords, xTolerance, yTolerance, zTolerance))
                {
                    return room.roomController;
                }
            }
        }
        return null;
    }
}