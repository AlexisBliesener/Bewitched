using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System;
using System.Runtime.InteropServices;
using NaughtyAttributes;



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
    [SerializeField,ReadOnly] List<string> eventsPlaying;
    [SerializeField, Tooltip("Reference to the character script controlling this character")]
    Character character;
    [Tooltip("Whether this enemy is an event enemy")]
    [SerializeField,ReadOnly] bool isEventEnemy = false;
    //Property for whether or not the character is possessed or not
    bool possessed { get { return (character is Enemy) && (character as Enemy).IsPlayerControlling(); } }
    EVENT_CALLBACK destroyCallback;
    
    /// <summary>
    /// Class used to pass the dictionary key and class instance to the static callback method
    /// </summary>
    private class EntryData
    {
        public string key;
        public AnimationAudio instance;
        public EntryData(string str, AnimationAudio inst)
        {
            key = str;
            instance = inst;
        }
    }

    void Start()
    {
        destroyCallback = new EVENT_CALLBACK(AnimationEventDestroyCallback);
        animEvents = new();
        eventsPlaying = new();
        if (!character)
        {
            if (!transform.parent.TryGetComponent(out character))
            {
                Debug.LogError("Animation audio could not find this character's character script");
                return;
            }
        }
        isEventEnemy = character.TryGetComponent<EventEnemy>(out var e);
        character.health.OnDeath += OnDeath;
    }

    void OnDestroy()
    {
        character.health.OnDeath -= OnDeath;
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
        EventInstance ev = animEvents[clipName];
        ev.getPlaybackState(out PLAYBACK_STATE state);
        if (state == PLAYBACK_STATE.STOPPED)
        {
            RegisterDestroyCallback(ev, clipName);
            if (possessed) ev.setParameterByNameWithLabel("Possessed", "True");
            if (isEventEnemy) ev.setParameterByNameWithLabel("Event", "True");
            if (anim.intParameter == 1) RuntimeManager.AttachInstanceToGameObject(ev, character.gameObject);
            ev.start();
            ev.release();
            eventsPlaying.Add(GetPath(ev));
        }
        else Debug.LogError($"Event from {clipName} already playing!");
    }

    /// <summary>
    /// Prepares, starts, and releases an event.
    /// </summary>
    /// <param name="anim">The animation event that called this. STRING: Name of the event to start. INT: Spatialized or not (0 is false)</param>
    public void StartEvent(AnimationEvent anim)
    {
        string clipName = anim.animatorClipInfo.clip.name;
        if (AudioManager.TryGetReference(anim.stringParameter, out EventReference evRef))
        {
            if (animEvents.ContainsKey(clipName))
            {
                animEvents[clipName].stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            }
            EventInstance ev = RuntimeManager.CreateInstance(evRef);
            eventsPlaying.Add(GetPath(ev));
            if (possessed) ev.setParameterByNameWithLabel("Possessed", "True");
            if (isEventEnemy) ev.setParameterByNameWithLabel("Event", "True");
            if (anim.intParameter == 1) RuntimeManager.AttachInstanceToGameObject(ev, character.gameObject);
            ev.start();
            ev.release();
            animEvents[clipName] = ev;
            RegisterDestroyCallback(animEvents[clipName], clipName);
        }
        else Debug.LogError($"Failed to prepare an event of this clipName: {anim.stringParameter} for {clipName}");
    }
    /// <summary>
    /// Sets a parameter on the fmod event associated with this animation event
    /// </summary>
    /// <param name="anim">The animation event who called this. STRING: Parameter name. FLOAT: Parameter value.</param>
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

    /// <summary>
    /// Starts an fmod event of the given name that doesn't need to be tracked.
    /// </summary>
    /// <param name="anim">Animation Event. STRING: Event name. INT: Attatched or not</param>
    public void StartOneShot(AnimationEvent anim)
    {
        AudioManager.TryPlayInstance(anim.stringParameter, out EventInstance ev, true, (anim.intParameter == 1) ? character.gameObject : null);
        if (possessed) ev.setParameterByNameWithLabel("Possessed", "True");
        if (isEventEnemy) ev.setParameterByNameWithLabel("Event", "True");
    }

    /// <summary>
    /// Plays a one shot only when the character is possessed
    /// </summary>
    /// <param name="anim">Animation Event. STRING: Event name. INT: Attatched or not</param>
    public void StartOneShotOnPossess(AnimationEvent anim)
    {
        if (!possessed) return;
        StartOneShot(anim);
    }
    /// <summary>
    /// Stops a playing 
    /// </summary>
    /// <param name="anim"></param>
    public void StopEvent(AnimationEvent anim)
    {
        string clipName = anim.animatorClipInfo.clip.name;
        if (animEvents.ContainsKey(clipName)) animEvents[clipName].stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        else Debug.LogError($"No event from {clipName} currently playing!");
    }

    /// <summary>
    /// Gets the path of the given event instance
    /// </summary>
    /// <param name="ev">The event instance</param>
    /// <returns>The path of the event</returns>
    public static string GetPath(EventInstance ev)
    {
        if (!ev.isValid())
        {
            Debug.Log("Given event instance is invalid!");
            return null;
        }
        ev.getDescription(out EventDescription desc);
        desc.getPath(out string path);
        return path;
    }

    /// <summary>
    /// Registers destroy callback on the passed event instance and stores the animation clip name that started this event as user data.
    /// </summary>
    /// <param name="ev">The event instance to register the callback for</param>
    /// <param name="clipName">The animation clip name that started the event</param>
    void RegisterDestroyCallback(EventInstance ev, string clipName)
    {

        if (!ev.isValid())
        {
            Debug.LogWarning("Event instance is not valid!");
            return;
        }
        GCHandle handle = GCHandle.Alloc(new EntryData(clipName,this));
        ev.setUserData(GCHandle.ToIntPtr(handle));
        ev.setCallback(destroyCallback, EVENT_CALLBACK_TYPE.DESTROYED);
    }

    /// <summary>
    /// Moves an event called from another animation clip to the animation clip that calls this
    /// </summary>
    /// <param name="currentAnim">Animation Event. STRING: Clip name to take event from.</param>
    void MoveEvent(AnimationEvent currentAnim)
    {
        string key = currentAnim.stringParameter;
        string clipName = currentAnim.animatorClipInfo.clip.name;
        if (key == clipName) return;
        if (animEvents.ContainsKey(clipName)) throw new ArgumentException("An event is already playing from this clip!");
        if (!animEvents.ContainsKey(key)) throw new ArgumentException("No event from the given animation clip name was found.");

        EventInstance ev = animEvents[key];
        //If the event already has user data, update it
        if (ev.getUserData(out IntPtr ptr) == FMOD.RESULT.OK)
        {
            //For debuging purposes mostly
            if (ptr == IntPtr.Zero)
            {
                Debug.LogError("Event has no user data but is in user data block");
                return;
            }
            //Change the object pinned at this memory address to be the new clipName
            GCHandle handle = GCHandle.FromIntPtr(ptr);
            (handle.Target as EntryData).key = clipName;

        }
        //If no user data, the event has been prepared but not started, don't set user data yet
        animEvents[clipName] = ev;
        animEvents.Remove(key);
    }
    /// <summary>
    /// Function used to stop all animation audio sound effects when the character dies
    /// </summary>
    void OnDeath(GameObject enemyGameObject)
    {
        foreach (var ev in animEvents.Values)
        {
            ev.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        }
        //When death animations are implemented, exclude death sound effects.
    }


    /// <summary>
    /// Callback that removes the event from the animEvents dictionary when it gets destroyed.
    /// </summary>
    /// <param name="type">The callback type (will always be DESTROYED)</param>
    /// <param name="instancePtr">The pointer to the destroyed event instance</param>
    /// <param name="paramPtr">For parameter-related callbacks, not relavant to this callback</param>
    /// <returns></returns>
    [AOT.MonoPInvokeCallback(typeof(EVENT_CALLBACK))]
    static FMOD.RESULT AnimationEventDestroyCallback(EVENT_CALLBACK_TYPE type, IntPtr instancePtr, IntPtr paramPtr)
    {
        EventInstance ev = new(instancePtr);
        ev.getUserData(out IntPtr userData);
        if (userData == IntPtr.Zero)
        {
            Debug.LogError("AnimationEventDestroyCallback called on event with no user data!");
            return FMOD.RESULT.ERR_INVALID_HANDLE;
        }
        GCHandle handle = GCHandle.FromIntPtr(userData);
        EntryData data = handle.Target as EntryData;
        if (data.instance != null)
        {
            if (data.instance.animEvents.ContainsKey(data.key) && data.instance.animEvents[data.key].Equals(ev))
            {
                data.instance.animEvents.Remove(data.key);
            }
            string path = GetPath(ev);
            if (data.instance.eventsPlaying.Contains(path)) data.instance.eventsPlaying.Remove(path);
        }
        handle.Free();
        return FMOD.RESULT.OK;
    }
}