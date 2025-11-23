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
    [SerializeField, Tooltip("The number of hits needed for possession at each stack amount")]
    private int[] hitsNeeded = { 3, 2, 1 }; 
    [Tooltip("The base number of hits needed to get possession from possession class")]
    private int baseHits;
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

        string filePath = Path.Combine(folderPath, nameof(BackupPlan) + FILE_ENDING);
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
        string filePath = Path.Combine(folderPath, nameof(BackupPlan) + FILE_ENDING);

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
        baseHits = PossessionAbility.instance.GetHitsToCharge();
    }

    /// <summary>
    /// Activates the Backup Plan upgrade.
    /// </summary>
    public void Activate(DropData dropData = null)
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
            PossessionAbility.instance.SetHitsToCharge(baseHits);
        }
    }

    /// <summary>
    /// Applies the cooldown reduction to PossessionAbility.
    /// </summary>
    private void ApplyUpgrade()
    {
        if (!active || PossessionAbility.instance == null) return;

        int stackNum = Mathf.Clamp(this.stackNum, 0, hitsNeeded.Length - 1);
        PossessionAbility.instance.SetHitsToCharge(hitsNeeded[stackNum]);
    }
}
