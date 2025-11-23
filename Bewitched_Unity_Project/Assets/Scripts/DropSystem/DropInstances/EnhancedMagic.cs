using System.IO;
using UnityEngine;
/// <summary>
/// Handles the "Enhanced Magic" upgrade,
/// Increase the range of the Witch's possession range by precent
/// </summary>
public class EnhancedMagic : MonoBehaviour, IDrop
{
    const string FILE_ENDING = ".json";
    [Tooltip("The amount of stacks this upgrade has")]
    public int stackNum { get; set; }
    [SerializeField, Tooltip("The percent of increase in range for each stack"), Range(0, 100)]
    private float[] perStackPercent = { 15f, 30f, 45f };
    [Tooltip("The base start range of the eleth's possession range this from possession class")]
    private float baseStartDistancePossession;
    [Tooltip("The base end range of the eleth's possession range this from possession class")]
    private float baseEndDistancePossession;

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

        string filePath = Path.Combine(folderPath, nameof(EnhancedMagic) + FILE_ENDING);
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
        string filePath = Path.Combine(folderPath, nameof(EnhancedMagic) + FILE_ENDING);

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
        baseStartDistancePossession = PossessionAbility.instance.GetStartingPossessionDistance();
        baseEndDistancePossession = PossessionAbility.instance.GetEndingPossessionDistance();
    }

    /// <summary>
    /// Activates the Enhanced Magic upgrade.
    /// </summary>
    public void Activate(DropData dropData = null)
    {
        active = true;
        ApplyUpgrade();
    }

    /// <summary>
    /// Deactivates the Enhanced Magic upgrade, and resets the cool down time.
    /// </summary>
    public void Deactivate()
    {
        active = false;
        if (PossessionAbility.instance != null)
        {
            PossessionAbility.instance.SetPossessionDistance(baseStartDistancePossession, baseEndDistancePossession);
        }
    }
    /// <summary>
    /// Applies the cooldown reduction to PossessionAbility.
    /// </summary>
    private void ApplyUpgrade()
    {
        if (!active || PossessionAbility.instance == null) return;
        float startPossessionDistance = baseStartDistancePossession * (1 + perStackPercent[GetStackIndex()] / 100f);
        float endPossessionDistance = baseEndDistancePossession * (1 + perStackPercent[GetStackIndex()] / 100f);
        PossessionAbility.instance.SetPossessionDistance(startPossessionDistance, endPossessionDistance);
    }
    /// <summary>
    /// Gets the index of the stack with fallback to the last stack
    /// </summary>
    /// <returns>The index of the stack</returns>
    private int GetStackIndex()
    {
        return Mathf.Clamp(stackNum, 0, perStackPercent.Length - 1);
    }
}
