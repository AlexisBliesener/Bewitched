using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// A room controller system it locks the door automatically when the player enters the room.
/// Enemies will remain despawned/inactive until the room is entered.
/// If the last enemy is defeated, the doors unlock automatically.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class RoomController : MonoBehaviour
{
    [Header("Room Configuration")]
    [Tooltip("The bounds of the room; this is used for enemy detection and gizmo display")]
    [SerializeField] private Bounds roomBounds = new Bounds(Vector3.zero, new Vector3(10f, 10f, 10f));

    [Tooltip("The bounds of when player enter the area the room will activate and the enemy will be spawned")]
    [SerializeField] private Bounds entryTriggerBounds = new Bounds(Vector3.zero, new Vector3(8f, 8f, 8f));

    [Tooltip("Layer mask for detecting enemies in the room bounds")]
    [SerializeField] private LayerMask enemyLayerMask = 1;

    [Tooltip("The tag to identify enemy objects [it has to be the same tag as the enemy prefab]")]
    [SerializeField] private string enemyTag = "Enemy";

    [Tooltip("How often to check for enemy status updates (in seconds) this will update when the room is active and it used to unlock doors if all enemies are defeated")]
    [SerializeField] private float enemyCheckInterval = 0.5f;

    [Tooltip("The color of the room bounds gizmo")]
    [SerializeField] private Color roomBoundsGizmoColor = Color.green;

    [Tooltip("The color of the entry trigger bounds gizmo")]
    [SerializeField] private Color entryTriggerGizmoColor = Color.cyan;

    [Tooltip("Doors that will be locked/unlocked when the room is activ; it should have a IDoor component!")]
    // Unity inspector will not show the custom IDoor object in the inspector so we need to use a list of gameobjects and then cast them to IDoor in the awake function
    [SerializeField] private List<GameObject> doorsObjects = new List<GameObject>();
    [Tooltip("The list of the doors found in the room, this is going to be used in the awake function to get IDoor components")]
    private List<IDoor> doors = new List<IDoor>();
    [Tooltip("The list of the enmies found in the bounds, you can add enemies to this list in the inspector")]
    [SerializeField] public List<GameObject> roomEnemies = new List<GameObject>();
    [Tooltip("The current state of the room")]
    private RoomState currentState = RoomState.Inactive;
    [Tooltip("The state of the door (lock/unlock)")]
    private DoorState doorState = DoorState.Unlocked;
    [SerializeField,Tooltip("Is this an event room? If true, the room will not be cleared when the enemy is defeated")]
    private bool isEventRoom = false;
    // Enum for door state, this is used to prevent multiple lock/unlock calls
    private enum DoorState
    {
        Unlocked,
        Locked
    }
    [Tooltip("Time of the last enemy status check")]
    private float lastEnemyCheckTime = 0f;

    [Tooltip("This is will be true when the last enemy is killed when leaving the room")]
    private bool lastEnemyKilled = false;
    /// <summary>
    /// Get the current state of the room
    /// </summary>
    public RoomState GetCurrentState() => currentState;
    /// <summary>
    /// Get the number of active enemies in the room
    /// </summary>
    public int GetActiveEnemyCount() => roomEnemies.Count(enemy => enemy != null && enemy.activeInHierarchy);

    /// <summary>
    /// Gets the room bounds 
    /// </summary>
    public Bounds GetRoomBounds() => roomBounds;

    /// <summary>
    /// Gets the entry trigger bounds 
    /// </summary>
    public Bounds GetEntryTriggerBounds() => entryTriggerBounds;

    /// <summary>
    /// Sets the room bounds
    /// </summary>
    public void SetRoomBounds(Bounds bounds) => roomBounds = bounds;
    /// <summary>
    /// Sets the entry trigger bounds
    /// </summary>
    public void SetEntryTriggerBounds(Bounds bounds) => entryTriggerBounds = bounds;
    /// <summary>
    /// Gets the enemy tag
    /// </summary>
    public string GetEnemyTag() => enemyTag;
    /// <summary>
    /// Get all the enemy count 
    /// </summary>
    public int GetEnemyCount() => roomEnemies.Count;

    /// <summary> 
    /// Sets the enemy tag 
    /// </summary>
    public void SetLayerMask(LayerMask mask) => enemyLayerMask = mask;

    /// <summary>
    /// This will be triggered when the player enter the room
    /// </summary>
    public event Action<RoomController> OnPlayerEntered;

    /// <summary>
    /// This will be triggered when all enemies are defeated and room is cleared
    /// </summary>
    public event Action<RoomController> OnRoomCleared;

    /// <summary>
    /// This will be triggered when the room state changes
    /// </summary>
    public event Action<RoomController, RoomState> OnStateChanged;



    private void Awake()
    {
        InitializeDoors();
        DetectEnemiesInBounds();

        // Gets the entry trigger bounds and sets it to the box collider 
        // so it matches the bounds of the entry trigger that was set in the editor
        BoxCollider trigger = GetComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = entryTriggerBounds.center;
        trigger.size = entryTriggerBounds.size;
    }

    private void Start()
    {
        // Disable all enemies in the room 
        DeactivateEnemies();
    }

    private void Update()
    {
        // Only check enemy status when room is active to not waste performance :) 
        // and only for specific time interval
        if (currentState == RoomState.Active && Time.time - lastEnemyCheckTime >= enemyCheckInterval)
        {
            CheckEnemyStatus();
            lastEnemyCheckTime = Time.time;
        }
        // We will check if we already killed the last enemy, if not we will check if the room is still active (The last enemy is still make the room acitve ), 
        // if so we will check if the last enemy is the player (possessed), and last we will check if the player is out of the current room 
        if ( !isEventRoom && !lastEnemyKilled && currentState == RoomState.Active && roomEnemies.Count == 1 && roomEnemies[0] == PlayerController.instance.currentCharacter.gameObject)
        {
            if (IsPlayerOutOfRoom())
            {
                KillEnemyOnLeave();
                lastEnemyKilled = true;
            }
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (currentState != RoomState.Inactive) return;

        // Check if this is the player
        if (other.gameObject == PlayerController.instance.currentCharacter.gameObject)
        {
            EnterRoom();
        }
    }
    /// <summary>
    /// Kill the last enemy on leave, and set the player to the current position of the enmey 
    /// </summary>
    private void KillEnemyOnLeave()
    {
        if (PlayerController.instance.GetHag().gameObject != PlayerController.instance.currentCharacter.gameObject)
        {
            PlayerController.instance.GetHag().gameObject.transform.position = PlayerController.instance.currentCharacter.gameObject.transform.position;
            PlayerController.instance.currentCharacter.health.SetCurrentHealth(0); // RIP
        }
        // just to be safe, we will kill all enemies that are for some reason still alive in the room... 
        foreach (GameObject enemyGameObject in roomEnemies)
        {
            if (enemyGameObject.TryGetComponent(out Enemy enemy))
            {
                enemy.health.SetCurrentHealth(0); 
            }
            Destroy(enemyGameObject);
        }

    }

    /// <summary>
    /// Activating enemies and locking doors
    /// </summary>
    public void EnterRoom()
    {
        if (currentState != RoomState.Inactive) return;

        ChangeState(RoomState.Active);

        ActivateEnemies();
        LockDoors();
        //Change to combat music. If it's event room, wait till cutscene is over
        if(roomEnemies.Count!=0&& !isEventRoom) AudioManager.ChangeMusicParameter("InCombat", "True");
        OnPlayerEntered?.Invoke(this);
    }


    /// <summary>
    /// Unlocking doors and changing state to cleared
    /// it's only allowed if the room was active
    /// </summary>
    public void ClearRoom()
    {
        if (currentState != RoomState.Active) return;

        ChangeState(RoomState.Cleared);
        UnlockDoors();
        //change to out of combat music
        AudioManager.ChangeMusicParameter("InCombat", "False");
        OnRoomCleared?.Invoke(this);
    }

    /// <summary>
    /// Detect all enemies within the room bounds 
    /// THis is only called once at the start of the room and add every enemy to the list 
    /// </summary>
    private void DetectEnemiesInBounds()
    {

        if (isEventRoom) return;
        Collider[] colliders = Physics.OverlapBox(transform.position + roomBounds.center, roomBounds.size * 0.5f, transform.rotation, enemyLayerMask);

        foreach (Collider collider in colliders)
        {
            if (collider.CompareTag(enemyTag) && !roomEnemies.Contains(collider.gameObject))
            {
                roomEnemies.Add(collider.gameObject);
            }
        }
    }

    /// <summary>
    /// Activates all enemies in the room
    /// This is called when the player enters the room
    /// </summary>
    private void ActivateEnemies()
    {
        if (isEventRoom) return;
        foreach (GameObject enemy in roomEnemies)
        {
            if (enemy != null)
            {
                enemy.SetActive(true);
            }
        }
    }

    /// <summary>
    /// Deactivate all enemies in the room 
    /// This is called once in Awake() 
    /// </summary>
    private void DeactivateEnemies()
    {
        if (isEventRoom) return;
        foreach (GameObject enemy in roomEnemies)
        {
            if (enemy != null)
            {
                enemy.SetActive(false);
            }
        }
    }
    /// <summary>
    /// Initializes door components from the assigned door objects
    /// This is called in Awake() once
    /// </summary>
    public void InitializeDoors()
    {
        doors.Clear();
        foreach (GameObject doorObject in doorsObjects)
        {
            if (doorObject == null) continue;

            IDoor door = doorObject.GetComponent<IDoor>();
            if (door != null)
            {
                doors.Add(door);
            }
        }
    }

    /// <summary>
    /// Lock all doors in the room
    /// </summary>
    private void LockDoors()
    {
        if (doorState == DoorState.Locked) return;
        foreach (IDoor door in doors)
        {
            door?.Lock();
        }
        doorState = DoorState.Locked;

        if(CameraController.instance != null)
        {
            CameraController.instance.SetInCombat(true);
        }
        else
        {
            Debug.LogWarning("CameraController instance is not set");
        }
    }

    /// <summary>
    /// Unlock all doors in the room
    /// </summary>
    private void UnlockDoors()
    {
        if (doorState == DoorState.Unlocked) return;
        foreach (IDoor door in doors)
        {
            door?.Unlock();
        }
        doorState = DoorState.Unlocked;

        if (CameraController.instance != null)
        {
            CameraController.instance.SetInCombat(false);
        }
        else
        {
            Debug.LogWarning("CameraController instance is not set");
        }
    }

    /// <summary>
    /// Check if all enemies are defeated and call ClearRoom() if needed
    /// </summary>
    private void CheckEnemyStatus()
    {
        // Remove destroyed enemies from the list
        // we will remove any enemy that is null (Died/destoryed)
        roomEnemies.RemoveAll(enemy => enemy == null);

        // If this is an event enemy, we will not clear the room
        if (isEventRoom) return;
        // Check if any enemies are still active
        bool hasActiveEnemies = roomEnemies.Any(enemy => enemy.activeInHierarchy);

        if (!hasActiveEnemies && currentState == RoomState.Active)
        {
            ClearRoom();
            return;
        }
        if (roomEnemies.Count == 1)
        {
            // We will not clear the room yet if there is only one enemy remaining 
            // because when the last enemy is possessed the doors will be unlocked
            // and if the player leaves the last enemy, the dooes will be locked again
            if (roomEnemies[0] == PlayerController.instance.currentCharacter.gameObject)
            {
                UnlockDoors();
            }
            else
            {
                LockDoors();
            }
        }
    }
    /// <summary>
    /// Check if the player is out of the room bounds, it will check only the X and Z axis.
    /// </summary>
    /// <returns>True if the player is out of the room bounds</returns>
    private bool IsPlayerOutOfRoom()
    {
        Vector3 roomBound = transform.position + roomBounds.center;
        Vector3 halfExtent = roomBounds.size * 0.5f;
        Vector3 playerPos = PlayerController.instance.currentCharacter.transform.position;
        return Mathf.Abs(playerPos.x - roomBound.x) > halfExtent.x || Mathf.Abs(playerPos.z - roomBound.z) > halfExtent.z;
    }

    /// <summary>
    /// Changes the room state
    /// OnStateChanged event will be triggered
    /// </summary>
    private void ChangeState(RoomState newState)
    {
        if (currentState == newState) return;

        currentState = newState;

        OnStateChanged?.Invoke(this, currentState);
    }

    /// <summary>
    /// Add an enemy to the room enemies list
    /// </summary>
    public void AddEnemy(GameObject enemy)
    {
        roomEnemies.Add(enemy);
    }
    /// <summary>
    /// Add a gameobject to the doors list
    /// </summary>
    public void AddDoor(GameObject door)
    {
        doorsObjects.Add(door);
    }

    #region Custom Editor Functions

    private void OnDrawGizmos()
    {
        // Draw wireframe cube for entry trigger    
        Gizmos.color = roomBoundsGizmoColor;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(roomBounds.center, roomBounds.size);

        // Filled cube for room bounds
        Color roomFillColor = roomBoundsGizmoColor;
        roomFillColor.a = 0.1f;
        Gizmos.color = roomFillColor;
        Gizmos.DrawCube(roomBounds.center, roomBounds.size);
        // Draw wireframe cube for entry trigger
        Gizmos.color = entryTriggerGizmoColor;
        Gizmos.DrawWireCube(entryTriggerBounds.center, entryTriggerBounds.size);
        // Filled cube for entry trigger
        Color entryFillColor = entryTriggerGizmoColor;
        entryFillColor.a = 0.1f;
        Gizmos.color = entryFillColor;
        Gizmos.DrawCube(entryTriggerBounds.center, entryTriggerBounds.size);

    }
    #endregion
}

