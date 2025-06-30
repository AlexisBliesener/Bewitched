using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBar : MonoBehaviour
{
    [Header("Health Bar Settings")]
    public Character character;
    public Slider slider;

    private float currentHealth;
    private float maxHealth;

    // Update is called once per frame
    void Update()
    {
        currentHealth = character.GetHealth();
        slider.value = currentHealth;
    }

    private void OnEnable()
    {
        if (character)
        {
            SetValues();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
    }

    public void SetCharacter(Character inst)
    {
        character = inst;
    }

    public void SetValues()
    {
        slider.maxValue = character.GetMaxHealth();
        maxHealth = character.GetMaxHealth();
        slider.value = character.GetHealth();
    }
}
