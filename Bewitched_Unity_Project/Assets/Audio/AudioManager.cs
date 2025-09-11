using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
   //Scriptable object containing a dictionary of FMODEvent References and their names.
    public EventRefsSO refSheet;
    //Singleton of this class
    private static AudioManager manager;

    [SerializeField, Tooltip("This scene's music")]
    EventReference levelMusicReference;
    EventInstance levelMusic;

    void Awake()
    {
        if (manager) throw new System.Exception("There are multiple audio managers in the scene!");
        else if (!refSheet) throw new System.Exception("Audio Manager refSheet not assigned!");
        manager = this;
        if (!levelMusicReference.IsNull)
        {
            levelMusic = RuntimeManager.CreateInstance(levelMusicReference);
            levelMusic.start();
            levelMusic.release();
        }
        else Debug.LogError("The level music for this scene is not assigned in Audio Manager");
    }

    void OnDestroy()
    {
        if (levelMusic.isValid()) levelMusic.stop(FMOD.Studio.STOP_MODE.IMMEDIATE); 
    }

    /// <summary>
    /// Tries to get an FMOD event reference of the given name from the ref sheet.
    /// </summary>
    /// <param name="name">The name of the event to get</param>
    /// <param name="eventRef">out variable for the event reference if it was found</param>
    /// <returns>True if the event was found, false otherwise</returns>
    public static bool TryGetReference(string name, out EventReference eventRef)
    {
        return manager.refSheet.eventRefs.TryGetValue(name, out eventRef);
    }
    /// <summary>
    /// Tries to instantiate and play an FMOD Event of the given name.
    /// </summary>
    /// <param name="name">The name of the event reference to play</param>
    /// <param name="instance">out variable for the instantiated event</param>
    /// <param name="release">Whether the event should be released or not</param>
    /// <param name="spatializedSource">The source of this sound for spatialized sound effects</param>
    /// <returns>True if an event was successfully instantiated, false otherwise</returns>
    public static bool TryPlayInstance(string name, out EventInstance instance, bool release = true, GameObject spatializedSource=null)
    {
        if (manager.refSheet.eventRefs.TryGetValue(name, out EventReference evRef))
        {
            instance = RuntimeManager.CreateInstance(evRef);
            if (spatializedSource) RuntimeManager.AttachInstanceToGameObject(instance, spatializedSource);
            instance.start();
            if (release) instance.release();
            return true;
        }
        instance = new();
        return false;
    }
    /// <summary>
    /// Tries to play a one shot event of the given name
    /// </summary>
    /// <param name="name">the name of the event to play</param>
    /// <param name="spatializedSource">The source of this sound for spatialized sound effects</param>
    /// <returns>True if the event was instantiated and played, false otherwise</returns>
    public static bool TryPlayOneShot(string name, GameObject spatializedSource=null)
    {
        if (manager.refSheet.eventRefs.TryGetValue(name, out EventReference evRef))
        {
            EventInstance ev = RuntimeManager.CreateInstance(evRef);
            if (spatializedSource) RuntimeManager.AttachInstanceToGameObject(ev, spatializedSource);
            ev.start();
            ev.release();
            return true;
        }
        return false;
    }
}

