using System.IO;
using UnityEngine;

/// <summary>
/// Handles the "Clear Focus" upgrade,
/// decreasing the time taken to focus the possession cone.
/// </summary>
public class ClearFocus : MonoBehaviour, IDrop
{
    const string FILE_ENDING = ".json";

    [Tooltip("Singleton instance of ClearFocus")]
    public static ClearFocus instance;

    [Tooltip("The amount of stacks this upgrade has.")]
    public int stackNum { get; set; }

    [Header("Focus Settings")]
    [SerializeField, Tooltip("Base reduction multiplier for focus time per stack")]
    private float[] focusTimeReduction = { 0.9f }; 
    [Tooltip("The base focus time for possession")]
    private float baseFocusTime;
    [Tooltip("Whether the upgrade effect is currently active.")]
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

        string filePath = Path.Combine(folderPath, "ClearFocus" + FILE_ENDING);
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
        string filePath = Path.Combine(folderPath, "ClearFocus" + FILE_ENDING);

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
    private void Start()
    {
        if (PossessionAbility.instance == null) return;
        baseFocusTime = PossessionAbility.instance.GetocusTime();
    }

    /// <summary>
    /// Activates the Clear Focus upgrade.
    /// </summary>
    public void Activate()
    {
        active = true;
        ApplyUpgrade();
    }

    /// <summary>
    /// Deactivates the Clear Focus upgrade.
    /// </summary>
    public void Deactivate()
    {
        active = false;
    }

    /// <summary>
    /// Applies the focus time reduction to PossessionAbility.
    /// </summary>
    private void ApplyUpgrade()
    {
        if (!active || PossessionAbility.instance == null) return;

        float reduction = 1f;
        for (int i = 0; i <= stackNum && i < focusTimeReduction.Length; i++)
        {
            reduction *= focusTimeReduction[i]; 
        }

        PossessionAbility.instance.SetFocusTime(baseFocusTime * reduction);
    }
}
