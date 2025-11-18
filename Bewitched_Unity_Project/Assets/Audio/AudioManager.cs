using System.Collections;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

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
    //The UI Input System on the EventSystem object in the scene
    InputSystemUIInputModule UIInput;
    bool clickSubscribed = false;

    void Awake()
    {
        if (manager) throw new System.Exception("There are multiple audio managers in the scene!");
        else if (!refSheet) throw new System.Exception("Audio Manager refSheet not assigned!");
        manager = this;
        if (!EventSystem.current.gameObject.TryGetComponent<InputSystemUIInputModule>(out UIInput)) throw new System.Exception("Could not find InputSystemUIInputModule in scene");
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
        UnsubscribeCheckClick();
        RuntimeManager.GetBus("bus:/SoundEffects/InGame").setPaused(false);

    }

    /// <summary>
    /// Tries to get an FMOD event reference of the given name from the ref sheet.
    /// </summary>
    /// <param name="name">The name of the event to get</param>
    /// <param name="eventRef">out variable for the event reference if it was found</param>
    /// <returns>True if the event was found, false otherwise</returns>
    public static bool TryGetReference(string name, out EventReference eventRef)
    {
        if (!manager)
        {
            eventRef = new();
            return false;
        }
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
        if (!manager)
        {
            instance = new();
            return false;
        }
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
        if (!manager) return false;
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
        if (!manager) return;
        if (manager.activeSnapshots.ContainsKey("UIOpen")) return;
        //Subscribes CheckClick to suitable UI Actions
        SubscribeCheckClick();
        EventInstance inst = RuntimeManager.CreateInstance(manager.refSheet.snapshotRefs["UIOpen"]);
        inst.setParameterByName("UITransitionIn", transitionTime);
        inst.start();
        inst.release();
        manager.activeSnapshots["UIOpen"] = inst;
        manager.pauseCoroutine = manager.StartCoroutine(manager.DelayedPause(transitionTime));
    }
    /// <summary>
    /// Coroutine that waits the given amount of time before pausing all in-game sound effects
    /// </summary>
    /// <param name="wait">The time to wait</param>
    IEnumerator DelayedPause(float wait)
    {
        //Debug.LogError("TEST");
        yield return new WaitForSecondsRealtime(wait);
        RuntimeManager.GetBus("bus:/SoundEffects/InGame").setPaused(true);
    }
    /// <summary>
    /// Stops the UIOpen snapshot 
    /// </summary>
    /// <param name="transitionTime">The amount of time for music and sound effects to fade back in</param>
    public static void CloseUIAudio(float transitionTime = 0.8f)
    {
        if (!manager) return;
        if (!manager.activeSnapshots.ContainsKey("UIOpen")) return;
        if (manager.pauseCoroutine != null) manager.StopCoroutine(manager.pauseCoroutine);
        //Unsubscribe CheckClick from UI Actions
        UnsubscribeCheckClick();
        EventInstance inst = manager.activeSnapshots["UIOpen"];
        manager.activeSnapshots.Remove("UIOpen");
        inst.setParameterByName("UITransitionOut", transitionTime);
        inst.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        RuntimeManager.GetBus("bus:/SoundEffects/InGame").setPaused(false);
    }
    /// <summary>
    /// Changes a float parameter in this level's music event
    /// </summary>
    /// <param name="param">The name of the parameter</param>
    /// <param name="value">The value to change the parameter to</param>
    public static void ChangeMusicParameter(string param, float value)
    {
        if(manager)manager.levelMusic.setParameterByName(param, value);
    }
    /// <summary>
    /// Changed a label parameter in this level's music event
    /// </summary>
    /// <param name="param">The name of the parameter</param>
    /// <param name="value">The value to change the parameter to</param>
    public static void ChangeMusicParameter(string param, string value)
    {
        if(manager)manager.levelMusic.setParameterByNameWithLabel(param, value);
    }
    /// <summary>
    /// Forces Audio Manager to check for UI clicks outside of OpenUIAudio.
    /// This is mostly used in the main menu.
    /// </summary>
    public static void SubscribeCheckClick()
    {
        if (!manager) return;
        if(!manager.clickSubscribed){
            manager.UIInput.actionsAsset["UI/Submit"].performed += manager.CheckClick;
            manager.UIInput.actionsAsset["UI/Click"].canceled += manager.CheckClick;
            manager.clickSubscribed=true;
        }
    }
    /// <summary>
    /// Forces Audio Manager to unsubscribe from click and submit actions.
    /// </summary>
    public static void UnsubscribeCheckClick()
    {
        if (!manager) return;
        if(manager.clickSubscribed){
            manager.UIInput.actionsAsset["UI/Submit"].performed -= manager.CheckClick;
            manager.UIInput.actionsAsset["UI/Click"].canceled -= manager.CheckClick;
            manager.clickSubscribed=false;
        }
    }

    /// <summary>
    /// Checks the UI object that has been clicked or submitted on, if any, and plays the click sound effect
    /// </summary>
    /// <param name="context">Action context</param>
    void CheckClick(InputAction.CallbackContext context)
    {
        if (EventSystem.current.currentSelectedGameObject == null) return;
        if (EventSystem.current.currentSelectedGameObject.TryGetComponent<Button>(out var button) && !button.gameObject.CompareTag("NoClick"))
        {
            TryPlayOneShot("Click");
        }
    }
    /// <summary>
    /// Tries to play a snapshot of the given name
    /// </summary>
    /// <param name="snapshotName">The name of the snapshot to play</param>
    /// <returns>True if snapshot was successfully started, false otherwise</returns>
    public static bool TryPlaySnapshot(string snapshotName)
    {
        if (!manager) return false;
        if (manager.refSheet.snapshotRefs.TryGetValue(snapshotName, out EventReference evRef))
        {
            EventInstance ev = RuntimeManager.CreateInstance(evRef);
            if (!manager.activeSnapshots.TryAdd(snapshotName, ev))
            {
                Debug.LogError($"{snapshotName} is already active!");
                return false;
            }
            ev.start();
            ev.release();
            return true;
        }
        return false;
    }
    /// <summary>
    /// Stops the snapshot of the given name if it is playing
    /// </summary>
    /// <param name="snapshotName">The name of the snapshot</param>
    /// <param name="allowFadeout">Whether or not to allow fadeout, defaults to true</param>
    public static void StopSnapshot(string snapshotName,bool allowFadeout=true)
    {
        if (!manager) return;
        if (manager.activeSnapshots.TryGetValue(snapshotName, out EventInstance snapshot))
        {
            snapshot.stop(allowFadeout? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
            manager.activeSnapshots.Remove(snapshotName);
        }
        else
        {
            Debug.LogError("No Snapshot of the given name is playing!");
        }
    }
}


