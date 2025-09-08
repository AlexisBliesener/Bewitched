#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Editor window for creating new rarities in the Drop System
/// Accessible via Tools > Drop System > Create New Rarity Item
/// </summary>
public class CreateRarityWindow : EditorWindow
{
    [Header("Rarity Configuration")]
    [Tooltip("The display name of the rarity")]
    [SerializeField] private string displayName = "";
    [Tooltip("The drop chance of the rarity")]
    [SerializeField] private int dropChance = 50;
    [Tooltip("The path to the folder where the rarity assets will be created")]
    private readonly string FOLDER_PATH = "Assets/SavedData/DropSystem";
    [Tooltip("This is used so when we have a lot of rarities, it will enable the scroll view")]
    private Vector2 scrollPosition = Vector2.zero;

    /// <summary>
    /// Add menu item to Tools menu
    /// Why it's a static function? unity wants that :) 
    /// </summary>
    [MenuItem("Tools/Drop System/Create New Rarity Item")]
    public static void ShowWindow()
    {
        CreateRarityWindow window = GetWindow<CreateRarityWindow>();
        window.titleContent = new GUIContent("Create New Rarity");
        window.minSize = new Vector2(400, 300);
        window.Show();
    }

    /// <summary>
    /// Draw the window 
    /// Rarity Information Section
    /// Preview Section
    /// Create Button
    /// </summary>
    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Rarity Information Section
        DrawRarityInfoSection();

        EditorGUILayout.Space(10);

        // Preview Section
        DrawPreviewSection();

        EditorGUILayout.Space(10);

        // Create Button
        DrawCreateButton();

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// Draw the rarity information input fields
    /// </summary>
    private void DrawRarityInfoSection()
    {
        EditorGUILayout.BeginVertical("Box");
        EditorGUILayout.LabelField("Rarity Information", EditorStyles.boldLabel);
        
        // Display Name
        EditorGUILayout.LabelField("Display Name:", EditorStyles.miniLabel);
        displayName = EditorGUILayout.TextField(displayName);
        
        // Drop Chance
        EditorGUILayout.LabelField("Drop Chance:", EditorStyles.miniLabel);
        dropChance = EditorGUILayout.IntSlider(dropChance, 1, 100);
        EditorGUILayout.LabelField($"Drop Chance: {dropChance}%", EditorStyles.miniLabel);
        
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// Draw preview section showing what will be created
    /// </summary>
    private void DrawPreviewSection()
    {
        EditorGUILayout.BeginVertical("Box");
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
        string fileName = GetValidFileName(displayName);
        
        EditorGUILayout.LabelField($"Asset Path: {FOLDER_PATH}/{fileName}.asset", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"Display Name: {displayName}", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"Drop Chance: {dropChance}%", EditorStyles.miniLabel);
        
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// Draw create button 
    /// Create Button
    /// </summary>
    private void DrawCreateButton()
    {
        // Create Button
        bool canCreate = !string.IsNullOrEmpty(displayName);
        EditorGUI.BeginDisabledGroup(!canCreate);
        
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Create New Rarity", GUILayout.Height(30)))
        {
            CreateRarity();
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUI.EndDisabledGroup();
    }

    /// <summary>
    /// Create the ItemRarity ScriptableObject
    /// </summary>
    private void CreateRarity()
    {
        try
        {
            // Create directory if needed
            CreateDirectoryIfNeeded(FOLDER_PATH);
            
            string fileName = GetValidFileName(displayName);
            string assetPath = Path.Combine(FOLDER_PATH, $"{fileName}.asset");
            
            if (File.Exists(assetPath))
            {
                if (!EditorUtility.DisplayDialog("File Exists", 
                    $"Rarity '{fileName}.asset' already exists. Overwrite it?", 
                    "Yes", "No"))
                {
                    return;
                }
            }
            
            // Create the ScriptableObject instance
            ItemRarity rarityItem = CreateInstance<ItemRarity>();
            rarityItem.displayName = displayName;
            rarityItem.dropChance = dropChance;
            
            // Save the asset
            AssetDatabase.CreateAsset(rarityItem, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"Created rarity item: {assetPath}");
            
            // Reset for creating another rarity
            ResetState();
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Failed to create rarity: {e.Message}", "OK");
        }
    }

    /// <summary>
    /// Reset the state for creating a new rarity
    /// </summary>
    private void ResetState()
    {
        displayName = "";
        dropChance = 50;
        Repaint();
    }

    /// <summary>
    /// Convert display name to a valid file name
    /// </summary>
    private string GetValidFileName(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "NewRarity";
        
        // Remove spaces and replace with underscore
        string fileName = input.Replace(" ", "_");
        
        return string.IsNullOrEmpty(fileName) ? "NewRarity" : fileName;
    }

    /// <summary>
    /// Create directory if it doesn't exist
    /// </summary>
    private void CreateDirectoryIfNeeded(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
        }
    }
}

#endif