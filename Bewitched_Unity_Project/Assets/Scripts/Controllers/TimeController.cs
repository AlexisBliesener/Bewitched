using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeController : MonoBehaviour
{
    // Singleton
    public static TimeController instance { get; protected set; }

    [Tooltip("Time change lerp time")]
    public float timeLerpDuration = 0.5f;

    [Tooltip("The time change coroutine currently running")]
    private Coroutine timeChangeCoroutine = null;

    [Tooltip("If the game is paused or not")]
    private bool gamePaused = false;

    [Tooltip("The last known time scale before the pause")]

    // Start is called before the first frame update
    void Awake()
    {
        instance = this;
    }

    /// <summary>
    /// Returns true if time is normal
    /// </summary>
    public bool IsNormalTime => Time.timeScale == 1;

    /// <summary>
    /// Starts a time slow function, stopping any currently running routine
    /// </summary>
    /// <param name="timescale"> Time scale to set to </param>
    /// <param name="delayTime"> Duration of the time scale change </param>
    public void StartTimeSlow(float timescale, float delayTime)
    {
        if (timeChangeCoroutine != null) StopCoroutine(timeChangeCoroutine);

        timeChangeCoroutine = StartCoroutine(HandleTimeChange(timescale, delayTime));
    }

    /// <summary>
    /// Handles the smoothing of the time slow down, speed up, and maintains the timescale
    /// </summary>
    /// <param name="timescale"> Time scale to set </param>
    /// <param name="delayTime"> Duration of time scale change </param>
    /// <returns></returns>
    public IEnumerator HandleTimeChange(float timescale, float delayTime)
    {
        // Using unscaled deltatime for consistent lengths and pause handling
        float initialScale = Time.time;
        float timeStarted = 0;
        while (timeStarted < timeLerpDuration)
        {
            if (!gamePaused)
            {
                Time.timeScale = Mathf.Lerp(initialScale, timescale, (Time.time - timeStarted) / timeLerpDuration);
                timeStarted += Time.unscaledDeltaTime;
            }
            yield return null;
        }

        timeStarted = 0;
        while (timeStarted < delayTime)
        {
            if (!gamePaused) timeStarted += Time.unscaledDeltaTime;
            yield return null;
        }

        timeStarted = 0;
        while (timeStarted < timeLerpDuration)
        {
            if (!gamePaused)
            {
                Time.timeScale = Mathf.Lerp(timescale, 1, (Time.time - timeStarted) / timeLerpDuration);
                timeStarted += Time.unscaledDeltaTime;
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
        Time.timeScale = 0;
    }

    /// <summary>
    /// Handles resuming the game
    /// </summary>
    public void ResumeGame()
    {
        gamePaused = false;
        if (timeChangeCoroutine == null) Time.timeScale = 1;
    }
}
