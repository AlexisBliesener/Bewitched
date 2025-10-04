using System.Collections;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    //Scriptable object containing a dictionary of FMODEvent References and their names.
    public EventRefsSO refSheet;
    //Singleton of this class
    public static AudioManager manager;

    [SerializeField, Tooltip("This scene's music")]
    EventReference levelMusicReference;
    EventInstance levelMusic;
    [Tooltip("Dictionary with the snapshots active during runtime as the value and the snapshot name as the key.")]
    Dictionary<string, EventInstance> activeSnapshots;
    Coroutine pauseCoroutine;

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
        activeSnapshots = new();
    }

    void OnDestroy()
    {
        if (levelMusic.isValid()) levelMusic.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        RuntimeManager.GetBus("bus:/SoundEffects/InGame").stopAllEvents(FMOD.Studio.STOP_MODE.IMMEDIATE);
        foreach (EventInstance inst in activeSnapshots.Values)
        {
            inst.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
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
    public static bool TryPlayInstance(string name, out EventInstance instance, bool release = true, GameObject spatializedSource = null)
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
    public static bool TryPlayOneShot(string name, GameObject spatializedSource = null)
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
    /// <summary>
    /// Ducks all non-UI audio in {transition time} seconds, max 1 second
    /// </summary>
    /// <param name="transitionTime">The amount of time it takes to fully transition into this snapshot</param>
    public static void OpenUIAudio(float transitionTime = 0.8f)
    {
        if (manager.activeSnapshots.ContainsKey("UIOpen")) return;
        EventInstance inst = RuntimeManager.CreateInstance(manager.refSheet.snapshotRefs["UIOpen"]);
        inst.setParameterByName("UITransitionIn", transitionTime);
        inst.start();
        inst.release();
        manager.activeSnapshots["UIOpen"] = inst;
        manager.pauseCoroutine = manager.StartCoroutine(manager.DelayedPause(transitionTime));
    }

    IEnumerator DelayedPause(float wait)
    {
        Debug.LogError("TEST");
        yield return new WaitForSecondsRealtime(wait);
        RuntimeManager.GetBus("bus:/SoundEffects/InGame").setPaused(true);
    }
    /// <summary>
    /// Stops the UIOpen snapshot 
    /// </summary>
    /// <param name="transitionTime">The amount of time for music and sound effects to fade back in</param>
    public static void CloseUIAudio(float transitionTime = 0.8f)
    {
        if (!manager.activeSnapshots.ContainsKey("UIOpen")) return;
        if (manager.pauseCoroutine != null) manager.StopCoroutine(manager.pauseCoroutine);
        EventInstance inst = manager.activeSnapshots["UIOpen"];
        manager.activeSnapshots.Remove("UIOpen");
        inst.setParameterByName("UITransitionOut", transitionTime);
        inst.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        RuntimeManager.GetBus("bus:/SoundEffects/InGame").setPaused(false);
    }

    public static void ChangeMusicParameter(string param, float value)
    {
        manager.levelMusic.setParameterByName(param, value);
    }
    public static void ChangeMusicParameter(string param, string value)
    {
        manager.levelMusic.setParameterByNameWithLabel(param, value);
    }
}


