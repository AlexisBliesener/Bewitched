using System.Collections;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

/// <summary>
/// Controls a prison door with simple lock/unlock animations and sounds.
/// </summary>
public class PrisonDoor : MonoBehaviour, IDoor
{
    [SerializeField, Tooltip("The object that holds the visible door model.")]
    private GameObject doorModel;

    [Tooltip("The collider used to block passage when the door is locked.")]
    private BoxCollider boxCollider;
    [Tooltip("The total distance for unlocked door position")]
    private Vector3 movePos = new Vector3(0, -4.5f, 0);
    [Tooltip("Time (in seconds) for the door to move when unlocking.")]
    private const float UNLOCK_DURATION = 0.5f;
    [Tooltip("Time (in seconds) for the door to move when locking.")]
    private const float LOCK_DURATION = 0.5f;
    [SerializeField] private EventReference doorOpen;
    [SerializeField] private EventReference doorClose;

    [Tooltip("if the door is locked or not, this used to prevent multiple lock/unlock calls")]
    private bool isLocked = false;
    [Tooltip("The position of the door when it's locked")]
    private Vector3 lockedPos;
    [Tooltip("The position of the door when it's unlocked")]
    private Vector3 unlockedPos;
    private void Start()
    {
        // Automatically find the BoxCollider if not set
        if (boxCollider == null)
            boxCollider = GetComponentInChildren<BoxCollider>();

        unlockedPos = doorModel.transform.position;
        lockedPos = doorModel.transform.position + movePos;
    }
    /// <summary>
    /// Locks the door (enables collider, plays animation, and sound).
    /// </summary>
    public void Lock()
    {
        if (isLocked) return;
        StartCoroutine(MoveDoor(true, LOCK_DURATION, doorClose));
        isLocked = true;
    }

    /// <summary>
    /// Unlocks the door (disables collider halfway, plays animation, and sound).
    /// </summary>
    public void Unlock()
    {
        if (!isLocked) return;
        StartCoroutine(MoveDoor(false, UNLOCK_DURATION, doorOpen, disableColliderHalfway: true));
        isLocked = false;
    }

    /// <summary>
    /// Coroutine that moves the door step by step.
    /// </summary>
    private IEnumerator MoveDoor(bool isLocking, float doorDuration, EventReference sound, bool disableColliderHalfway = false)
    {
        // Play sound
        if(!sound.IsNull)
        {
            RuntimeManager.PlayOneShotAttached(sound,gameObject);
        }
        else
        {
            Debug.LogWarning("Audio manager does not exisit!");
        }
        if (isLocking)
        {
            boxCollider.enabled = true;
        }
        float timeElapsed = 0f;
        Vector3 startPos = doorModel.transform.position;
        Vector3 endPos = isLocking ? lockedPos : unlockedPos;

        while (timeElapsed < 1f)
        {
            timeElapsed += Time.deltaTime / doorDuration;

            doorModel.transform.position = Vector3.Lerp(startPos, endPos, timeElapsed);

            // Disable collider halfway through if unlocking
            if (disableColliderHalfway && timeElapsed >= 0.5f)
                boxCollider.enabled = false;

            yield return null;
        }
    }
}

