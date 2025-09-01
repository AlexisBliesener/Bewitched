using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MiniHealthBar : MonoBehaviour
{
    [Header("Health Bar Settings")]
    public Slider slider;
    private Character character;
    [Tooltip("life time in seconds before health bar disappear if enemy is not recently hit")]
    public float lifeTime = 3;
    private HealthController healthController;

    [Header("Positioning Variables")]
    [Tooltip("Main Camera")]
    public Camera mainCamera;
    [Tooltip("Height offset")]
    public float heightOffset = 1;
    [Tooltip("Canvas to Write On")]
    public Canvas canvas;
    private RectTransform rectTransform;

    void Update()
    {
        if (character == null || healthController == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 charPosition = new Vector3(character.transform.position.x, character.transform.position.y + heightOffset, character.transform.position.z);
        Vector3 screenPos = mainCamera.WorldToScreenPoint(charPosition);
        Vector2 canvasPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, screenPos, canvas.worldCamera, out canvasPos);
        rectTransform.anchoredPosition = canvasPos;

        if (Time.time - character.health.TimeLastHit > lifeTime)
        {
            Destroy(gameObject);
        }
    }

    public void SetCharacter(Character inst)
    {
        character = inst;
    }
    public void Subscribe(HealthController hc)
    {
        healthController = hc;

        // Set initial values
        slider.maxValue = healthController.GetMaxHealth();
        slider.value = healthController.GetHealth();

        // Subscribe to updates
        healthController.OnHealthChanged += SetValues;
        healthController.OnDeath += HandleDeath;

        mainCamera = Camera.main;
        canvas = GameObject.FindGameObjectWithTag("MiniBars").GetComponent<Canvas>();
        transform.parent = canvas.transform;
        rectTransform = GetComponent<RectTransform>();

    }

    public void SetValues(float newHealth, float maxHealth)
    {
        slider.maxValue = maxHealth;
        slider.value = newHealth;

        // Sets bar size proportional to health (2 pixels per hp)
        gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0.5f * maxHealth, 15);
    }
    private void HandleDeath()
    {
        Destroy(gameObject);
    }
}