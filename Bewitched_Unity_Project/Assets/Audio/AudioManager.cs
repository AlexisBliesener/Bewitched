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
    //Backing singleton version of the refSheet field for static functions
    private static EventRefsSO _refSheet;

    [SerializeField,Tooltip("This scene's music")]
    EventReference levelMusicReference;
    EventInstance levelMusic;

    void Awake()
    {
        if (_refSheet) throw new System.Exception("There are multiple audio managers in the scene!");
        else if (!refSheet) throw new System.Exception("Audio Manager refSheet not assigned!");
        _refSheet = refSheet;
        if (!levelMusicReference.IsNull)
        {
            levelMusic = RuntimeManager.CreateInstance(levelMusicReference);
            levelMusic.start();
            levelMusic.release();
        }
    }
    
    /// <summary>
    /// Tries to get an FMOD event reference of the given name from the ref sheet.
    /// </summary>
    /// <param name="name">The name of the event to get</param>
    /// <param name="eventRef">out variable for the event reference if it was found</param>
    /// <returns>True if the event was found, false otherwise</returns>
    public static bool TryGetReference(string name, out EventReference eventRef)
    {
        return _refSheet.eventRefs.TryGetValue(name, out eventRef);
    }
    /// <summary>
    /// Tries to instantiate and play an FMOD Event of the given name.
    /// </summary>
    /// <param name="name">The name of the event reference to play</param>
    /// <param name="instance">out variable for the instantiated event</param>
    /// <param name="release">Whether the event should be released or not</param>
    /// <returns>True if an event was successfully instantiated, false otherwise</returns>
    public static bool TryPlayInstance(string name, out EventInstance instance, bool release = true)
    {
        if (_refSheet.eventRefs.TryGetValue(name, out EventReference evRef))
        {
            instance = RuntimeManager.CreateInstance(evRef);
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
    /// <returns>True if the event was instantiated and played, false otherwise</returns>
    public static bool TryPlayOneShot(string name)
    {
        if (_refSheet.eventRefs.TryGetValue(name, out EventReference evRef))
        {
            EventInstance ev = RuntimeManager.CreateInstance(evRef);
            ev.start();
            ev.release();
            return true;
        }
        return false;
    }
}
