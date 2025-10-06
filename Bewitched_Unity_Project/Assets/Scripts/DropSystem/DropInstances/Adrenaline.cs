using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Handles the "Adrenaline" upgrade,
/// increasing damage dealt to enemies when swapping to them 
/// </summary>
public class Adrenaline : MonoBehaviour, IDrop
{
    const string FILE_ENDING = ".json";

    [Tooltip("Singleton")]
    public static Adrenaline instance;

    [Tooltip("The amount of stacks this upgrade has")]
    public int stackNum { get; set; }

    [SerializeField, Tooltip("The damage multiplier per stack when swapping enemies")]
    private float[] damageMultiplier = { 1.25f };

    [SerializeField, Tooltip("Duration of the damage buff in seconds")]
    private float buffDuration = 5f;

    [Tooltip("Whether the effect is currently active")]
    private bool active = false;

    [Tooltip("Whether the damage buff is currently applied")]
    private bool buffActive = false;

    [Tooltip("Time when the buff was activated")]
    private float buffActivatedTime = -Mathf.Infinity;

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

        string filePath = Path.Combine(folderPath, "Adrenaline" + FILE_ENDING);
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
        string filePath = Path.Combine(folderPath, "Adrenaline" + FILE_ENDING);

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
    /// Activates the Adrenaline effect
    /// </summary>
    public void Activate()
    {
        active = true;
    }
    /// <summary>
    /// Deactivates the Adrenaline effect
    /// </summary>
    public void Deactivate()
    {
        active = false;
        buffActive = false;
    }

    private void Update()
    {
        // Check if buff should expire
        if (buffActive && Time.time - buffActivatedTime >= buffDuration)
        {
            buffActive = false;
        }
    }

    /// <summary>
    /// Called when the possession ability is used
    /// Sets the adrenaline rush effect if the player is controlling the new enemy.
    /// </summary>
    /// <param name="newCharacter">The new character to switch control to</param>
    public void OnCharacterControlChange(Character newCharacter)
    {
        if (newCharacter != null && newCharacter != PlayerController.instance.oldHag)
        {
            ApplyAdrenalineRush();
        }
    }

    private void OnEnable()
    {
        PossessionAbility.CharacterControlChangeEvent += OnCharacterControlChange;
    }
    private void OnDisable()
    {
        PossessionAbility.CharacterControlChangeEvent -= OnCharacterControlChange;
    }

    /// <summary>
    /// Apply the adrenaline buff when swapping to a new enemy
    /// Should be called by the possession system when a new enemy is possessed.
    /// </summary>
    public void ApplyAdrenalineRush()
    {
        if (active)
        {
            buffActive = true;
            buffActivatedTime = Time.time;
        }
    }
    /// <summary>
    /// Get the modified damage for the adrenaline rush effect
    /// </summary>
    /// <param name="baseDamage">The base damage to apply</param>
    /// <returns>The modified damage</returns>
    public float GetModifiedDamage(float baseDamage)
    {
        if (buffActive && stackNum < damageMultiplier.Length)
        {
            return baseDamage * damageMultiplier[stackNum];
        }
        return baseDamage;
    }
    /// <summary>
    /// Checks if the adrenaline rush effect is currently active
    /// </summary>
    /// <returns>True if active, false otherwise</returns>
    public bool IsBuffActive()
    {
        return buffActive;
    }

}
