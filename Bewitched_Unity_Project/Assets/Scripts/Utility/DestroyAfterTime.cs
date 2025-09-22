using System.Collections;
using UnityEngine;

/// <summary>
/// Automatically destroys the GameObject after a specified amount of time.
/// Useful for temporary effects such as particle systems.
/// </summary>
public class DestroyAfterTime : MonoBehaviour
{
    [SerializeField, Tooltip("The amount of time in seconds before the object is destroyed.")]
    private float timeToDestroy;

    private void Awake()
    {
        // Begin the timed destruction process
        StartCoroutine(WaitToDestroy());
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
