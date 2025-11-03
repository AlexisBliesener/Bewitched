using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Handles the "Grand Finale" upgrade,
/// including enemy explosions and player health damage.
/// </summary>
public class GrandFinale : MonoBehaviour, IDrop
{
    const string FILE_ENDING = ".json";

    [Tooltip("Public singleton instance of GrandFinale.")]
    public static GrandFinale instance;

    [Header("Explosion Settings")]
    [SerializeField, Tooltip("Leave Body Explosion Minimum Radius per stack.")]
    private float[] explosionRadiusMin = { 5 };
    [SerializeField, Tooltip("Leave Body Explosion Maximum Radius per stack.")]
    private float[] explosionRadiusMax = { 20 };
    [SerializeField, Tooltip("Leave Body Explosion Minimum Damage per stack.")]
    private float[] explosionMinDamage = { 15 };
    [SerializeField, Tooltip("Leave Body Explosion Maximum Damage per stack.")]
    private float[] explosionMaxDamage = { 60 };
    [SerializeField, Tooltip("Damage dealt to player health if they stay in the body until it explodes.")]
    private float[] playerExplodeDamage = { 10 };
    [SerializeField, Tooltip("Time to trigger enemy explosion per stack.")]
    private float[] enemyExplosionTime = { 1 };

    [Header("References")]
    [SerializeField, Tooltip("The character layer mask.")]
    private LayerMask characters;
    [SerializeField, Tooltip("The environment layer mask.")]
    private LayerMask environment;
    [SerializeField, Tooltip("The RectTransform of the enemy's health bar.")]
    private RectTransform enemyHealthBar;
    [SerializeField, Tooltip("The prefab of the explosion VFX")]
    private GameObject explosionVFX;

    [Tooltip("Whether the effect is currently active.")]
    private bool active = false;
    [Tooltip("This is to check if the character did call explode function before so the player will not be damaged.")]
    private Character lastExplodedCharater;

    [Tooltip("The amount of stacks this upgrade has")]
    public int stackNum { get; set; }

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

        string filePath = Path.Combine(folderPath, "GrandFinale" + FILE_ENDING);
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
        string filePath = Path.Combine(folderPath, "GrandFinale" + FILE_ENDING);

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

    private void FixedUpdate()
    {
        if (!active) return;

        float healthPercent = PlayerController.instance.currentCharacter.health.GetHealth() /
                              PlayerController.instance.currentCharacter.health.GetMaxHealth();

        if (healthPercent < 0.25f)
            Pulse(1.75f);
        else if (healthPercent < 0.5f)
            Pulse(1.5f);
        else if (healthPercent < 0.75f)
            Pulse(1.25f);
    }

    /// <summary>
    /// Returns if the Grand Finale upgrade is currently active
    /// </summary>
    /// <returns>If the Grand Finale Upgrade is active </returns>
    public bool GetActive()
    {
        return active;
    }

    /// <summary>
    /// Activates the Grand Finale effect.
    /// </summary>
    public void Activate(DropData dropData = null)
    {
        active = true;
    }
    /// <summary>
    /// Deactivates the Grand Finale effect.
    /// </summary>
    public void Deactivate()
    {
        active = false;
    }

    /// <summary>
    /// Triggers the explosion logic depending on possession time and whether it should hit the player.
    /// </summary>
    /// <param name="timePossessing">The duration the player has possessed the enemy.</param>
    /// <param name="explodePlayer">If true, damages the player instead of enemies.</param>
    public void Explode(float timePossessing, bool explodePlayer)
    {
        if (lastExplodedCharater == PlayerController.instance.currentCharacter) return;
        if (explodePlayer)
        {
            ExplodePlayer();
        }
        else
        {
            if (Time.time - timePossessing > enemyExplosionTime[stackNum])
                ExplodeEnemy();
        }
    }


    /// <summary>
    /// Pulses the enemy health bar based on a sine wave scaling effect.
    /// </summary>
    private void Pulse(float pulseAmount)
    {
        float scaleVal = Mathf.Lerp(1, pulseAmount, MathF.Abs(Mathf.Sin(Time.time)) / 2f);
        enemyHealthBar.localScale = new Vector3(scaleVal, scaleVal, scaleVal);
    }

    /// <summary>
    /// Explodes the enemy, damaging all valid characters in range and ending possession.
    /// </summary>
    private void ExplodeEnemy()
    {
        Instantiate(explosionVFX, PlayerController.instance.oldHag.transform.position, Quaternion.identity);

        float radius = Mathf.Lerp(
            explosionRadiusMax[stackNum],
            explosionRadiusMin[stackNum],
            PlayerController.instance.currentCharacter.health.GetHealth() /
            PlayerController.instance.currentCharacter.health.GetMaxHealth()
        );

        Collider[] hits = Physics.OverlapSphere(
            PlayerController.instance.currentCharacter.transform.position,
            radius,
            characters
        );

        foreach (Collider hit in hits)
        {
            Character hitChar = hit.GetComponent<Character>();
            if (hitChar != null &&
                CheckCharacterBehindEnvironment(hitChar.transform) &&
                hitChar.teamID != PlayerController.instance.currentCharacter.teamID)
            {
                float dist = (hitChar.transform.position - PlayerController.instance.currentCharacter.transform.position).magnitude;
                Vector3 direction = (hitChar.transform.position - PlayerController.instance.currentCharacter.transform.position).normalized;

                float dmg = Mathf.Lerp(
                    explosionMinDamage[stackNum],
                    explosionMaxDamage[stackNum],
                    (radius - dist) / radius
                );

                hitChar.health.SubHealth(dmg);
                Enemy enemy = hitChar.GetComponent<Enemy>();
                if (enemy != null && !enemy.IsPlayerControlling())
                    hitChar.health.ShowMiniHealthBar(true, hitChar);

                // Knockback disabled but can be re-added here
                // float knockback = Mathf.Lerp(leaveBodyExplosionMinimumKnockback, leaveBodyExplosionMaximumKnockback, (radius - dist) / radius);
                // hitChar.GetComponent<KnockbackControl>().AddImpact(direction, knockback);
            }
        }

        lastExplodedCharater = PlayerController.instance.currentCharacter;
        if (PlayerController.instance.currentCharacter != PlayerController.instance.GetHag())
        {
            PlayerController.instance.currentCharacter.Die();
        }
        // Switch back to Hag
        PlayerController.instance.currentCharacter.SetControlled(false);
        PossessionAbility.CharacterControlChangeEvent?.Invoke(PlayerController.instance.GetHag());
    }

    /// <summary>
    /// Damages the player if they were inside the possessed body during the explosion.
    /// </summary>
    private void ExplodePlayer()
    {
        Instantiate(explosionVFX, PlayerController.instance.oldHag.transform.position, Quaternion.identity);
        PlayerController.instance.oldHag.health.SubHealth(playerExplodeDamage[stackNum]);
    }

    /// <summary>
    /// Checks if a character is visible (not blocked by environment).
    /// </summary>
    /// <param name="pos">The character's transform.</param>
    /// <returns>True if visible, false if blocked.</returns>
    public bool CheckCharacterBehindEnvironment(Transform pos)
    {
        float dist = (pos.position - PlayerController.instance.currentCharacter.transform.position).magnitude;
        return !Physics.Raycast(PlayerController.instance.currentCharacter.transform.position, pos.position - PlayerController.instance.currentCharacter.transform.position, dist, environment);
    }

}
