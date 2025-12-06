using System;
using System.Collections.Generic;
using DG.Tweening;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
/// <summary>
/// This class handles the narrative state popup, it will show the player a popup with a text when a certain state is triggered.
/// It will also handle the priority of the states, so that the higher priority states will override the lower priority states.
/// </summary>
public class NarrativeStatePopup : MonoBehaviour
{
    [Tooltip("Singleton instance of NarrativeStatePopup")]
    public static NarrativeStatePopup instance;
    [Tooltip("The different narrative states")]
    public enum NarrativeState
    {
        PlayerPossessionDraining,
        BreakWallActivated,
        OgrePossessionAvailable,
        NarrativeTrigger,
        RoomDoorsOpened,
        None
    }
    [Tooltip("Track the last used text index for each state to avoid repeating the same text")]
    private Dictionary<NarrativeState, int> lastUsedIndex = new Dictionary<NarrativeState, int>()
    {
        { NarrativeState.OgrePossessionAvailable, -1 },
        { NarrativeState.PlayerPossessionDraining, -1 },
        { NarrativeState.RoomDoorsOpened, -1 },
        { NarrativeState.BreakWallActivated, -1 },
    };

    [SerializeField, Tooltip("Th priority states, the first is the most important and it will override the others")]
    private List<NarrativeState> priorityStates = new List<NarrativeState>() { NarrativeState.PlayerPossessionDraining, NarrativeState.BreakWallActivated, NarrativeState.OgrePossessionAvailable, NarrativeState.NarrativeTrigger, NarrativeState.RoomDoorsOpened, NarrativeState.None};
    [SerializeField, Tooltip("States in this list will be queued if they trigger while a higher priority narrative is already active. They will automatically show once the high priority state finishes.")]
    private List<NarrativeState> statesAllowedToQueue = new List<NarrativeState>() {  NarrativeState.RoomDoorsOpened };
    [SerializeField, Tooltip("Player Possession Draining Texts"), TextArea(3, 10)]
    private List<string> playerPossessionDrainingTexts;
    [SerializeField, Tooltip("Ogre Possession Available Texts"), TextArea(3, 10)]
    private List<string> ogrePossessionAvailableTexts;
    [SerializeField, Tooltip("Room Doors Opened Texts"), TextArea(3, 10)]
    private List<string> roomDoorsOpenedTexts;
    [SerializeField, Tooltip("Break Wall Activated Texts"), TextArea(3, 10)]
    private List<string> breakWallActivatedTexts;
    [Tooltip("The current state")]
    private NarrativeState currentState = NarrativeState.None;
    [Tooltip("The time the last state started")]
    private float timeStarted = 0f;
    [SerializeField, Tooltip("How long the text will fade out"), Range(0.1f, 2f)]
    private float fadeOutDuration = 0.6f;
    [SerializeField, Tooltip("Delay before showing another text from the same state when triggered repeatedly"), Range(0f, 10f)]
    private float sameStateRepeatDelay = 5f;
    [Tooltip("The state that was requested to be shown on low priority")]
    private NarrativeState requestedStateOnLowPriority = NarrativeState.None;
    [Tooltip("The canvas group for the ui ")]
    private CanvasGroup canvasGroup;
    [Tooltip("The text mesh pro ui")]
    private TextMeshProUGUI textMeshProUGUI;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }


    private void Start()
    {
        if (PossessionAbility.instance != null)
        {
            PossessionAbility.CharacterControlChangeEvent += OnCharacterControlChange;
        }
        if (GetNarrativePanel() != null)
        {
            canvasGroup = GetNarrativePanel().GetComponent<CanvasGroup>();
            textMeshProUGUI = GetNarrativePanel().GetComponentInChildren<TextMeshProUGUI>();
        }
    }
    /// <summary>
    /// Get the narrative panel from player controller
    /// </summary>
    /// <returns>The narrative panel</returns>
    private GameObject GetNarrativePanel()
    {
        if (PlayerController.instance.narrativePanel == null)
        {
            Debug.LogWarning("Narrative panel is null on Player Controller");
        }
        return PlayerController.instance.narrativePanel;
    }

    /// <summary>
    /// A helper function to show the narrative panel with the given text
    /// </summary>
    /// <param name="text">The text to show</param>
    private void ShowNarrativePanel(String text)
    {
        GetNarrativePanel().transform.DOKill();
        if (GetNarrativePanel().activeSelf)
        {
            canvasGroup.DOKill();
            canvasGroup.DOFade(0f, fadeOutDuration).OnComplete(() =>
            {
                textMeshProUGUI.text = text;
                canvasGroup.DOFade(1f, fadeOutDuration);
            });
            return;
        }
        textMeshProUGUI.text = text;
        GetNarrativePanel().SetActive(true);
        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, fadeOutDuration);
    }

    /// <summary>
    /// Hides the narrative panel with the given state
    /// </summary>
    /// <param name="state">The state</param>
    public void HideNarrativePanel(NarrativeState state)
    {
        // hide only if the state is the same as the current state
        if (currentState != state && state != NarrativeState.None)
        {
            // remove it from the queue because it's not vaild anymore since the hide request was called.. 
            if (requestedStateOnLowPriority == state)
            {
                requestedStateOnLowPriority = NarrativeState.None;
            }
            return;
        }

        currentState = NarrativeState.None;
        GetNarrativePanel().transform.DOKill();
        canvasGroup.alpha = 1f;
        canvasGroup.DOFade(0f, fadeOutDuration).OnComplete(() =>
        {
            if (currentState == NarrativeState.None)
            {
                GetNarrativePanel().SetActive(false);
            }
            // Show a request that gets called when a higher priority state was triggered
            if (requestedStateOnLowPriority != NarrativeState.None && currentState == NarrativeState.None)
            {
                ShowNarrativePanel(requestedStateOnLowPriority);
                requestedStateOnLowPriority = NarrativeState.None;
            }
        });
        timeStarted = 0f;

    }

    /// <summary>
    /// Shows the narrative panel with the given state and text
    /// if the text was null, it will get a random text from the list of texts for the state
    /// </summary>
    /// <param name="state">The state</param>
    /// <param name="text">The text</param>
    public void ShowNarrativePanel(NarrativeState state, string text = null)
    {
        // if the state is the same and the time since the last state is less than the repeat delay
        if (currentState == state && ((timeStarted != 0f && Time.time - timeStarted < sameStateRepeatDelay) || (text == null && GetListByState(state).Count <= 1))) return;
        if (IsLowerPriority(state, currentState))
        {
            if (statesAllowedToQueue.Contains(state))
                requestedStateOnLowPriority = state;
            return;
        }
        currentState = state;
        timeStarted = Time.time;
        if (text == null)
        {
            ShowNarrativePanel(GetRandomText(state));
        }
        else
        {
            ShowNarrativePanel(text);
        }
    }
    /// <summary>
    /// Gets a random text from the list of texts for a given state 
    /// </summary>
    /// <param name="state">The state</param>
    /// <returns>A random text from the list of texts for the state that is not selected in the last time</returns>
    private string GetRandomText(NarrativeState state)
    {
        List<string> list = new List<string>(GetListByState(state));

        if (lastUsedIndex[state] != -1 && list.Count > 1)
            list.RemoveAt(lastUsedIndex[state]);

        string selectedText = list[UnityEngine.Random.Range(0, list.Count)];
        lastUsedIndex[state] = GetListByState(state).IndexOf(selectedText);
        return selectedText;
    }
    /// <summary>
    /// Gets the list of texts for a given state
    /// </summary>
    /// <param name="state">The state</param>
    /// <returns>The list of texts for the state</returns>
    private List<string> GetListByState(NarrativeState state)
    {
        switch (state)
        {
            case NarrativeState.OgrePossessionAvailable:
                return ogrePossessionAvailableTexts;
            case NarrativeState.PlayerPossessionDraining:
                return playerPossessionDrainingTexts;
            case NarrativeState.RoomDoorsOpened:
                return roomDoorsOpenedTexts;
            case NarrativeState.BreakWallActivated:
                return breakWallActivatedTexts;
            default:
                return null;
        }
    }
    /// <summary>
    /// An event that is triggered when the charachter is changed to a new character
    /// </summary>
    /// <param name="newCharacter">The new character</param>
    private void OnCharacterControlChange(Character newCharacter)
    {
        if (newCharacter == PlayerController.instance.oldHag && currentState == NarrativeState.PlayerPossessionDraining)
        {
            HideNarrativePanel(NarrativeState.PlayerPossessionDraining);
        }
    }
    /// <summary>
    /// Checks if the new state is lower priority than the current state
    /// </summary>
    /// <param name="newState">The new state</param>
    /// <param name="currentState">The current state</param>
    /// <returns>True if the new state is lower priority than the current state, false otherwise</returns>
    private bool IsLowerPriority(NarrativeState newState, NarrativeState currentState)
    {
        return priorityStates.IndexOf(newState) > priorityStates.IndexOf(currentState);
    }
}