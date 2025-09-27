using System.IO;
using UnityEngine;

/// <summary>
/// Handles the Vampirism effect, allowing the player to heal a percentage of damage dealt
/// when the effect is active. Implemented as a singleton.
/// </summary>
public class Vampirism : MonoBehaviour, IDrop
{
    const string FILE_ENDING = ".json";

    [Tooltip("Singleton")]
    public static Vampirism instance;

    [Tooltip("The amount of stacks this upgrade has")]
    public int stackNum { get; set; }

    [SerializeField, Tooltip("The percent of health gained from damage done"), Range(0, 100)]
    private int[] percentHeal = { 2 };

    [Tooltip("Whether the effect is currently active.")]
    private bool active = false;

    #region Saving/Loading

    [ContextMenu("Save to JSON")]
    public void SaveToJson()
    {
        string statsStr = JsonUtility.ToJson(this, true);

        string folderPath = Path.Combine(Application.dataPath, "JSON");
        folderPath = Path.Combine(folderPath, "UpgradeStats");
        SeeFilePath();
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string filePath = Path.Combine(folderPath, "Vampirism" + FILE_ENDING);
        File.WriteAllText(filePath, statsStr);


#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif


    }

    [ContextMenu("See File Path")]
    public void SeeFilePath()
    {
        string folderPath = Path.Combine(Application.persistentDataPath, "JSON");
        folderPath = Path.Combine(folderPath, "UpgradeStats");
        Debug.Log("Path To JSON File:");
        Debug.Log(folderPath);
    }

    [ContextMenu("Load From JSON")]
    public void LoadFromJson()
    {

        string folderPath = Path.Combine(Application.dataPath, "JSON");
        folderPath = Path.Combine(folderPath, "UpgradeStats");
        string filePath = Path.Combine(folderPath, "Vampirism" + FILE_ENDING);

        string jsonStr = File.ReadAllText(filePath);

        string[] jsons = jsonStr.Split("|");

        JsonUtility.FromJsonOverwrite(jsons[0], this);

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif

    }

    #endregion

    private void Awake()
    {
        instance = this;
    }

    /// <summary>
    /// Activates the Vampirism effect, enabling life steal.
    /// </summary>
    public void Activate()
    {
        active = true;
    }
    /// <summary>
    /// Deactivates the Vampirism effect, disabling life steal.
    /// </summary>
    public void Deactivate()
    {
        active = false;
    }
    /// <summary>
    /// Steals health from the damage dealt to enemies.
    /// When active, heals the player by a percentage of the damage done,
    /// based on the current stack level.
    /// </summary>
    /// <param name="damageDone">The amount of damage dealt by the player.</param>
    public void stealHealth(float damageDone)
    {
        if (active)
        {
            if (PlayerController.instance != null)
            {
                PlayerController.instance.oldHag.health.AddHealth(damageDone * 0.01f * percentHeal[stackNum]);
            }
            else
            {
                Debug.LogWarning("Player Controller instance is not set!");
            }
        }
    }
}
