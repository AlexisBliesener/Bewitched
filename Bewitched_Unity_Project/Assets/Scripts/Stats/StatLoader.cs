using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class StatLoader : MonoBehaviour
{
    const string FILE_NAME = "PlayerStats";

    [SerializeField, Tooltip("The Witch")]
    private Hag oldHag;

    [Tooltip("The Player's Stats")]
    private PlayerStats playerStats;

    public static StatLoader instance; // singleton

    // Start is called before the first frame update
    void Start()
    {
        instance = this;

        LoadPlayerStats();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// Saves Stats to JSON - called when scene is being changed
    /// </summary>
    public void SavePlayerStats()
    {
        playerStats.SetSceneSwap(true);
        string playerStatsStr = JsonUtility.ToJson(playerStats);

        string folderPath = Path.Combine(Application.persistentDataPath, "JSON");
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string filePath = Path.Combine(folderPath, FILE_NAME);
        File.WriteAllText(filePath, playerStatsStr);
    }

    public void LoadPlayerStats()
    {
        string folderPath = Path.Combine(Application.persistentDataPath, "JSON");
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string filePath = Path.Combine(folderPath, FILE_NAME);

        if (File.Exists(filePath))
        {
            string jsonStr = File.ReadAllText(filePath);
            playerStats = JsonUtility.FromJson<PlayerStats>(jsonStr);

            if (!playerStats.CheckSwappingScenes())
            {
                playerStats = new PlayerStats(); // If the run ended, start new game
            }
        }
        else
        {
            playerStats = new PlayerStats(); // If a run never began, start new game
        }
    }

    /// <summary>
    /// Starts level up UI and changes stats
    /// </summary>
    /// <returns> Nothing </returns>
    public void HandleLevelUp() // Does nothing for now - will do more after discussing with team :)
    {
        return;
    }
}
