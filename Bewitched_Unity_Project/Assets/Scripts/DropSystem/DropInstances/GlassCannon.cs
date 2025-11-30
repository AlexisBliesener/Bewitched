using System.IO;
using UnityEngine;
/// <summary>
/// Handles the "Glass Cannon" upgrade,
/// Half the Witch's total health. Increase all damage done by 100%
/// </summary>
public class GlassCannon : MonoBehaviour, IDrop
{
    const string FILE_ENDING = ".json";
    [Tooltip("Singleton")]
    public static GlassCannon instance;
    [Tooltip("The amount of stacks this upgrade has")]
    public int stackNum { get; set; }
    [SerializeField, Tooltip("The percent of decrease in base health for each stack"), Range(0, 100)]
    private float[] decreaseHealthPercent = { 50f, 75f, 87.5f };
    [SerializeField, Tooltip("The percent of increase in damage for each stack"), Range(0, 500)]
    private float[] increaseDamagePercent = { 100f, 200f, 300f };
    [Tooltip("Eleth reference")]
    private Hag eleth;

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

        string filePath = Path.Combine(folderPath, nameof(GlassCannon) + FILE_ENDING);
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
        string filePath = Path.Combine(folderPath, nameof(GlassCannon) + FILE_ENDING);

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
        if (PlayerController.instance == null || PlayerController.instance.GetHag() == null) return;
        eleth = PlayerController.instance.GetHag();
    }

    /// <summary>
    /// Activates the Glass Cannon upgrade.
    /// </summary>
    public void Activate(DropData dropData = null)
    {
        active = true;
        ApplyUpgrade();
    }

    /// <summary>
    /// Deactivates the Glass Cannon upgrade, and resets the cool down time.
    /// </summary>
    public void Deactivate()
    {
        active = false;
        if (eleth != null)
        {
            eleth.health.SetMaxHealth(eleth.health.GetBaseMaxHealth(), false);
        }
    }
    /// <summary>
    /// Get the modified damage for the Glass Cannon upgrade
    /// </summary>
    /// <param name="baseDamage">The base damage to apply</param>
    /// <returns>The modified damage (if it is inactive, it will return the base damage)</returns>
    public float GetModifiedDamage(float baseDamage)
    {
        if (!active) return baseDamage;
        return baseDamage * (1 + increaseDamagePercent[GetStackIndex()] / 100f);
    }

    /// <summary>
    /// Applies the cooldown reduction to PossessionAbility.
    /// </summary>
    private void ApplyUpgrade()
    {
        if (!active || eleth == null) return;
        float newMaxHealth = eleth.health.GetBaseMaxHealth() * (1 - decreaseHealthPercent[GetStackIndex()] / 100f);
        eleth.health.SetMaxHealth(newMaxHealth, false);
    }
    /// <summary>
    /// Gets the index of the stack with fallback to the last stack
    /// </summary>
    /// <returns>The index of the stack</returns>
    private int GetStackIndex()
    {
        return Mathf.Clamp(stackNum, 0, decreaseHealthPercent.Length - 1);
    }
}