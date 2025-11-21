using System.Collections;
using UnityEngine;

/// <summary>
/// Automatically destroys the GameObject after a specified amount of time.
/// Useful for temporary effects such as particle systems.
/// </summary>
public class DestroyAfterTime : MonoBehaviour
{
    [SerializeField, Tooltip("Wait till time is set to start destorying")]
    private bool waitTillSet = false;
    [SerializeField, Tooltip("The amount of time in seconds before the object is destroyed.")]
    private float timeToDestroy;
    [Tooltip("If time was set")]
    private bool setTime = false;

    /// <summary>
    /// Sets the time that it will take utill this object will be destroyed
    /// Effective immediately
    /// </summary>
    /// <param name="time"></param>
    public void SetTime(float time)
    {
        timeToDestroy = time;
        setTime = true;
    }

    private void Awake()
    {
        if (!waitTillSet)
        {
            // Begin the timed destruction process
            StartCoroutine(WaitToDestroy());
        }
    }

    private void Update()
    {
        if(waitTillSet && setTime)
        {
            setTime = false;
            StartCoroutine(WaitToDestroy());
        }
    } 

    /// <summary>
    /// Coroutine that waits for the given amount of time before destroying the GameObject.
    /// </summary>
    private IEnumerator WaitToDestroy()
    {
        yield return new WaitForSeconds(timeToDestroy);
        Destroy(this.gameObject);
    }
}
