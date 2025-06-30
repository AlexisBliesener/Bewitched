using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MiniHealthBar : MonoBehaviour
{
    [Header("Health Bar Settings")]
    public Enemy character;
    public Slider slider;
    public float lifeTime = 3;

    [Header("Positioning Variables")]
    [Tooltip("Main Camera")]
    public Camera mainCamera;
    [Tooltip("Height offset")]
    public float heightOffset = 1;
    [Tooltip("Canvas to Write On")]
    public Canvas canvas;

    private float currentHealth;
    private float maxHealth;
    private RectTransform rectTransform;

    void Update()
    {
        if (character == null)
        {
            Destroy(gameObject);
            return;
        }

        currentHealth = character.GetHealth();
        slider.value = currentHealth;

        Vector3 charPosition = new Vector3(character.transform.position.x, character.transform.position.y + heightOffset, character.transform.position.z);
        Vector3 screenPos = mainCamera.WorldToScreenPoint(charPosition);
        Vector2 canvasPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, screenPos, canvas.worldCamera, out canvasPos);
        rectTransform.anchoredPosition = canvasPos;

        if (Time.time - character.GetTimeLastHit() > lifeTime)
        {
            Destroy(gameObject);
        }
    }

    public void SetCharacter(Enemy inst)
    {
        character = inst;
        SetValues();
        mainCamera = Camera.main;
        canvas = GameObject.FindGameObjectWithTag("MiniBars").GetComponent<Canvas>();
        transform.parent = canvas.transform;
        rectTransform = GetComponent<RectTransform>();
    }

    public void SetValues()
    {
        slider.maxValue = character.GetMaxHealth();
        maxHealth = character.GetMaxHealth();
        slider.value = character.GetHealth();

        // Sets bar size proportional to health (2 pixels per hp)
        gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0.5f * maxHealth, 15);
    }
}