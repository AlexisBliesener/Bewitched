using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles the ShieldingSpirits effect, when possessing an enemy, grant a temporary shield.
/// </summary>
public class ShieldingSpirits : MonoBehaviour, IDrop
{
    const string FILE_ENDING = ".json";

    [Tooltip("The amount of stacks this upgrade has")]
    public int stackNum { get; set; }
    [Header("Shield Settings")]
    [SerializeField, Tooltip("Multiplier applied to base shield amount per stack")]
    private float[] shieldMultipliers = { 1f, 1.25f, 1.5f, 2f };

    [SerializeField, Tooltip("Shield duration per stack (seconds)")]
    private float[] shieldDurations = { 5f, 6f, 7f, 8f };
    [Tooltip("Shield coroutine to cancel it when it's reactived")]
    private Coroutine shieldCoroutine;

    [Tooltip("Whether the effect is currently active.")]
    private bool active = false;
    [Tooltip("The last enemy possessed by the player")]
    private Character lastPossessedCharacter;
    [Tooltip("The max health of the last enemy possessed by the player")]
    private float lastMaxHealth;


    [Header("UI References")]
    [SerializeField, Tooltip("Slider for the shield amount")]
    private Slider shieldSlider;

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

        string filePath = Path.Combine(folderPath, "ShieldingSpirits" + FILE_ENDING);
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
        string filePath = Path.Combine(folderPath, "ShieldingSpirits" + FILE_ENDING);

        string jsonStr = File.ReadAllText(filePath);

        string[] jsons = jsonStr.Split("|");

        JsonUtility.FromJsonOverwrite(jsons[0], this);

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif

    }

    #endregion

    /// <summary>
    /// Activates the ShieldingSpirits effect
    /// </summary>
    public void Activate()
    {
        active = true;
    }
    /// <summary>
    /// Deactivates the ShieldingSpirits effect
    /// </summary>
    public void Deactivate()
    {
        active = false;
        if (shieldCoroutine != null)
        {
            StopCoroutine(shieldCoroutine);
            shieldCoroutine = null;
        }
        if (shieldSlider != null)
        {
            shieldSlider.gameObject.SetActive(false);
        }
        if (lastPossessedCharacter != null)
        {
            lastPossessedCharacter.health.SetMaxHealth(lastMaxHealth);
        }
    }

    private void OnEnable()
    {
        PossessionAbility.CharacterControlChangeEvent += OnCharacterControlChange;
    }

    private void OnDisable()
    {
        PossessionAbility.CharacterControlChangeEvent -= OnCharacterControlChange;

        if (shieldSlider != null)
        {
            shieldSlider.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Called when the player possesses a new enemy.
    /// Applies a temporary shield based on goblin health.
    /// </summary>
    /// <param name="newCharacter">Newly possessed character</param>
    public void OnCharacterControlChange(Character newCharacter)
    {
        if (!active || newCharacter == null || PlayerController.instance == null) return;

        // Don't apply shield if returning to Hag
        if (newCharacter == PlayerController.instance.oldHag)
        {
            if (shieldCoroutine != null)
            {
                StopCoroutine(shieldCoroutine);
                shieldCoroutine = null;
            }
            if (shieldSlider == null)
            {
                Debug.LogWarning("Shield slider UI prefab is null on Shielding Spirits upgrade!");
            }
            else
            {
                shieldSlider.gameObject.SetActive(false);
            }
            return;
        }

        if (shieldCoroutine != null)
        {
            StopCoroutine(shieldCoroutine);
        }

        shieldCoroutine = StartCoroutine(HandleShield(newCharacter));
    }
    /// <summary>
    /// Coroutine to apply shield and remove it after a duration.
    /// </summary>
    private IEnumerator HandleShield(Character target)
    {
        float baseMaxHealth = target.health.GetMaxHealth();
        lastPossessedCharacter = target;
        lastMaxHealth = baseMaxHealth;
        float shieldAmount = (target.health.GetHealth() / 2f) * shieldMultipliers[Mathf.Min(stackNum, shieldMultipliers.Length - 1)];
        float duration = shieldDurations[Mathf.Min(stackNum, shieldDurations.Length - 1)];
        float initialHealth = target.health.GetHealth();
        float maxShieldedHealth = initialHealth + shieldAmount;
        if (shieldSlider != null)
        {
            shieldSlider.gameObject.SetActive(true);
            shieldSlider.minValue = 0f;
            shieldSlider.maxValue = shieldAmount; 
            shieldSlider.value = shieldAmount;    
        }
        target.health.SetMaxHealth(maxShieldedHealth);
        target.health.AddHealth(shieldAmount);

        float startTime = Time.time;

        while (Time.time - startTime < duration)
        {
            if (shieldSlider != null)
            {
                float currentHealth = target.health.GetHealth();
                float currentShield = Mathf.Clamp(currentHealth - initialHealth, 0f, shieldAmount);
                shieldSlider.value = currentShield;
            }
            yield return null;
        }


        target.health.SetMaxHealth(lastMaxHealth);
        shieldCoroutine = null;
        if (shieldSlider != null)
        {
            shieldSlider.gameObject.SetActive(false);
        }
    }

}
