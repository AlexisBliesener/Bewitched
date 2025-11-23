using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

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
    [Tooltip("The ID of the upgrade")]
    private string upgradeID;
    [Tooltip("The icons to flash")]
    private Image[] upgradeIcons;
    [SerializeField, Tooltip("Flash speed at the start of the buff")]
    private float startFlashSpeed = 3f;
    [SerializeField, Tooltip("Flash speed when the buff is almost over")]
    private float endFlashSpeed = 8f;

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

        string filePath = Path.Combine(folderPath, nameof(Adrenaline) + FILE_ENDING);
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
        string filePath = Path.Combine(folderPath, nameof(Adrenaline) + FILE_ENDING);

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
    /// <param name="dropData">The drop data of the upgrade</param>
    public void Activate(DropData dropData = null)
    {
        active = true;
        if (dropData != null)
        {
            upgradeID = dropData.GetID();
        }
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
        if (!buffActive) return;
        float elapsed = Time.time - buffActivatedTime;
        float remaining = buffDuration - elapsed;
        // Check if buff should expire
        if (remaining <= 0)
        {
            buffActive = false;
            ChangeImageAlpha(1f);
            return;
        }
        float progress = Mathf.Clamp01(elapsed /buffDuration);
        FlashIcons(progress, elapsed);
    }

    /// <summary>
    /// Called when the possession ability is used
    /// Sets the adrenaline rush effect if the player is controlling the new enemy.
    /// </summary>
    /// <param name="newCharacter">The new character to switch control to</param>
    public void OnCharacterControlChange(Character newCharacter)
    {
        if (active && newCharacter != null && newCharacter != PlayerController.instance.oldHag)
        {
            ApplyAdrenalineRush();
            if (HUDManager.Instance != null && HUDManager.Instance.upgradeDict.ContainsKey(upgradeID))
            {
                upgradeIcons = HUDManager.Instance.upgradeDict[upgradeID].gameObject.GetComponentsInChildren<Image>();
                ChangeImageAlpha(1f);
            }

        }
    }
    /// <summary>
    /// Flash the icons, in upgradeicons 
    /// </summary>
    /// <param name="progress">How much progress to make the icons flash</param>
    /// <param name="elapsed">How much time has passed since the last flash</param>
    private void FlashIcons(float progress, float elapsed)
    {
        if (upgradeIcons == null || upgradeIcons.Length == 0) return;
        float speed = Mathf.Lerp(startFlashSpeed, endFlashSpeed, progress);
        float min  = Mathf.Lerp(0.1f,  0.9f,  progress);
        float phase = Mathf.PingPong(elapsed * speed, 1f);
        float alpha = Mathf.Lerp(1f, min, phase);

        ChangeImageAlpha(alpha);
    }
    
    /// <summary>
    /// Change the alpha of the icons, in upgradeicons (To make the flashing effect)
    /// </summary>
    /// <param name="alpha">how much alpha to set</param>
    private void ChangeImageAlpha(float alpha)
    {
        if (upgradeIcons == null) return;

        for (int i = 0; i < upgradeIcons.Length; i++)
        {
            Image img = upgradeIcons[i];
            if (img == null) continue;

            img.color = new Color(img.color.r, img.color.g, img.color.b, alpha);
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
        if (buffActive)
        {
            return baseDamage * damageMultiplier[GetStackIndex()];
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
    /// <summary>
    /// Gets the index of the stack with fallback to the last stack
    /// </summary>
    /// <returns>The index of the stack</returns>
    private int GetStackIndex()
    {
        return Mathf.Clamp(stackNum, 0, damageMultiplier.Length - 1);
    }
}
