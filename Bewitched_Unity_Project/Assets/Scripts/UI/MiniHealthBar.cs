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
    [Tooltip("Reference to the health controller")]
    private HealthController healthController;
    [Header("Positioning Variables")]
    [Tooltip("Main Camera")]
    public Camera mainCamera;
    
    [Tooltip("Height offset")]
    public float heightOffset = 2;
    [Tooltip("Canvas to Write On")]
    public Canvas canvas;
    private RectTransform rectTransform;
    [Tooltip("Maximum distance for full scale")]
    public float maxScaleDistance = 10f;
    [Tooltip("Minimum scale when far away")]
    public float minScale = 0.5f;
    [Tooltip("Maximum scale when close")]
    public float maxScale = 1.5f;
    [Tooltip("Original Scale for the healthbar")]
    private Vector3 originalScale;
    [Tooltip("Is the health bar currently visible?")]
    private bool isVisible = true;
    void Start()
    {
        originalScale = transform.localScale;
    }
    
    void Update()
    {
        if (character == null || healthController == null)
        {
            Destroy(gameObject);
            return;
        }
        
        // Update position
        UpdatePosition();
        
        // Update visibility based on camera view, it will hide if behind camera 
        UpdateVisibility();
        
        // Update scale based on distance since we went to third person camera
        UpdateScale();
        
        // Check lifetime
        if (Time.time - character.health.TimeLastHit > lifeTime)
        {
            Destroy(gameObject);
        }
    }

    // <summary>
    // Update the position of the health bar to be above the character; This is called every frame.
    // </summary>
    private void UpdatePosition()
    {
        Vector3 charPosition = new Vector3(character.transform.position.x, character.transform.position.y + heightOffset, character.transform.position.z);

        Vector3 screenPos = mainCamera.WorldToScreenPoint(charPosition);
        Vector2 canvasPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, screenPos, canvas.worldCamera, out canvasPos);
        rectTransform.anchoredPosition = canvasPos;
    }

    // <summary>
    // Update the visibility of the health bar based on if the character was in front of the camera or not; This is called every frame.
    // </summary>
    private void UpdateVisibility()
    {
        Vector3 charPosition = character.transform.position;
        Vector3 screenPos = mainCamera.WorldToScreenPoint(charPosition);

        // Check if character is behind camera 
        bool shouldBeVisible = screenPos.z > 0;

        if (isVisible != shouldBeVisible)
        {
            isVisible = shouldBeVisible;
            gameObject.SetActive(isVisible);
        }
    }
    // <summary>
    // Update the scale of the health bar based on distance from camera; This is called everey frame
    // </summary>    
    private void UpdateScale()
    {
        float distance = Vector3.Distance(mainCamera.transform.position, character.transform.position);
        float scaleFactor = Mathf.Lerp(maxScale, minScale, distance / maxScaleDistance);
        scaleFactor = Mathf.Clamp(scaleFactor, minScale, maxScale);

        transform.localScale = originalScale * scaleFactor;
    }
    // <summary>
    // Set the character for this health bar
    // </summary>
    public void SetCharacter(Character inst)
    {
        character = inst;
    }

    // <summary>
    // Subscribe to a health controller to get updates  
    // </summary>
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
        transform.SetParent(canvas.transform);
        rectTransform = GetComponent<RectTransform>();


    }
    //  <summary>
    // Set the values of the health bar; This is called when health changes 
    // </summary>
    public void SetValues(float newHealth, float maxHealth)
    {
        slider.maxValue = maxHealth;
        slider.value = newHealth;


        // Sets bar size proportional to health (2 pixels per hp)
        gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0.5f * maxHealth, 15);
    }

    // <summary>
    // Handle the death of the character by destroying the health bar.
    // </summary>
    private void HandleDeath()
    {
        Destroy(gameObject);
    }
}