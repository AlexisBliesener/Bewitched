using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using UnityEditor.EditorTools;

/// <summary>
/// This class is used to play any audio that needs to be called via Animation Events
/// </summary>
public class AnimationAudio : MonoBehaviour
{
    [SerializeField, Tooltip("The FMOD Event Reference for this character's walk cycle")]
    EventReference walkEvent;
    //The currently playing walk event (only really useful for any walk cycle that's looping and need to be stopped manually)
    EventInstance walk;

    void Awake()
    {
        if (walkEvent.IsNull) Debug.LogWarning($"The walk audio event of {transform.parent.gameObject.name} is not assigned.");
    }
    
    /// <summary>
    /// Plays the movement sound effect from the assigned walkEvent reference
    /// </summary>
    public void StartWalk()
    {
        if (walkEvent.IsNull) return;
        walk = RuntimeManager.CreateInstance(walkEvent);
        walk.start();
        walk.release();
    }
    /// <summary>
    /// Stops the current
    /// </summary>
    public void StopWalk()
    {
        if (!walk.isValid()) throw new System.Exception("There is no walk sound effect currently playing!");
        walk.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
    }
}
