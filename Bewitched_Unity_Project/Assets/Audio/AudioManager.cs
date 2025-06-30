using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public EventRefsSO refSheet;
    private static EventRefsSO _refSheet;
    private static Dictionary<string,EventInstance> activeSnapshots = new();

    void Awake()
    {
        if(_refSheet) throw new System.Exception("There are multiple audio managers in the scene!");
        else if(!refSheet) throw new System.Exception("Audio Manager refSheet not assigned!");
        _refSheet = refSheet;
    }

    public static bool TryGetReference(string name, out EventReference eventRef){
        return _refSheet.eventRefs.TryGetValue(name,out eventRef);
    }

    public static bool TryPlayOneShot(string name){
        if(_refSheet.eventRefs.TryGetValue(name,out EventReference evRef)){
            EventInstance ev = RuntimeManager.CreateInstance(evRef);
            ev.start();
            ev.release();
            return true;
        }
        return false;
    }

    public static bool PlaySnapshot(string name){
        if(_refSheet.snapshotRefs.TryGetValue(name,out EventReference snapRef)){
            EventInstance snapshot = RuntimeManager.CreateInstance(snapRef);
            snapshot.start();
            activeSnapshots.Add(name,snapshot);
            return true;
        }
        return false;
    }
    public static bool StopSnapshot(string name){
        if(activeSnapshots.TryGetValue(name,out EventInstance snap)){
            activeSnapshots.Remove(name);
            snap.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            return true;
        }
        return false;
    }

    public static bool LinkSnapshot(EventInstance ev, string snapshotName){
        if(PlaySnapshot(snapshotName)){
            GCHandle handle = GCHandle.Alloc(snapshotName);
            ev.setUserData(GCHandle.ToIntPtr(handle));
            ev.setCallback(StopLinkedSnapshot,EVENT_CALLBACK_TYPE.DESTROYED);
            return true;
        }
        return false;
    }
    [AOT.MonoPInvokeCallback(typeof(EVENT_CALLBACK))]
    static FMOD.RESULT StopLinkedSnapshot(EVENT_CALLBACK_TYPE type, IntPtr evPtr,IntPtr paramPtr){
        EventInstance ev = new(evPtr);
        ev.getUserData(out IntPtr userdata);
        GCHandle handle = GCHandle.FromIntPtr(userdata);
        StopSnapshot(handle.Target as string);
        handle.Free();
        return FMOD.RESULT.OK;
    }
}
