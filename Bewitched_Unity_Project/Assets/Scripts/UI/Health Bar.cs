using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBar : MonoBehaviour
{
    [Header("Health Bar Settings")]
    public Slider slider;
    private HealthController healthController;

    public void Subscribe(HealthController hc)
    {
        if (hc == null || slider == null) return;
        healthController = hc;
        slider.maxValue = hc.GetMaxHealth();
        slider.value = hc.GetHealth();

        hc.OnHealthChanged += SetValues;
    }
    
    private void OnDestroy()
    {
        if (healthController != null)
            healthController.OnHealthChanged -= SetValues; // unsubscribe
    }
    
    public void SetValues(float current, float max)
    {
        slider.maxValue = max;
        slider.value = current;
    }
}
