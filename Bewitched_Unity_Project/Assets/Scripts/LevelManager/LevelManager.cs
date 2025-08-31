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
    [SerializeField] private int currentStageIndex = 0;
    [Tooltip("Current Level Index in non-randomized stage")]
    [SerializeField] private int currentLevelIndex = 0;
    [Tooltip("Remaining Levels in Current Stage")]
    private List<string> remainingLevels = new List<string>();
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
        InitializeStage();
    }

    #region Level Loading

    /// <summary>
    /// Load the next level based on current stage and randomization settings.
    /// </summary>
    public void LoadNextLevel()
    {
        // No stages available
        if (levelData.stages.Count == 0) return;

        // All stages completed
        if (currentStageIndex >= levelData.stages.Count)
        {
            Debug.Log("All stages are completed!");
            return;
        }

        StageData stage = levelData.stages[currentStageIndex];
        string levelName = null;

        if (stage.isRandomized)
        {
            if (remainingLevels == null || remainingLevels.Count == 0)
            {
                LoadNextStage();
                return;
            }

            int index = UnityEngine.Random.Range(0, remainingLevels.Count);
            levelName = remainingLevels[index];
            remainingLevels.RemoveAt(index);
        }
        else
        {
            if (currentLevelIndex >= stage.levels.Count)
            {
                LoadNextStage();
                return;
            }

            levelName = stage.levels[currentLevelIndex];
            currentLevelIndex++; 

        }

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

    /// <summary>
    /// Initialize the current stage, setting up remaining levels based on randomization.
    /// </summary>
    private void InitializeStage()
    {
        if (levelData.stages.Count == 0 || currentStageIndex < 0 || currentStageIndex >= levelData.stages.Count)
        {
            remainingLevels = null;
            Debug.LogWarning("No stages available or invalid stage index");
            return;
        }

        StageData stage = levelData.stages[currentStageIndex];

        if (stage.isRandomized)
        {
            remainingLevels = new List<string>(stage.levels);
            currentLevelIndex = 0;
        }
        else
        {
            remainingLevels = null;
            currentLevelIndex = 0;
        }
        Debug.Log("Moved to stage: " + stage.stageName);
        OnStageChanged?.Invoke(stage.stageName);
    }

    /// <summary>
    /// Move to the next stage and initialize it.
    /// </summary>
    private void LoadNextStage()
    {
        currentStageIndex++;
        if (currentStageIndex >= levelData.stages.Count)
        {
            Debug.Log("all stages are completed!");
            return;
        }

        InitializeStage();
        LoadNextLevel();
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
        InitializeStage();

    }
    /// <summary>
    /// Get remaining levels in current stage
    /// </summary>

    public List<string> GetRemainingLevels()
    {
        return remainingLevels;
    }
    /// <summary>
    /// Set remaining levels in current stage
    /// </summary>
    public void SetRemainingLevels(List<string> levels)
    {
        remainingLevels = levels;
    }
    #endregion
}
