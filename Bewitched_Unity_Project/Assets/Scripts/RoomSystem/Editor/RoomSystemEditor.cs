#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// RoomSystem editor to add the ability to add rooms to the room system in the inspector 
/// </summary>
[CustomEditor(typeof(RoomSystem))]
public class RoomSystemEditor : Editor
{   

    [Tooltip("The reference of the room system")]
    private RoomSystem roomSystem;

    private void OnEnable()
    {
        roomSystem = (RoomSystem)target;

        // This is to keep all the data up to date, it gets all the room controllers in the children of the room system and add them to the list
        roomSystem.ClearRooms();
        RoomController[] rooms = roomSystem.gameObject.GetComponentsInChildren<RoomController>(true);
        foreach (RoomController room in rooms)
        {
            roomSystem.Add(new RoomData{roomName = room.gameObject.name,roomController = room});
        }

    }

    public override void OnInspectorGUI()
    {
        GUI.backgroundColor = Color.green;
        // Add new room button
        if (GUILayout.Button("Add New Room", GUILayout.Height(30)))
        {
            RoomController newRoom = roomSystem.CreateNewRoom();
            Selection.activeGameObject = newRoom.gameObject;
            EditorUtility.SetDirty(roomSystem);
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.Space(3);
        // Room list
        var rooms = roomSystem.GetRooms();
        if (rooms.Count == 0)
        {
            EditorGUILayout.HelpBox("No rooms created yet!", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField($"Rooms ({rooms.Count}):", EditorStyles.boldLabel);
        
        for (int i = 0; i < rooms.Count; i++)
        {
            DrawRoomItem(rooms[i], i);
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(roomSystem);
        }
    }
    /// <summary>
    /// Draws a room item in the inspector
    /// Room name, select, focus, delete buttons, and room status in the runtime 
    /// </summary>
    private void DrawRoomItem(RoomData roomData, int index)
    {
        EditorGUILayout.BeginVertical("Box");

        EditorGUILayout.BeginHorizontal();

        // Room name it's editable
        EditorGUI.BeginChangeCheck();
        roomData.roomName = EditorGUILayout.TextField(roomData.roomName);
        if (EditorGUI.EndChangeCheck() && roomData.roomController != null)
        {
            roomData.roomController.gameObject.name = roomData.roomName;
            EditorUtility.SetDirty(roomSystem);
        }

        // Select button
        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button("Select", GUILayout.Width(60)))
        {
            if (roomData.roomController != null)
            {
                Selection.activeGameObject = roomData.roomController.gameObject;
                EditorGUIUtility.PingObject(roomData.roomController.gameObject);
            }
        }

        // enabled the button only in the play mode and the room is active 
        GUI.enabled = Application.isPlaying && roomSystem.GetRooms()[index].roomController.GetCurrentState() == RoomState.Active;
        GUI.backgroundColor = Color.red;
        // Kill button
        if (GUILayout.Button("Kill All Enemies", GUILayout.Width(120)))
        {
            roomSystem.GetRooms()[index].roomController.roomEnemies.ForEach(enemy => {if (enemy != null) enemy.Die();});
            EditorUtility.SetDirty(roomSystem);
            return;
        }
        GUI.enabled = true;

        // Delete button
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Delete", GUILayout.Width(60)))
        {
                roomSystem.RemoveRoom(index);
                EditorUtility.SetDirty(roomSystem);
                return;
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        
        // Show room status if controller exists
        if (roomData.roomController != null)
        {
            EditorGUI.indentLevel++;
            string status = roomData.roomController.GetCurrentState().ToString();
            int enemies = roomData.roomController.GetActiveEnemyCount();
            int enemiesDetected = roomData.roomController.GetEnemyCount();
            EditorGUILayout.LabelField($"Status: {status} | Enemies spawned: {enemies} | Enemies detected: {enemiesDetected}", EditorStyles.miniLabel);
            EditorGUI.indentLevel--;
        }
        else
        {
            EditorGUILayout.LabelField("Missing RoomController!", EditorStyles.miniLabel);
        }
        
        EditorGUILayout.EndVertical();

    }
}
#endif