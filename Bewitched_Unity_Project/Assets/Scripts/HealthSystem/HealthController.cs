using UnityEngine;
using System;

/// <summary>
/// This has to be attached to a character (player or enemy)
/// </summary>
public class HealthController : MonoBehaviour
{
    [Tooltip("Maximum health for this character.")]
    [SerializeField] private float maxHealth = 100f;
    [Tooltip("Health decay per second.")]
    [SerializeField] private float decayRate = 0f;
    [Tooltip("Enable automatic health decay each frame.")]
    private bool updateOnModel = true;
    [Tooltip("If true, this character cannot take damage from TakeDamage and DrainLife.")]
    private bool invincible = false;
    [Tooltip("Prefab for mini health bar.")]
    public GameObject miniBarPrefab;
    /// <summary> Reference to the mini health bar instance. </summary>
    private GameObject minibar;
    /// <summary>Current health value.</summary>
    public float CurrentHealth { get; private set; }
    /// <summary>Returns true if the character is dead.</summary>
    public bool IsDead => CurrentHealth <= 0f;

    /// Timestamp of last received damage (set by this controller)
    public float TimeLastHit { get; private set; } = -Mathf.Infinity;

    public event Action<float, float> OnHealthChanged; // current, max
    public event Action<float> OnDamaged; // amount
    public event Action<float> OnHealed; // amount
    public event Action OnDeath;

    private void Awake()
    {
        CurrentHealth = maxHealth;
        NotifyHealthChanged();
    }

    private void Update()
    {
        // If we don't auto update or already dead, skip!
        if (!updateOnModel || IsDead) return;
        if (decayRate > 0f)
        {
            float old = CurrentHealth;
            CurrentHealth = Mathf.Max(0f, CurrentHealth - decayRate * Time.deltaTime);
            if (CurrentHealth != old)
            {
                NotifyHealthChanged();
                if (IsDead) OnDeath?.Invoke();
            }
        }
    }

    #region Public Functions
    /// <summary> Get current health</summary>
    public float GetHealth() => CurrentHealth;
    /// <summary> Get max health</summary>
    public float GetMaxHealth() => maxHealth;
    /// <summary> Get current health decay rate per second.</summary>
    public float GetDecayRate() => decayRate;
    /// <summary>
    /// Set current health directly. Clamped between 0 and max health.
    /// Does not trigger OnDamaged or OnHealed events, but will trigger OnDeath if set to zero.
    /// </summary>
    public void SetCurrentHealth(float current)
    {
        CurrentHealth = Mathf.Clamp(current, 0f, maxHealth);
        NotifyHealthChanged();
        if (IsDead) OnDeath?.Invoke();
    }
    /// <summary>
    /// Set current health to max health.
    /// and will trigger OnHealthChanged event.
    /// </summary>
    public void SetHealthToMax()
    {
        CurrentHealth = maxHealth;
        NotifyHealthChanged();
    }
    /// <summary>
    /// Reduces health by some amount. Updates damage events and timestamp.
    /// </summary>
    public void SubHealth(float amt)
    {
        if (IsDead || amt <= 0f || invincible) return;
        float old = CurrentHealth;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - amt);

        TimeLastHit = Time.time;
        OnDamaged?.Invoke(amt);

        if (CurrentHealth != old) NotifyHealthChanged();
        if (IsDead) OnDeath?.Invoke();
    }

    /// <summary>
    /// This will drain life without triggering OnDamaged or updating TimeLastHit.
    /// </summary>
    public void DrainLife(float amt)
    {

        if (IsDead || amt <= 0f || invincible) return;
        float old = CurrentHealth;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - amt);

        if (CurrentHealth != old) NotifyHealthChanged();
        if (IsDead) OnDeath?.Invoke();
    }
    /// <summary>
    /// Heal the character.
    /// </summary>
    public void AddHealth(float amt)
    {
        if (IsDead || amt <= 0f) return;

        float old = CurrentHealth;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amt);

        OnHealed?.Invoke(amt);

        if (CurrentHealth != old) NotifyHealthChanged();
    }
    /// <summary>
    /// Set health decay rate per second. Clamped to zero or more.
    /// </summary>
    public void SetDecay(float perSecond)
    {
        decayRate = Mathf.Max(0f, perSecond);
    }

    public void ShowMiniHealthBar(bool show, Character character = null)
    {
        if (!show)
        {
            if (minibar != null) Destroy(minibar);
            return;
        }
        if (minibar != null) Destroy(minibar);
        Debug.Log("Showing mini health bar");
        minibar = Instantiate(miniBarPrefab);
        minibar.GetComponent<MiniHealthBar>().SetCharacter(character);
        minibar.GetComponent<MiniHealthBar>().Subscribe(this);
    }

    /// <summary>
    /// Set maximum health. Clamped to 1 or more. 
    /// Current health is adjusted if above new max.
    /// </summary>
    public void SetMaxHealth(float max)
    {
        maxHealth = Mathf.Max(1f, max);
        CurrentHealth = Mathf.Min(CurrentHealth, maxHealth);
        NotifyHealthChanged();
    }
    /// <summary>
    /// Enable or disable automatic health decay each frame.
    /// </summary>
    public void EnableUpdateModel(bool enable) => updateOnModel = enable;

    /// <summary>
    /// Set invincibility state. If true, character cannot take damage or be healed.
    /// </summary>
    public void SetInvincible(bool value) => invincible = value;

    #endregion
    private void NotifyHealthChanged()
    {
        // This sends out current and max health values
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }
}
