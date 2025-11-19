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
    [SerializeField,Tooltip("How long the text will fade out"), Range(0.1f,2f)]
    private float fadeOutDuration = 0.6f;
    [Tooltip("The time the text started to be displayed")]
    private float timeStarted = 0f;
    /// <summary>
    /// Get the correct narrative panel based on the A/B testing state
    /// </summary>
    /// <returns>Which narrative panel to use</returns>
    private GameObject GetNarrativePanel()
    {
        if (PlayerController.instance.narrativePanel == null)
        {
            Debug.LogWarning("Narrative panel is null on Player Controller");
        }
        return PlayerController.instance.narrativePanel;
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
                GetNarrativePanel().GetComponentInChildren<TextMeshProUGUI>().text = textToShow;
                timeStarted = Time.time;
                if (GetNarrativePanel().activeSelf) return;
                GetNarrativePanel().SetActive(true);
                GetNarrativePanel().transform.DOKill();
                GetNarrativePanel().GetComponent<CanvasGroup>().alpha = 0f;
                GetNarrativePanel().GetComponent<CanvasGroup>().DOFade(1f,fadeOutDuration);      
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
                GetNarrativePanel().transform.DOKill();
                GetNarrativePanel().SetActive(false);
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
            GetNarrativePanel().transform.DOKill();
            GetNarrativePanel().GetComponent<CanvasGroup>().alpha = 1f;
            GetNarrativePanel().GetComponent<CanvasGroup>().DOFade(0f,fadeOutDuration).OnComplete(()=> GetNarrativePanel().SetActive(false));      
            timeStarted = 0f;
            if (showOnlyOnce)
            {
                Destroy(gameObject);
                return;
            }
        }
    }
}
