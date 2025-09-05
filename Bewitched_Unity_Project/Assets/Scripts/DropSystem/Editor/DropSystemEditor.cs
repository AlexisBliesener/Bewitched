#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// DropSystem editor to add the ability to manage drops in the inspector easily
/// </summary>
[CustomEditor(typeof(DropSystem))]
public class DropSystemEditor : Editor
{
    [Tooltip("The reference of the drop system")]
    private DropSystem dropSystem;
    [Tooltip("This is used so when we have a lot of drops, it will enable the scroll view")]
    private Vector2 scrollPosition = Vector2.zero;
    
    [Tooltip("This is used to store the list of all available DropItemBase objects in the project so they can be added to the system")]
    private List<DropItemBase> allAvailableDropItems = new List<DropItemBase>();
    [Tooltip("This is the dropdown names array for the popup")]
    private string[] dropItemNames;
    [Tooltip("This is the selected drop index in the dropdown")]
    private int selectedDropIndex = 0;
    // <summary>
    // This is called when the script is loaded in the editor
    // it will save the reference to the drop system and refresh the list of available drops
    // </summary>
    private void OnEnable()
    {
        dropSystem = (DropSystem)target;
        RefreshDropItemsList();
    }
    // <summary>
    // This is called when the inspector is drawn
    // it will draw the drop system settings, the add drop section, and the runtime info
    // </summary>
    public override void OnInspectorGUI()
    {
        // Header
        EditorGUILayout.Space(3);

        // Drop Settings (Drop item chance, prefab)
        DrawDropSettings();

        EditorGUILayout.Space(10);

        // Runtime Information 
        // This is only shown when the game is running
        if (Application.isPlaying)
        {
            DrawRuntimeInfo();
            EditorGUILayout.Space(10);
        }
        // A reminder text for our designer :) 
        EditorGUILayout.HelpBox("To create a new drop go to Tools > Drop System > Create New Drop Item", MessageType.Info);
        EditorGUILayout.HelpBox("To create a new rarity go to Tools > Drop System > Create New Rarity Item", MessageType.Info);
        EditorGUILayout.Space(5);

        // This will draw the add drop section with a dropdown for selecting the drop to add 
        // and a button to add all available drops
        // AND A RED BUTTON TO CLEAR ALL DROPS!!
        DrawAddDropSection();


        EditorGUILayout.Space(5);

        // All list on drops that available in the system
        DrawDropList();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(dropSystem);
        }
    }

    /// <summary>
    /// Refreshes the list of all available DropItemBase objects in the project
    /// It finds all DropItemBase objects in the project and also searche in scene for DropItemBase components
    /// </summary>
    private void RefreshDropItemsList()
    {
        allAvailableDropItems.Clear();
        
        string prefabFolderPath = "Assets/Prefabs/Drops";
        
        // Find all prefabs in that folder
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new string[] { prefabFolderPath });
        
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            
            if (prefab != null)
            {
                // Check if this prefab has any component that inherits from DropItemBase
                DropItemBase dropComponent = prefab.GetComponent<DropItemBase>();
                if (dropComponent != null)
                {
                    allAvailableDropItems.Add(dropComponent);
                }
            }
        }
        
        // And search in scene for DropItemBase components
        DropItemBase[] sceneDrops = FindObjectsOfType<DropItemBase>();
        foreach (DropItemBase drop in sceneDrops)
        {
            if (!allAvailableDropItems.Contains(drop))
            {
                allAvailableDropItems.Add(drop);
            }
        }
        
        // Create dropdown names array
        UpdateDropdownNames();
    }

    /// <summary>
    /// Update the dropdown names array for the popup
    /// This is just a helper function to create the dropdown names array
    /// </summary>
    private void UpdateDropdownNames()
    {
        if (allAvailableDropItems.Count == 0)
        {
            // empty? only show the placeholder
            dropItemNames = new string[] { "No drops found" };
            selectedDropIndex = 0;
            return;
        }
        // why +1 ? because we need to add the placeholder at the start 
        dropItemNames = new string[allAvailableDropItems.Count + 1];
        dropItemNames[0] = "Select an drop to add...";

        for (int i = 0; i < allAvailableDropItems.Count; i++)
        {
            DropItemBase drop = allAvailableDropItems[i];
            string name = drop != null ? drop.GetDropName() : "don't choose me! Internal error!";
            
            if (string.IsNullOrEmpty(name))
                name = drop != null ? drop.name : "Unnamed drop";
            
            // We didn't forget the placeholder at the start so we added i + 1 :) 
            dropItemNames[i + 1] = $"{name}";
        }
        
        selectedDropIndex = 0; // Reset selection
    }

    /// <summary>
    /// Draw the add drop section with dropdown
    /// </summary>
    private void DrawAddDropSection()
    {
        EditorGUILayout.BeginVertical("Box");
        EditorGUILayout.LabelField("Add Drops", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        
        // Refresh button to refresh the list of available drops 
        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button("Refresh", GUILayout.Width(70)))
        {
            RefreshDropItemsList();
        }
        GUI.backgroundColor = Color.white;
        
        // Dropdown for selecting drops
        EditorGUI.BeginChangeCheck();
        selectedDropIndex = EditorGUILayout.Popup(selectedDropIndex, dropItemNames);
        if (EditorGUI.EndChangeCheck())
        {
            // Did they select an drop? call the function to add it! 
            if (selectedDropIndex > 0 && allAvailableDropItems.Count > 0)
            {
                AddSelectedDrop();
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        // Show add all available drops button, and clear all drops button only if there are drops in the project!
        if (allAvailableDropItems.Count > 0)
        {
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button($"Add All Available ({allAvailableDropItems.Count})", GUILayout.Height(25)))
            {
                AddAllDrops();
            }
            
            // Clear all button
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Clear All", GUILayout.Height(25)))
            {
                    dropSystem.availableDrops.Clear();
                    EditorUtility.SetDirty(dropSystem);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }
        
        // Info about found drops
        EditorGUILayout.LabelField($"Found {allAvailableDropItems.Count} drops in the project", EditorStyles.miniLabel);
        
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// Add the currently selected drop to the system
    /// It's a helper function to add the selected drop to the system
    /// </summary>
    private void AddSelectedDrop()
    {
        // skip the placeholder and any invalid index or empty drop
        if (selectedDropIndex <= 0 || selectedDropIndex - 1 >= allAvailableDropItems.Count)
            return;
            
        DropItemBase selectedDrop = allAvailableDropItems[selectedDropIndex - 1];
        
        if (selectedDrop != null)
        {
            // Check if already added
            if (!dropSystem.availableDrops.Contains(selectedDrop))
            {
                dropSystem.availableDrops.Add(selectedDrop);
                EditorUtility.SetDirty(dropSystem);
                Debug.Log($"Added drop: {selectedDrop.GetDropName()}");
            }
            else
            {
                Debug.LogWarning($"Drop '{selectedDrop.GetDropName()}' is already in the list!!");
            }
        }
        
        // Reset selection to the placeholder
        selectedDropIndex = 0;
    }

    /// <summary>
    /// Add all available drops to the system
    /// </summary>
    private void AddAllDrops()
    {
        int addedCount = 0;
        foreach (DropItemBase drop in allAvailableDropItems)
        {
            if (drop != null && !dropSystem.availableDrops.Contains(drop))
            {
                dropSystem.availableDrops.Add(drop);
                addedCount++;
            }
        }
        
        if (addedCount > 0)
        {
            EditorUtility.SetDirty(dropSystem);
            Debug.Log($"{addedCount} drops have been added to the system");
        }
        else
        {
            Debug.Log("All available drops are already in the system");
        }
    }

    /// <summary>
    /// Draw the drop settings section
    /// </summary>
    private void DrawDropSettings()
    {
        EditorGUILayout.BeginVertical("Box");
        EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

        // Drop item chance
        EditorGUI.BeginChangeCheck();
        int newDropChance = EditorGUILayout.IntSlider("Drop item Chance (%)",
            dropSystem.GetDropChance(), 0, 100);
        if (EditorGUI.EndChangeCheck())
        {
            dropSystem.SetDropChance(newDropChance);
            EditorUtility.SetDirty(dropSystem);
        }

        // Drop pickup prefab
        EditorGUI.BeginChangeCheck();
        GameObject newPrefab = (GameObject)EditorGUILayout.ObjectField("Drop Pickup Prefab",dropSystem.dropPickupPrefab, typeof(GameObject), false);
        if (EditorGUI.EndChangeCheck())
        {
            dropSystem.dropPickupPrefab = newPrefab;
            EditorUtility.SetDirty(dropSystem);
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// Draw runtime information when playing
    /// This is only shown when the game is running
    /// </summary>
    private void DrawRuntimeInfo()
    {
        EditorGUILayout.BeginVertical("Box");
        EditorGUILayout.LabelField("Runtime Information", EditorStyles.boldLabel);

        EditorGUILayout.LabelField($"Items Dropped This Run: {dropSystem.GetDroppedItemThisRun()}", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"Available Drops Count: {dropSystem.availableDrops.Count}", EditorStyles.miniLabel);

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// Draw the drop list
    /// </summary>
    private void DrawDropList()
    {
        List<DropItemBase> drops = dropSystem.availableDrops;

        if (drops.Count == 0)
        {
            EditorGUILayout.HelpBox("No drops added yet!", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField($"Available Drops ({drops.Count}):", EditorStyles.boldLabel);

        // Scroll view for a large list of drops
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(400));

        for (int i = 0; i < drops.Count; i++)
        {
            DrawDropItem(drops[i], i);
        }

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// Draw an individual drop item 
    /// This is called for each drop in the list
    /// </summary>
    private void DrawDropItem(DropItemBase drop, int index)
    {
        EditorGUILayout.BeginVertical("Box");

        EditorGUILayout.BeginHorizontal();


        EditorGUI.BeginChangeCheck();

        // set the script to the selected drop
        DropItemBase newDrop = (DropItemBase)EditorGUILayout.ObjectField(drop, typeof(DropItemBase), true);
        if (EditorGUI.EndChangeCheck())
        {
            dropSystem.availableDrops[index] = newDrop;
            EditorUtility.SetDirty(dropSystem);
        }

        // Select button
        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button("Select", GUILayout.Width(60)))
        {
            if (drop != null)
            {
                Selection.activeGameObject = drop.gameObject;
                EditorGUIUtility.PingObject(drop.gameObject);
            }
        }

        // Focus button
        GUI.backgroundColor = Color.yellow;
        if (GUILayout.Button("Focus", GUILayout.Width(60)))
        {
            if (drop != null)
            {
                Selection.activeGameObject = drop.gameObject;
                SceneView.FrameLastActiveSceneView();
            }
        }

        // Remove button
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Remove", GUILayout.Width(60)))
        {
            dropSystem.availableDrops.RemoveAt(index);
            EditorUtility.SetDirty(dropSystem);
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return;
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();

        // Show drop details if enabled and drop exists (if not then show a warning)
        if (drop != null)
        {
            DrawDropDetails(drop);
        }
        else if (drop == null)
        {
            EditorGUILayout.HelpBox("Missing Drop Reference!", MessageType.Warning);
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// Draw detailed information about an drop
    /// Like the name, description, rarity, and activation button
    /// </summary>
    private void DrawDropDetails(DropItemBase drop)
    {
        EditorGUI.indentLevel++;

        // drop name
        string dropName = drop.GetDropName();
        if (string.IsNullOrEmpty(dropName))
            dropName = "Unnamed Drop";
        EditorGUILayout.LabelField($"Name: {dropName}", EditorStyles.miniLabel);

        // description
        string description = drop.GetDescription();
        if (string.IsNullOrEmpty(description))
            description = "No description provided";
        EditorGUILayout.LabelField($"Description: {description}", EditorStyles.wordWrappedMiniLabel);

        // Rarity information
        ItemRarity rarity = drop.GetRarity();
        if (rarity != null)
        {
            // A line to separate the Rarity information
            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField($"Rarity: {rarity.displayName}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Drop Chance: {rarity.dropChance}%", EditorStyles.miniLabel);
        }
        else
        {
            EditorGUILayout.LabelField("Rarity: Not Set", EditorStyles.miniLabel);
        }

        // Test button for runtime
        if (Application.isPlaying)
        {
            GUI.backgroundColor = Color.magenta;
            if (GUILayout.Button("Test Activate", GUILayout.Height(20)))
            {
                drop.Activate();
            }
            GUI.backgroundColor = Color.white;
        }

        EditorGUI.indentLevel--;
    }
}
#endif