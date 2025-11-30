using UnityEngine;
using DG.Tweening;
using NaughtyAttributes;
using System;
using TMPro;
public class NarrativeTrigger : MonoBehaviour
{
    [SerializeField,Tooltip("The text to show"), TextArea(3,10)]
    private string textToShow;
    [SerializeField,Tooltip("This will hide the narrative panel when the player leaves the trigger")]
    private bool hideWhenPlayerExitsTrigger = true;
    [SerializeField,Tooltip("If true the text will only be shown once and after that it will be destoryed"), Label("Show only once? [This will destroy the object after showing]")]
    private bool showOnlyOnce = true;
    [SerializeField,Tooltip("How long the text will be displayed"), Range(1f,20f)]
    private float displayDurationInSeconds = 5f;
    [Tooltip("The time the text started to be displayed")]
    private float timeStarted = 0f;

    private void Start()
    {
        if (NarrativeStatePopup.instance == null)
        {
            Debug.LogWarning("NarrativeTrigger needs NarrativeStatePopup script to work!");
        }
    }

    /// <summary>
    /// Activates the narrative panel and shows the text when the player enters the trigger
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent(out Character character))
        {
            if (character == PlayerController.instance.currentCharacter)
            {
                timeStarted = Time.time;
                NarrativeStatePopup.instance?.ShowNarrativePanel(NarrativeStatePopup.NarrativeState.NarrativeTrigger, textToShow);
            }
        }
    }
    /// <summary>
    /// Hides the narrative panel when the player exits the trigger if the option is enabled (hideWhenPlayerExitsTrigger)
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        if (hideWhenPlayerExitsTrigger && other.gameObject.TryGetComponent(out Character character))
        {
            if (character == PlayerController.instance.currentCharacter)
            {
                NarrativeStatePopup.instance?.HideNarrativePanel(NarrativeStatePopup.NarrativeState.NarrativeTrigger);
                timeStarted = 0f;
                if (showOnlyOnce)
                {
                    Destroy(gameObject);
                    return;
                }
            }
        }
    }

    private void Update()
    {
        if (timeStarted != 0f && Time.time - timeStarted > displayDurationInSeconds)
        {
            NarrativeStatePopup.instance?.HideNarrativePanel(NarrativeStatePopup.NarrativeState.NarrativeTrigger);
            timeStarted = 0f;
            if (showOnlyOnce)
            {
                Destroy(gameObject);
                return;
            }
        }
    }
}
