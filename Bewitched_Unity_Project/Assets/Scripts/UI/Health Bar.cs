using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBar : MonoBehaviour
{
    [Header("Health Bar Settings")]
    private HealthViewModel viewModel;
    public Slider slider;

    public void Subscribe(HealthViewModel vm)
    {
        viewModel = vm;
        slider.maxValue = vm.Max;
        slider.value = vm.Current;

        viewModel.OnHealthChanged += SetValues;
    }
    
    private void OnDestroy()
    {
        if (viewModel != null)
            viewModel.OnHealthChanged -= SetValues; // unsubscribe
    }
    
    public void SetValues(float current, float max)
    {
        slider.maxValue = max;
        slider.value = current;
    }
}
