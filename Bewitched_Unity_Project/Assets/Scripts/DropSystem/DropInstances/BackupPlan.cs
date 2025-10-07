using System.IO;
using UnityEngine;

/// <summary>
/// Handles the "Backup Plan" upgrade,
/// decreasing the possession cooldown when knocked out of an enemy.
/// </summary>
public class BackupPlan : MonoBehaviour, IDrop
{
    const string FILE_ENDING = ".json";
    [Tooltip("The amount of stacks this upgrade has")]
    public int stackNum { get; set; }

    [Header("Cooldown Settings")]
    [SerializeField, Tooltip("Multiplicative reduction factor per stack for possession cooldown")]
    private float[] cooldownReduction = { 0.9f }; 
    [Tooltip("The base cooldown from possession class")]
    private float baseCooldown;
    [Tooltip("Whether the effect is currently active")]
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

        string filePath = Path.Combine(folderPath, "BackupPlan" + FILE_ENDING);
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
        string filePath = Path.Combine(folderPath, "BackupPlan" + FILE_ENDING);

        string jsonStr = File.ReadAllText(filePath);

        string[] jsons = jsonStr.Split("|");

        JsonUtility.FromJsonOverwrite(jsons[0], this);

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif

    }

    #endregion
    private void Start()
    {
        if (PossessionAbility.instance == null) return;
        baseCooldown = PossessionAbility.instance.GetCooldown();
    }

    /// <summary>
    /// Activates the Backup Plan upgrade.
    /// </summary>
    public void Activate()
    {
        active = true;
        ApplyUpgrade();
    }

    /// <summary>
    /// Deactivates the Backup Plan upgrade, and resets the cool down time.
    /// </summary>
    public void Deactivate()
    {
        active = false;
        if (PossessionAbility.instance != null)
        {
            PossessionAbility.instance.SetCooldown(baseCooldown);
        }
    }

    /// <summary>
    /// Applies the cooldown reduction to PossessionAbility.
    /// </summary>
    private void ApplyUpgrade()
    {
        if (!active || PossessionAbility.instance == null) return;

        float reduction = 1f;
        for (int i = 0; i <= stackNum && i < cooldownReduction.Length; i++)
        {
            reduction *= cooldownReduction[i];
        }

        PossessionAbility.instance.SetCooldown(baseCooldown * reduction);
    }
}
