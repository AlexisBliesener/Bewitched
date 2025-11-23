using System.IO;
using UnityEngine;

/// <summary>
/// Handles the "Get Off of Me" upgrade,
/// All knockback effects are increased (Knockback effects are continually multiplied)
/// </summary>
public class GetOffOfMe : MonoBehaviour, IDrop
{
    const string FILE_ENDING = ".json";

    [Tooltip("Singleton")]
    public static GetOffOfMe instance;

    [Tooltip("The amount of stacks this upgrade has")]
    public int stackNum { get; set; }

    [SerializeField, Tooltip("The knockback multiplier per stack when swapping enemies")]
    private float[] knockbackMultiplier = { 1.25f, 1.5f, 1.75f };
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

        string filePath = Path.Combine(folderPath, nameof(GetOffOfMe) + FILE_ENDING);
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
        string filePath = Path.Combine(folderPath, nameof(GetOffOfMe) + FILE_ENDING);

        string jsonStr = File.ReadAllText(filePath);

        string[] jsons = jsonStr.Split("|");

        JsonUtility.FromJsonOverwrite(jsons[0], this);

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif

    }

    #endregion

    /// <summary>
    /// Activates the GetOffOfMe effect
    /// </summary>
    /// <param name="dropData">The drop data of the upgrade</param>
    public void Activate(DropData dropData = null)
    {
        active = true;
    }
    /// <summary>
    /// Deactivates the GetOffOfMe effect
    /// </summary>
    public void Deactivate()
    {
        active = false;
    }
    /// <summary>
    /// Get the modified knockback for the GetOffOfMe upgrage
    /// </summary>
    /// <param name="baseKnockback">The base knockback to apply</param>
    /// <returns>The modified knockback</returns>
    public float GetModifiedKnockback(float baseKnockback)
    {
        if (!active) return baseKnockback;
    
        // if the stack is greater than the length of the array, fall back to the last multiplier
        if (stackNum >= knockbackMultiplier.Length) 
            return baseKnockback * knockbackMultiplier[knockbackMultiplier.Length - 1];

        // otherwise, return the multiplier at the current stack
        return baseKnockback * knockbackMultiplier[stackNum];
    }
}
