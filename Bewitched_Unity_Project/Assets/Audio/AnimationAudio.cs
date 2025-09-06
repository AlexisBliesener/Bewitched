using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

/// <summary>
/// This class is used to play any audio that needs to be called via Animation Events
/// </summary>
public class AnimationAudio : MonoBehaviour
{
    [SerializeField]EventReference walkEvent;
    EventInstance walk;

    public void StartWalk()
    {
        walk = RuntimeManager.CreateInstance(walkEvent);
        walk.start();
        walk.release();
    }

    public void StopWalk()
    {
        walk.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
    }
}
