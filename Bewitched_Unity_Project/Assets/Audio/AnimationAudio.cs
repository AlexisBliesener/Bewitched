using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System;
using System.Runtime.InteropServices;

/// <summary>
/// This class is used to play any audio that needs to be called via Animation Events
/// Every function call in animation events needs to pass it's arguments as an Animation Event
/// </summary>
public class AnimationAudio : MonoBehaviour
{
    /*
    This dictionary stores prepared or currently playing event instances.
    Because of how Animation Events work, each of this game object's animation clips
    will only be able to play one sound effect at a time.
    Key: the name of the animation clip that started the sound effect
    Value: The sound effect event instance 
    */
    Dictionary<string, EventInstance> animEvents;
    [Tooltip("Previews the fmod events currently playing on this script")]
    List<string> eventsPlaying;

    void Awake()
    {
        animEvents = new();
        eventsPlaying = new();
    }

    /// <summary>
    /// Instantiates an event of the given name but doesn't start or release it. This is for if initial parameters need to be set first.
    /// </summary>
    /// <param name="anim">The animation event that called this. STRING PARAMETER: The name of the event.</param>
    public void PrepareEvent(AnimationEvent anim)
    {
        string clipName = anim.animatorClipInfo.clip.name;
        if (AudioManager.TryGetReference(anim.stringParameter, out EventReference evRef))
        {
            if (animEvents.ContainsKey(clipName)) animEvents[clipName].stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            animEvents[clipName] = RuntimeManager.CreateInstance(evRef);
            RegisterDestroyCallback(animEvents[clipName], clipName);

        }
        else Debug.LogError($"Failed to prepare an event of this clipName: {clipName}");
    }

    /// <summary>
    /// Starts and releases the event the given animation event prepared.
    /// </summary>
    /// <param name="anim">The animation event to start the prepared event of</param>
    public void StartPreparedEvent(AnimationEvent anim)
    {
        string clipName = anim.animatorClipInfo.clip.name;
        if (!animEvents.ContainsKey(clipName))
        {
            Debug.LogError($"No Animation Audio Event Prepared From {clipName}!");
            return;
        }
        animEvents[clipName].getPlaybackState(out PLAYBACK_STATE state);
        if (state == PLAYBACK_STATE.STOPPED)
        {
            animEvents[clipName].start();
            animEvents[clipName].release();
        }
        else Debug.LogError($"Event from {clipName} already playing!");
    }

    /// <summary>
    /// Prepares, starts, and releases an event.
    /// </summary>
    /// <param name="anim">The animation event that called this. STRING: Name of the event to start</param>
    public void StartEvent(AnimationEvent anim)
    {
        string clipName = anim.animatorClipInfo.clip.name;
        if (AudioManager.TryGetReference(anim.stringParameter, out EventReference evRef))
        {
            if (animEvents.ContainsKey(clipName))
            {
                animEvents[clipName].stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                if (animEvents[clipName].isValid()) animEvents[clipName].release();
            }
            EventInstance ev = RuntimeManager.CreateInstance(evRef);
            ev.start();
            ev.release();
            animEvents[clipName] = ev;
            RegisterDestroyCallback(animEvents[clipName], clipName);
        }
        else Debug.LogError($"Failed to prepare an event of this clipName: {anim.stringParameter} for {clipName}");
    }
    /// <summary>
    /// Sets a parameter 
    /// </summary>
    /// <param name="anim"></param>
    public void SetEventParam(AnimationEvent anim)
    {
        string clipName = anim.animatorClipInfo.clip.name;
        if (!animEvents.ContainsKey(clipName))
        {
            Debug.LogError($"No Animation Audio Event prepared or playing for {clipName}!");
            return;
        }
        if (animEvents[clipName].setParameterByName(anim.stringParameter, anim.floatParameter) != FMOD.RESULT.OK)
        {
            Debug.LogError($"{animEvents[clipName]} does not have parameter {anim.stringParameter}!");
        }
    }

    public void StartOneShot(string clipName)
    {
        AudioManager.TryPlayOneShot(clipName);
    }

    public void StopEvent(AnimationEvent anim)
    {
        string clipName = anim.animatorClipInfo.clip.name;
        if (animEvents.ContainsKey(clipName)) animEvents[clipName].stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        else Debug.LogError($"No event from {clipName} currently playing!");
    }

    void RegisterDestroyCallback(EventInstance ev, string clipName)
    {
        GCHandle handle = GCHandle.Alloc(clipName);
        ev.setUserData(GCHandle.ToIntPtr(handle));
        ev.setCallback(AnimationEventDestroyCallback, EVENT_CALLBACK_TYPE.DESTROYED);
    }

    [AOT.MonoPInvokeCallback(typeof(EVENT_CALLBACK))]
    FMOD.RESULT AnimationEventDestroyCallback(EVENT_CALLBACK_TYPE type, IntPtr instancePtr, IntPtr paramPtr)
    {
        EventInstance ev = new(instancePtr);
        ev.getUserData(out IntPtr userData);
        GCHandle handle = GCHandle.FromIntPtr(userData);
        string clipName = handle.Target.ToString();
        if (animEvents.ContainsKey(clipName))
        {
            animEvents.Remove(clipName);
        }
        handle.Free();
        return FMOD.RESULT.OK;
    }
}