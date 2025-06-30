using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AYellowpaper.SerializedCollections;
using FMODUnity;

[CreateAssetMenu(fileName ="EventRefSheet",menuName ="New EventRefSheet")]
public class EventRefsSO : ScriptableObject
{
    public SerializedDictionary<string,EventReference> eventRefs;
    public SerializedDictionary<string,EventReference> snapshotRefs;
}
