using UnityEngine;
using UnityEngine.UI;

public class EventHealth : EnemyHealth
{
    [SerializeField,Tooltip("The speed of the flashing effect for the health bar")]
    private float flashSpeed = 1.5f;
    [Header("References")]
    [SerializeField, Tooltip("Event health prefab")]
    private HealthBar healthBarUI;
    [SerializeField, Tooltip("The 'Fill' image on the health bar")]
    private Image flashingEffect;
    [Tooltip("Is the health bar currently flashing?")]
    private bool isFlashing = false;
    private void Start()
    {
        if (healthBarUI == null)
        {
            Debug.LogWarning("Health bar prefab is not assigned on Event health !");
            return;
        }
        healthBarUI.Subscribe(this);

        HideHealthBar();
    }
    /// <summary>
    /// Hides the health bar
    /// </summary>
    public void HideHealthBar()
    {
        if (healthBarUI == null) return;
        healthBarUI.gameObject.SetActive(false);
    }
    /// <summary>
    /// Shows the health bar
    /// </summary>
    public void ShowHealthBar()
    {
        if (healthBarUI == null) return;
        healthBarUI.gameObject.SetActive(true);
    }

    /// <summary>
    /// Destroy the health bar when the health controller is destroyed
    /// </summary>
    private void OnDestroy()
    {
        if (healthBarUI == null) return;
        healthBarUI.gameObject.SetActive(false);
    }
    /// <summary>
    /// Update the flashing effect of the health bar
    /// </summary>
    private void LateUpdate()
    {
        if (!isFlashing) return;

        float phase = Mathf.PingPong(Time.time * flashSpeed, 1f);
        float alpha = Mathf.Lerp(0f, 1f, phase);
        flashingEffect.color = new Color(flashingEffect.color.r, flashingEffect.color.g, flashingEffect.color.b, alpha);
    }
    /// <summary>
    /// Set the flashing state of the health bar
    /// </summary>
    public void SetFlashing(bool val)
    {
        isFlashing = val;
        if (!isFlashing)
        {
            flashingEffect.color = new Color(flashingEffect.color.r, flashingEffect.color.g, flashingEffect.color.b, 1f);
        }
    }
}
