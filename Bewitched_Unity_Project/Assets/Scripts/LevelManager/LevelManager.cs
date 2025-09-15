using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;

/// <summary>
/// This is a singleton class that manages levels and stages in the game.
/// It supports loading levels in a randomized or sequential manner based on stage settings.
/// Level and stage data is stored in a JSON file.
/// </summary>
public class LevelManager : MonoBehaviour
{

    [Tooltip("Singleton Instance")]
    public static LevelManager Instance { get; private set; }
    [Tooltip("Level Data")]
    public LevelData levelData = new LevelData();

    [Tooltip("Current Stage Index")]
    [SerializeField] private int currentStageIndex = -1;
    [Tooltip("Level loaded event")]
    public event Action<string> OnLevelLoaded;
    [Tooltip("Stage changed event")]
    public event Action<string> OnStageChanged;

    [Tooltip("JSON path relative to Assets folder")]
    public string JSON_PATH = "JSON/levels/LevelData.json";
    private void Awake()
    {
        // Only one instance of LevelManager should be there
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadFromJson();
    }

    #region Level Loading

    /// <summary>
    /// Load the next level based on current stage. It selects a random level from the stage
    /// </summary>
    public void LoadNextLevel()
    {
        // No stages available
        if (levelData.stages.Count == 0) return;
        // Move to the next stage 
        currentStageIndex++;
        // All stages completed
        if (currentStageIndex >= levelData.stages.Count)
        {
            Debug.Log("All stages are completed!");
            return;
        }

        StageData stage = levelData.stages[currentStageIndex];
        string levelName = null;

        if (stage.levels.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, stage.levels.Count);
            levelName = stage.levels[index];
        }
        else
        {
            Debug.LogWarning("No levels found in the current stage");
            return;
        }
        OnStageChanged?.Invoke(stage.stageName);
        OnLevelLoaded?.Invoke(levelName);

        // Only load scene if levelName is valid (avoid errors in tests)
        if (!string.IsNullOrEmpty(levelName))
            LoadScene(levelName);
    
    }
    /// <summary>
    /// Loads a scene by name. Omade it virtual to allow overriding in tests
    /// </summary>
    protected virtual void LoadScene(string levelName)
    {
        SceneManager.LoadScene(levelName);
    }

    #endregion

    #region JSON Load/Save
    /// <summary>
    /// Load level data from JSON file.
    /// </summary>
    public void LoadFromJson()
    {
        string path = Path.Combine(Application.dataPath, JSON_PATH);
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            levelData = JsonUtility.FromJson<LevelData>(json);

            // We should make sure that all lists are initalized to avoid null refs
            foreach (StageData stage in levelData.stages)
            {
                if (stage.levels == null)
                    stage.levels = new List<string>();
            }
        }
        else
        {
            Debug.Log("JSON file not found for LevelManager, starting with empty data.");
            levelData = new LevelData();
        }
    }
    /// <summary>
    /// Save level data to JSON file.
    /// </summary>
    public void SaveToJson()
    {
        string path = Path.Combine(Application.dataPath, JSON_PATH);
        string json = JsonUtility.ToJson(levelData, true);
        File.WriteAllText(path, json);
    }

    #endregion

    #region  Public Getters/Setters
    /// <summary>
    /// Get current stage index
    /// </summary>
    public int GetCurrentStageIndex()
    {
        return currentStageIndex;
    }
    /// <summary>
    /// Set current stage index and reinitialize stage
    /// </summary>
    public void SetCurrentStageIndex(int index)
    {
        if (index < 0 || index >= levelData.stages.Count)
        {
            Debug.LogWarning("Invalid stage index");
        }
        currentStageIndex = index;
    }
    #endregion
}
