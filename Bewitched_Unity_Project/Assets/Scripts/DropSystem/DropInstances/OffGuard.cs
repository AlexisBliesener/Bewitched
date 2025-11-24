using System.IO;
using UnityEngine;

/// <summary>
/// Handles the "Off Guard" upgrade,
/// Increase the damage and stun effect when hitting an enemy that is winding up for an attack
/// </summary>
public class OffGuard : MonoBehaviour, IDrop
{
    const string FILE_ENDING = ".json";

    [Tooltip("Singleton")]
    public static OffGuard instance;

    [Tooltip("The amount of stacks this upgrade has")]
    public int stackNum { get; set; }

    [SerializeField, Tooltip("The damage increase per stack when hitting a winding up enemy"), Range(0, 100)]
    private float[] damagePercentPerStack = { 10f, 15f, 20f };
    [SerializeField, Tooltip("The stun duration increase per stack when hitting a winding up enemy"), Range(0, 100)]
    private float[] stunDurationPercentPerStack = { 10f, 15f, 20f };
    [Tooltip("Whether the effect is currently active")]
    private bool active = false;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

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

        string filePath = Path.Combine(folderPath, nameof(OffGuard) + FILE_ENDING);
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
        string filePath = Path.Combine(folderPath, nameof(OffGuard) + FILE_ENDING);

        string jsonStr = File.ReadAllText(filePath);

        string[] jsons = jsonStr.Split("|");

        JsonUtility.FromJsonOverwrite(jsons[0], this);

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif

    }

    #endregion

    /// <summary>
    /// Activates the OffGuard effect
    /// </summary>
    /// <param name="dropData">The drop data of the upgrade</param>
    public void Activate(DropData dropData = null)
    {
        active = true;
    }
    /// <summary>
    /// Deactivates the OffGuard effect
    /// </summary>
    public void Deactivate()
    {
        active = false;
    }
    /// <summary>
    /// Get the modified damage for the OffGuard upgrade
    /// </summary>
    /// <param name="baseDamage">The base damage to apply</param>
    /// <returns>The modified damage</returns>
    public float GetModifiedDamage(float baseDamage)
    {
        if (!active) return baseDamage;
        return baseDamage * ( 1 + damagePercentPerStack[GetStackIndex()] / 100f);
    }
    /// <summary>
    /// Get the modified stun duration for the OffGuard upgrade
    /// </summary>
    /// <param name="baseStunDuration">The base stun duration to apply</param>
    /// <returns>The modified stun duration</returns>
    public float GetModifiedStunDuration(float baseStunDuration)
    {
        if (!active) return baseStunDuration;
        return baseStunDuration * (1 + stunDurationPercentPerStack[GetStackIndex()] / 100f);
    }
    /// <summary>
    /// Gets the index of the stack with fallback to the last stack
    /// </summary>
    /// <returns>The index of the stack</returns>
    private int GetStackIndex()
    {
        return Mathf.Clamp(stackNum, 0, damagePercentPerStack.Length - 1);
    }
}