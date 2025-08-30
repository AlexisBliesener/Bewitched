using UnityEngine;
using System;

/// <summary>
/// This has to be attached to a character (player or enemy)
/// </summary>
public class HealthController : MonoBehaviour
{
    [Header("Note designers: Max health and decay rate per second can be changed on the character stats!")]

    private float maxHealth = 100f;
    private float decayRate = 0f;
    private bool updateOnModel = true;

    public float CurrentHealth { get; private set; }
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

    public float GetCurrent() => CurrentHealth;
    public float GetMax() => maxHealth;
    public float GetDecay() => decayRate;

    public void SetCurrentHealth(float current)
    {
        CurrentHealth = Mathf.Clamp(current, 0f, maxHealth);
        NotifyHealthChanged();
        if (IsDead) OnDeath?.Invoke();
    }

    public void SetToMax()
    {
        CurrentHealth = maxHealth;
        NotifyHealthChanged();
    }

    public void TakeDamage(float amt)
    {
        if (IsDead || amt <= 0f) return;
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

        if (IsDead || amt <= 0f) return;
        float old = CurrentHealth;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - amt);

        if (CurrentHealth != old) NotifyHealthChanged();
        if (IsDead) OnDeath?.Invoke();
    }

    public void Heal(float amt)
    {
        if (IsDead || amt <= 0f) return;

        float old = CurrentHealth;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amt);

        OnHealed?.Invoke(amt);

        if (CurrentHealth != old) NotifyHealthChanged();
    }

    public void SetDecay(float perSecond)
    {
        decayRate = Mathf.Max(0f, perSecond);
    }

    public void SetMaxHealth(float max)
    {
        maxHealth = Mathf.Max(1f, max);
        CurrentHealth = Mathf.Min(CurrentHealth, maxHealth);
        NotifyHealthChanged();
    }

    public void EnableUpdateModel(bool enable) => updateOnModel = enable;

    private void NotifyHealthChanged()
    {
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }
}
