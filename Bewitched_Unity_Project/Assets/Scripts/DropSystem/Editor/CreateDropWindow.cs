#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEditor.Compilation;
using System;

/// <summary>
/// Editor window for creating new drops in the Drop System
/// Accessible via Tools > Drop System > Create New Drop
/// </summary>
public class CreateDropWindow : EditorWindow
{
    [Header("Drop Configuration")]
    [Tooltip("The name of the drop")]
    [SerializeField] private string dropName = "";
    [Tooltip("The description of the drop")]
    [SerializeField] private string dropDescription = "";
    [Tooltip("The icon of the drop")]
    [SerializeField] private Sprite dropIcon;
    [Tooltip("The rarity of the drop")]
    [SerializeField] private ItemRarity dropRarity;
    [Tooltip("The path to the folder where the scripts will be created")]
    private readonly string FOLDER_PATH_SCRIPTS = "Assets/Scripts/DropSystem/Drops";
    [Tooltip("The path to the folder where the prefabs will be created")]
    private readonly string FOLDER_PATH_PREFABS = "Assets/Prefabs/Drops";
    [Tooltip("This is used so when we have a lot of drops, it will enable the scroll view")]
    private Vector2 scrollPosition = Vector2.zero;
    
    [Tooltip("This is used to track if the script is being created. To start creating the prefab, and then attach the script to it")]
    private bool scriptCreated = false;
    [Tooltip("This is used to track if the script is being compiled")]
    private bool isCompiling = false;
    [Tooltip("This is the name of the script that was created")]
    private string createdScriptClassName = "";
    /// <summary>
    /// Update the prefab creation when the script is compiled
    /// </summary>
    public void Update()
    {
        if (!isCompiling && scriptCreated)
        {
            CreatePrefab();
        }
    }
    /// <summary>
    /// Add menu item to Tools menu
    /// Why it's a static function? unity wants that :) 
    /// </summary>
    [MenuItem("Tools/Drop System/Create New Drop")]
    public static void ShowWindow()
    {
        CreateDropWindow window = GetWindow<CreateDropWindow>();
        window.titleContent = new GUIContent("Create New Drop");
        window.minSize = new Vector2(500, 500); 
        window.Show();
    }
    /// <summary>
    /// Enable the compilation events. TO track when the script is compiled
    /// </summary>
    private void OnEnable()
    {
        // Subscribe to compilation events to detect when the script is compiled
        CompilationPipeline.compilationStarted += OnCompilationStarted;
        CompilationPipeline.compilationFinished += OnCompilationFinished;
    }
    /// <summary>
    /// Disable the compilation events
    /// </summary>
    private void OnDisable()
    {
        // Unsubscribe from compilation events
        CompilationPipeline.compilationStarted -= OnCompilationStarted;
        CompilationPipeline.compilationFinished -= OnCompilationFinished;
    }
    /// <summary>
    /// Handle when the script is being compiled
    /// </summary>
    private void OnCompilationStarted(object obj)
    {
        isCompiling = true;
        Repaint();
    }
    /// <summary>
    /// Handle when the script is compiled
    /// </summary>
    private void OnCompilationFinished(object obj)
    {
        isCompiling = false;
        Repaint();
    }
    
    /// <summary>
    /// Draw the window 
    /// Drop Information Section
    /// Preview Section
    /// create Button
    /// </summary>
    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Drop Information Section
        DrawDropInfoSection();

        EditorGUILayout.Space(10);

        // Preview Section
        DrawPreviewSection();

        EditorGUILayout.Space(10);

        // create Button
        DrawCreateButton();

        EditorGUILayout.EndScrollView();
    }
    
    
    /// <summary>
    /// Draw the drop information input fields
    /// </summary>
    private void DrawDropInfoSection()
    {
        EditorGUILayout.BeginVertical("Box");
        EditorGUILayout.LabelField("Drop Information", EditorStyles.boldLabel);
        
        // Drop Name
        EditorGUILayout.LabelField("Drop Name:", EditorStyles.miniLabel);
        string newName = EditorGUILayout.TextField(dropName);
        if (newName != dropName)
        {
            dropName = newName;
            // Reset script creation state when name change
            scriptCreated = false;
            createdScriptClassName = "";
        }
        
        // Drop Description
        EditorGUILayout.LabelField("Description:", EditorStyles.miniLabel);
        dropDescription = EditorGUILayout.TextArea(dropDescription, GUILayout.Height(60));
        
        // Drop Icon
        EditorGUILayout.LabelField("Icon:", EditorStyles.miniLabel);
        dropIcon = (Sprite)EditorGUILayout.ObjectField(dropIcon, typeof(Sprite), false);
        
        // Drop Rarity
        EditorGUILayout.LabelField("Rarity:", EditorStyles.miniLabel);
        dropRarity = (ItemRarity)EditorGUILayout.ObjectField(dropRarity, typeof(ItemRarity), false);
        
        EditorGUILayout.EndVertical();
    }

    
    /// <summary>
    /// Draw preview section showing what will be created
    /// </summary>
    private void DrawPreviewSection()
    {
        EditorGUILayout.BeginVertical("Box");
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
        string className = dropName.Replace(" ", "");
        string prefabName = $"Drop_{dropName}";

        // show the rarity details if it's not null
        if (dropRarity != null)
        {
            EditorGUILayout.LabelField($"Rarity: {dropRarity.displayName}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Drop Chance: {dropRarity.dropChance}%", EditorStyles.miniLabel);
        }
        else
        {
            EditorGUILayout.LabelField("Rarity: Not Set", EditorStyles.miniLabel);
        }
        EditorGUILayout.LabelField($"Script Path: {FOLDER_PATH_SCRIPTS}/{className}.cs", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"Prefab Path: {FOLDER_PATH_PREFABS}/{prefabName}.prefab", EditorStyles.miniLabel);
        
        EditorGUILayout.EndVertical();
    }
    
    /// <summary>
    /// Draw create button 
    /// Create Button
    /// </summary>
    private void DrawCreateButton()
    {
        
        // Create Button
        bool canCreateScript = !string.IsNullOrEmpty(dropName) && !isCompiling;
        EditorGUI.BeginDisabledGroup(!canCreateScript);
        
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Create New Drop ", GUILayout.Height(30)))
        {
            CreateScript();
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUI.EndDisabledGroup();

    }
    
    /// <summary>
    /// Create the drop script
    /// </summary>
    private void CreateScript()
    {
        try
        {
            string className = dropName.Replace(" ", "");
            
            // Create directory if needed
            CreateDirectoryIfNeeded(FOLDER_PATH_SCRIPTS);
            
            // Create the script
            string scriptPath = Path.Combine(FOLDER_PATH_SCRIPTS, $"{className}.cs");
            
            if (File.Exists(scriptPath))
            {
                if (!EditorUtility.DisplayDialog("File Exists", 
                    $"Script '{className}.cs' already exists. Overwrite it?", 
                    "Yes", "No"))
                {
                    
                    return;
                }
            }
            
            string scriptContent = GetScriptContent(className);
            File.WriteAllText(scriptPath, scriptContent);
            
            // Mark script as created
            scriptCreated = true;
            createdScriptClassName = className;
            
            // Refresh assets
            AssetDatabase.Refresh();
            
            Debug.Log($"Created drop script: {scriptPath}");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Failed to create a script: {e.Message}", "OK");
        }
    }
    
    /// <summary>
    /// Create the drop prefab
    /// </summary>
    private void CreatePrefab()
    {
        try
        {
            string prefabName = $"Drop_{dropName}";
            string prefabPath = Path.Combine(FOLDER_PATH_PREFABS, $"{prefabName}.prefab");
            
            // Create directory if needed
            CreateDirectoryIfNeeded(FOLDER_PATH_PREFABS);
            
            if (File.Exists(prefabPath))
            {
                if (!EditorUtility.DisplayDialog("File Exists", 
                    $"Prefab '{prefabName}.prefab' already exists. Overwrite it?", 
                    "Yes", "No"))
                {
                    return;
                }
            }
            
            // Create GameObject
            GameObject dropObject = new GameObject(prefabName);
            
            // Find and add the custom script component
            Type dropType = FindDropType(createdScriptClassName);
            
            if (dropType != null)
            {
                Component dropComponent = dropObject.AddComponent(dropType);
                
                // Set values
                if (dropComponent is DropItemBase dropBase)
                {
                    dropBase.SetDropName(dropName);
                    dropBase.SetDescription(dropDescription);
                    if (dropIcon != null) dropBase.SetIcon(dropIcon);
                    if (dropRarity != null) dropBase.SetRarity(dropRarity);
                }
                
                Debug.Log($"Successfully added {createdScriptClassName} component to prefab!");
            }
            else
            {
                // Fall back to default if type not found
                Debug.LogError($"Could not find type '{createdScriptClassName}'. Adding DropItemBase instead.");
                DropItemBase baseComponent = dropObject.AddComponent<DropItemBase>();
                baseComponent.SetDropName(dropName);
                baseComponent.SetDescription(dropDescription);
                if (dropIcon != null) baseComponent.SetIcon(dropIcon);
                if (dropRarity != null) baseComponent.SetRarity(dropRarity);
            }
            
            // Start saving and creating a prefab
            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(dropObject, prefabPath);
            
            DestroyImmediate(dropObject);
            
            Debug.Log($"Created drop prefab: {prefabPath}");
            
            // Reset for adding another drop
            ResetState();
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Failed to create prefab: {e.Message}", "OK");
        }
    }

    /// <summary>
    /// Find the drop type after compilation
    /// </summary>
    private Type FindDropType(string className)
    {
        // This is suggested by a comment on this page: https://discussions.unity.com/t/unable-to-get-type-from-string-from-the-editor-solved/903315/8
        foreach (var t in TypeCache.GetTypesDerivedFrom<Component>())
        {
            if (t.Name == className)
            {
                return t;
            }
        }
        return null;
    }

    /// <summary>
    /// Reset the state for creating a new drop
    /// </summary>
    private void ResetState()
    {
        scriptCreated = false;
        createdScriptClassName = "";
        dropName = "";
        dropDescription = "";
        dropIcon = null;
        dropRarity = null;
        
        Repaint();
    }
    
    /// <summary>
    /// Generate the script content for the new drop
    /// </summary>
    private string GetScriptContent(string className)
    {
        return $@"using UnityEngine;

/// <summary>
/// {dropName} drop implementation
/// </summary>
public class {className} : DropItemBase
{{
    
    /// <summary>
    /// Override to implemented the drop's functionality
    /// </summary>
    public override void Activate()
    {{
        base.Activate();
        
        Debug.Log($""Activating {{GetDropName()}} drop!"");
    }}
    
}}";
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