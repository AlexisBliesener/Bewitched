using UnityEngine;
using System.IO;

/// <summary>
/// Handels Smarter Control upgrade it lowers the health decay rate of enemies
/// </summary>
public class SmarterControl : MonoBehaviour, IDrop
{
    const string FILE_ENDING = ".json";

    [Tooltip("Singleton instance of SmarterControl.")]
    public static SmarterControl instance;

    [Tooltip("The amount of stacks this upgrade has")]
    public int stackNum { get; set; }

    [Header("Smarter Control Settings")]
    [SerializeField, Tooltip("Minimum allowed decay rate percentage per second")]
    private float minDecayRate = 0.1f;

    [Tooltip("Whether the upgrade is currently active.")]
    private bool active = false;
    [Tooltip("The last enemy possessed by the player")]
    private Character lastPossessedEnemy;

    [Tooltip("The base decay rate of the last enemy possessed by the player")]
    private float lastPossessedEnemyDecayRate;
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

        string filePath = Path.Combine(folderPath, "SmarterControl" + FILE_ENDING);
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
        string filePath = Path.Combine(folderPath, "SmarterControl" + FILE_ENDING);

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
    /// Activates the Smarter Control effect, enabling lower health decay rate.
    /// </summary>
    public void Activate(DropData dropData = null)
    {
        active = true;
    }
    /// <summary>
    /// Deactivates the Smarter Control effect, disabling lower health decay rate.
    /// </summary>
    public void Deactivate()
    {
        active = false;
        ResetLastEnemyDecay();
        lastPossessedEnemy = null;
    }

    public void OnEnable()
    {
        PossessionAbility.CharacterControlChangeEvent += ApplyDecayReduction;
    }

    public void OnDisable()
    {
        PossessionAbility.CharacterControlChangeEvent -= ApplyDecayReduction;
    }


    /// <summary>
    /// Applies the slower health decay rate to the given enemy
    /// Called when the player possesses a new enemy.
    /// </summary>
    /// <param name="enemy">The possessed enemy character.</param>
    public void ApplyDecayReduction(Character enemy)
    {
        if (!active || enemy == null || enemy == PlayerController.instance.oldHag) return;
        if (lastPossessedEnemy != enemy)
        {
            ResetLastEnemyDecay();
        }
        lastPossessedEnemy = enemy;
        HealthController health = enemy.GetComponent<HealthController>();
        if (health == null) return;

        float currentDecay = health.GetDecayRate();
        lastPossessedEnemyDecayRate = currentDecay;
        for (int i = 0; i <= stackNum; i++)
        {
            currentDecay *= 0.5f;
        }

        // clamp to avoid 0 decay rate
        currentDecay = Mathf.Max(currentDecay, minDecayRate);

        health.SetDecay(currentDecay);
    }
    /// <summary>
    /// Reset last enemny decay rate to the original value
    /// </summary>
    private void ResetLastEnemyDecay()
    {
        if (lastPossessedEnemy != null)
        {
            HealthController health = lastPossessedEnemy.GetComponent<HealthController>();
            if (health != null)
            {
                health.SetDecay(lastPossessedEnemyDecayRate);
            }
        }
    }
}
