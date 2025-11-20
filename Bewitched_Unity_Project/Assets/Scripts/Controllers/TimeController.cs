using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeController : MonoBehaviour
{
    // Singleton
    public static TimeController instance { get; protected set; }

    [Tooltip("The time change coroutine currently running")]
    private Coroutine timeChangeCoroutine = null;

    [Tooltip("If the game is paused or not")]
    private bool gamePaused = false;

    [Tooltip("The last known time scale before the pause")]
    private float previousTimeScale = 1;

    /// <summary>
    /// Determines if the game is paused
    /// </summary>
    public bool IsPaused => gamePaused;

    // Start is called before the first frame update
    void Awake()
    {
        instance = this;
    }

    /// <summary>
    /// Starts a time slow function, stopping any currently running routine
    /// </summary>
    /// <param name="timescale"> Time scale to set to </param>
    /// <param name="delayTime"> Duration of the time scale change </param>
    /// <param name="transitionTime"> Time it takes to transition </param>
    public void StartTimeSlow(float timescale, float delayTime, float transitionTime = 0.15f)
    {
        if (timeChangeCoroutine != null) StopCoroutine(timeChangeCoroutine);

        timeChangeCoroutine = StartCoroutine(HandleTimeChange(timescale, delayTime, transitionTime));
    }

    /// <summary>
    /// Handles the smoothing of the time slow down, speed up, and maintains the timescale
    /// </summary>
    /// <param name="timescale"> Time scale to set </param>
    /// <param name="delayTime"> Duration of time scale change </param>
    /// <param name="transitionTime"> Time it takes to transition</param>
    /// <returns></returns>
    public IEnumerator HandleTimeChange(float timescale, float delayTime, float transitionTime)
    {
        // Using unscaled deltatime for consistent lengths and pause handling
        float initialScale = Time.time;
        float timeStarted = 0;
        while (timeStarted < transitionTime)
        {
            if (!gamePaused)
            {
                Time.timeScale = Mathf.Lerp(initialScale, timescale, (Time.time - timeStarted) / transitionTime);
                timeStarted += Time.deltaTime;
            }
            yield return null;
        }

        timeStarted = 0;
        while (timeStarted < delayTime)
        {
            if (!gamePaused) timeStarted += Time.deltaTime;
            yield return null;
        }

        timeStarted = 0;
        while (timeStarted < transitionTime)
        {
            if (!gamePaused)
            {
                Time.timeScale = Mathf.Lerp(timescale, 1, (Time.time - timeStarted) / transitionTime);
                timeStarted += Time.deltaTime;
            }
            yield return null;
        }
        timeChangeCoroutine = null;
    }

    /// <summary>
    /// Pauses the game, handling options for 
    /// </summary>
    public void PauseGame()
    {
        gamePaused = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0;
    }

    /// <summary>
    /// Handles resuming the game
    /// </summary>
    public void ResumeGame()
    {
        if (gamePaused)
        {
            gamePaused = false;
            Time.timeScale = previousTimeScale;
        }
    }
}
