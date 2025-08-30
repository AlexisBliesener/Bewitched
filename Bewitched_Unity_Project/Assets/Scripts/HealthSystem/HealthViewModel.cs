using System;

/// <summary>
/// The view model for health system, it handles communication between model and view
/// </summary>
public class HealthViewModel
{
    private readonly HealthModel model;

    public event Action<float, float> OnHealthChanged; // current, max
    public event Action<float> OnDamaged; // damage amount
    public event Action<float> OnHealed;  // healed amount
    public event Action OnDeath;

    public float Current => model.CurrentHealth;
    public float Max => model.MaxHealth;
    public bool IsDead => model.IsDead;

    public HealthViewModel(HealthModel model)
    {
        this.model = model;
        NotifyHealthChanged();
    }

    public void UpdateOnModel(float deltaTime)
    {
        if (model.IsDead) return;
        float oldHealth = model.CurrentHealth;
        model.UpdateOnModel(deltaTime);
        if (model.CurrentHealth != oldHealth)
        {
            NotifyHealthChanged();
        }
        if (model.IsDead){
            OnDeath?.Invoke();
        }    
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f) return;
        float oldHealth = model.CurrentHealth;
        model.TakeDamage(amount);
        OnDamaged?.Invoke(amount);
        if (model.CurrentHealth != oldHealth){
            NotifyHealthChanged();
        }
        if (model.IsDead){
            OnDeath?.Invoke();
        }
    }

    public void Heal(float amount)
    {
        if (amount <= 0f) return;
        float oldHealth = model.CurrentHealth;
        model.Heal(amount);
        OnHealed?.Invoke(amount);
        if (model.CurrentHealth != oldHealth)
        {
            NotifyHealthChanged();  
        } 
    }

    public void SetToMax()
    {
        model.SetToMax();
        NotifyHealthChanged();
    }

    public void SetMaxHealth(float max)
    {
        model.SetMaxHealth(max);
        NotifyHealthChanged();
    }

    private void NotifyHealthChanged()
    {
        OnHealthChanged?.Invoke(model.CurrentHealth, model.MaxHealth);
    }

    public void SetDecayRate(float rate)
    {
        model.SetDecayRate(rate); 
    }

    public void SetCurrentHealth(float current)
    {
        model.SetCurrentHealth(current);
        NotifyHealthChanged();
        if (model.IsDead){
            OnDeath?.Invoke();
        }  
    }

    public float GetDecayRate() => model.DecayRate;
}
