using UnityEngine;
using System;

/// <summary>
/// This has to be attached to a character (player or enemy)
/// It creates and own the HealthViewModel (VM)
/// </summary>
public class HealthController : MonoBehaviour
{
    [Header("Note designers: Max health and decay rate per second can be changed on the character stats!")]

    private readonly float maxHealth = 100f;
    private readonly float decayRate = 0f;
    private bool updateOnModel = true;

    public HealthViewModel viewModel { get; private set; }

    /// Timestamp of last received damage (set by this controller)
    public float TimeLastHit { get; private set; } = -Mathf.Infinity;

    public event Action<float, float> OnHealthChanged; // current, max
    public event Action<float> OnDamaged; // amount
    public event Action<float> OnHealed; // amount
    public event Action OnDeath;

    private void Awake()
    {
        Init();
    }

    public void Init()
    {
        if (viewModel != null) return; // already initialized
        HealthModel model = null;
        if (gameObject.CompareTag("Player")){
            model = new PlayerHealthModel(maxHealth, decayRate);
        }else{
            model = new EnemyHealthModel(maxHealth, decayRate);
        }

        viewModel = new HealthViewModel(model);

        // subscribe to the view model events and forward them
        viewModel.OnHealthChanged += (current, max) => OnHealthChanged?.Invoke(current, max);
        viewModel.OnDamaged += amount =>
        {
            TimeLastHit = Time.time;
            OnDamaged?.Invoke(amount);
        };
        viewModel.OnHealed += amount => OnHealed?.Invoke(amount);
        viewModel.OnDeath += () => OnDeath?.Invoke();
    }

    private void Update()
    {
        if (updateOnModel) viewModel.UpdateOnModel(Time.deltaTime);
    }

    
    public float GetCurrent() => viewModel.Current;
    public void SetCurrentHealth(float current) => viewModel.SetCurrentHealth(current);
    public float GetMax() => viewModel.Max;
    public void SetToMax() => viewModel.SetToMax();
    public void TakeDamage(float amt) => viewModel.TakeDamage(amt);
    public void Heal(float amt) => viewModel.Heal(amt);
    public void SetDecay(float perSecond) => viewModel.SetDecayRate(perSecond);
    public void SetMaxHealth(float max) => viewModel.SetMaxHealth(max);
    public float GetDecay() => viewModel.GetDecayRate();
    public void EnableUpdateModel(bool enable) => updateOnModel = enable;
}
