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
    [Tooltip("The distance moved per animation step.")]
    private Vector3 moveStep = new Vector3(0, -0.09f, 0);
    [Tooltip("The number of steps the door moves when fully locking/unlocking.")]
    private const int TOTAL_STEPS = 50;
    [Tooltip("Time (in seconds) between each movement step.")]
    private const float STEP_DELAY = 0.01f;
    [SerializeField] private EventReference doorOpen;
    [SerializeField] private EventReference doorClose;

    [Tooltip("if the door is locked or not, this used to prevent multiple lock/unlock calls")]
    private bool isLocked = false;
    private void Start()
    {
        // Automatically find the BoxCollider if not set
        if (boxCollider == null)
            boxCollider = GetComponentInChildren<BoxCollider>();
    }

    /// <summary>
    /// Locks the door (enables collider, plays animation, and sound).
    /// </summary>
    public void Lock()
    {
        if (isLocked) return;
        StartCoroutine(MoveDoor(moveStep, TOTAL_STEPS, doorClose));
        isLocked = true;
    }

    /// <summary>
    /// Unlocks the door (disables collider halfway, plays animation, and sound).
    /// </summary>
    public void Unlock()
    {
        if (!isLocked) return;
        StartCoroutine(MoveDoor(-moveStep, TOTAL_STEPS, doorOpen, disableColliderHalfway: true));
        isLocked = false;
    }

    /// <summary>
    /// Coroutine that moves the door step by step.
    /// </summary>
    private IEnumerator MoveDoor(Vector3 step, int steps, EventReference sound, bool disableColliderHalfway = false)
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
        if (!disableColliderHalfway)
        {
            boxCollider.enabled = true;
        }
        for (int i = 0; i < steps; i++)
        {
            doorModel.transform.position += step;

            // Disable collider halfway through if unlocking
            if (disableColliderHalfway && i == steps / 2)
                boxCollider.enabled = false;
            else if (!disableColliderHalfway && i == steps / 2)
                boxCollider.enabled = true;

            yield return new WaitForSeconds(STEP_DELAY);
        }
    }
}

