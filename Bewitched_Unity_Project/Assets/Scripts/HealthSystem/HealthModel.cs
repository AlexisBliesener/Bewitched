using UnityEngine;

/// <summary>
/// This is the base health model which define the health logic for the character
/// </summary>
[System.Serializable]
public abstract class HealthModel
{
    public float CurrentHealth { get; protected set; }
    public float MaxHealth { get; protected set; }
    public float DecayRate { get; protected set; } // this is per second

    public bool IsDead => CurrentHealth <= 0f;

    protected HealthModel(float maxHealth, float decayRate)
    {
        MaxHealth = maxHealth;
        CurrentHealth = maxHealth;
        DecayRate = decayRate;
    }

    public virtual void UpdateOnModel(float deltaTime)
    {
        if (IsDead) return;
        if (DecayRate <= 0f) return;
        CurrentHealth -= DecayRate * deltaTime;
        CurrentHealth = Mathf.Max(0f, CurrentHealth);
    }

    public virtual void TakeDamage(float amount)
    {
        if (IsDead) return;
        CurrentHealth -= amount;
        CurrentHealth = Mathf.Max(0f, CurrentHealth);
    }

    public virtual void Heal(float amount)
    {
        if (IsDead) return;
        CurrentHealth += amount;
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth);
    }

    public virtual void SetToMax()
    {
        CurrentHealth = MaxHealth;
    }
    public virtual void SetMaxHealth(float max)
    {
        MaxHealth = Mathf.Max(1f, max);
        CurrentHealth = Mathf.Min(CurrentHealth, MaxHealth);
    }
    public void SetCurrentHealth(float current)
    {
        CurrentHealth = Mathf.Clamp(current, 0f, MaxHealth);
    }
    public void SetDecayRate(float rate)
    {
        DecayRate = Mathf.Max(0f, rate);
    }
}
