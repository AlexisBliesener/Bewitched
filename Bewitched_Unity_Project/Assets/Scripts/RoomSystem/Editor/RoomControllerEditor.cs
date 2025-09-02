#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;


/// <summary>
/// Custom editor for RoomController to have the ability to edit bounds in scene view
/// https://docs.unity3d.com/ScriptReference/Handles.Slider.html
/// </summary>
/// 
[CustomEditor(typeof(RoomController))]
public class RoomControllerEditor : Editor
{
    [Tooltip("The refrence of the room controller")]
    private RoomController roomController;
    [Tooltip("Show the room bounds in the scene view")]
    private bool showRoomBounds = true;
    [Tooltip("Show the entry trigger bounds in the scene view")]
    private bool showEntryTrigger = true;
    [Tooltip("Show unity transform gizmo (for the gameobject)")]
    private bool showUnityGizmo = false;
    [Tooltip("The last tool used in the editor")]
    private Tool lastTool = Tool.None;

    private void OnEnable()
    {
        roomController = (RoomController)target;
        lastTool = Tools.current;
        Tools.current = Tool.None;
    }
    /// <summary>
    /// Draws the inspector for the RoomController component.
    /// </summary>
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Settings for Editor: ", EditorStyles.boldLabel);

        showRoomBounds = EditorGUILayout.Toggle("Show Room Bounds Arrows", showRoomBounds);
        showEntryTrigger = EditorGUILayout.Toggle("Show Entry Trigger Arrows", showEntryTrigger);
        showUnityGizmo = EditorGUILayout.Toggle("Show Unity Arrow for GameObject", showUnityGizmo);


        EditorGUILayout.Space();


        EditorGUILayout.HelpBox($"Remember to make sure that enemy has a tag called: {roomController.GetEnemyTag()} !!", MessageType.Info);

        if (GUI.changed)
        {
            SceneView.RepaintAll();
        }
    }

    private void OnSceneGUI()
    {
        if (roomController == null) return;

        // Get current bounds 
        Bounds roomBounds = roomController.GetRoomBounds();
        Bounds entryBounds = roomController.GetEntryTriggerBounds();

        EditorGUI.BeginChangeCheck();

        // Draw room bounds arrow handles
        if (showRoomBounds)
        {
            // For the arrow handles 
            Handles.color = Color.green;
            Bounds newRoomBounds = DrawBoundsHandle(roomBounds, "Room Bounds " + roomController.gameObject.name);
            // To use the power of ctrl+z :) 
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(roomController, "Changed Room Bounds");
                roomController.SetRoomBounds(newRoomBounds);
                EditorUtility.SetDirty(roomController);
            }
        }

        EditorGUI.BeginChangeCheck();

        // Draw entry trigger bounds arrow handles
        if (showEntryTrigger)
        {
            // For the arrow handles 
            Handles.color = Color.cyan;
            Bounds newEntryBounds = DrawBoundsHandle(entryBounds, "Entry Trigger " + roomController.gameObject.name);
            // To use the power of ctrl+z :) 
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(roomController, "Changed Entry Trigger Bounds");
                roomController.SetEntryTriggerBounds(newEntryBounds);
                EditorUtility.SetDirty(roomController);
            }
        }

        if (showUnityGizmo)
        {
            Tools.current = lastTool;
        }
        else
        {
            Tools.current = Tool.None;
        }
    }

    /// <summary>
    /// Draw the arrow handles to change the size of the bounds
    /// </summary>
    private Bounds DrawBoundsHandle(Bounds bounds, string label)
    {
        Transform transform = roomController.transform;
        Vector3 worldCenter = transform.TransformPoint(bounds.center);
        Vector3 worldSize = Vector3.Scale(bounds.size, transform.lossyScale);

        // Draw the name of the bounds + the object name
        Handles.Label(worldCenter + Vector3.up * (worldSize.y * 0.5f + 1f), label);

        EditorGUI.BeginChangeCheck();
        Vector3 newWorldCenter = Handles.PositionHandle(worldCenter, transform.rotation);
        if (EditorGUI.EndChangeCheck())
        {
            bounds.center = transform.InverseTransformPoint(newWorldCenter);
        }

        // Size awrrows on each face
        Vector3 right = transform.rotation * Vector3.right;
        Vector3 up = transform.rotation * Vector3.up;
        Vector3 forward = transform.rotation * Vector3.forward;

        // x axis arrows ( left/right)
        EditorGUI.BeginChangeCheck();
        Vector3 rightArrow = Handles.Slider(worldCenter + right * worldSize.x * 0.5f, right);
        Vector3 leftArrow = Handles.Slider(worldCenter - right * worldSize.x * 0.5f, -right);
        if (EditorGUI.EndChangeCheck())
        {
            // This will be triggered when the size of the bounds changes
            float newSizeX = Vector3.Distance(rightArrow, leftArrow);
            bounds.size = new Vector3(newSizeX / transform.lossyScale.x, bounds.size.y, bounds.size.z);
            bounds.center = transform.InverseTransformPoint((rightArrow + leftArrow) * 0.5f);
        }

        // y axis arrows (up/down)
        EditorGUI.BeginChangeCheck();
        Vector3 upArrow = Handles.Slider(worldCenter + up * worldSize.y * 0.5f, up);
        Vector3 downArrow = Handles.Slider(worldCenter - up * worldSize.y * 0.5f, -up);
        if (EditorGUI.EndChangeCheck())
        {
            // This will be triggered when the size of the bounds changes
            float newSizeY = Vector3.Distance(upArrow, downArrow);
            bounds.size = new Vector3(bounds.size.x, newSizeY / transform.lossyScale.y, bounds.size.z);
            bounds.center = transform.InverseTransformPoint((upArrow + downArrow) * 0.5f);
        }

        // z axis arrows (forward/back)
        EditorGUI.BeginChangeCheck();
        Vector3 forwardArrow = Handles.Slider(worldCenter + forward * worldSize.z * 0.5f, forward);
        Vector3 backArrow = Handles.Slider(worldCenter - forward * worldSize.z * 0.5f, -forward);
        if (EditorGUI.EndChangeCheck())
        {
            float newSizeZ = Vector3.Distance(forwardArrow, backArrow);
            bounds.size = new Vector3(bounds.size.x, bounds.size.y, newSizeZ / transform.lossyScale.z);
            bounds.center = transform.InverseTransformPoint((forwardArrow + backArrow) * 0.5f);
        }


        return bounds;
    }

    private void OnDisable()
    {
        Tools.current = lastTool;
    }
}
#endif