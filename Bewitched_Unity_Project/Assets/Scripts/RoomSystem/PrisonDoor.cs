using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrisonDoor : MonoBehaviour, IDoor
{
    MeshRenderer render;
    Collider coll;

    void Awake()
    {
        render = GetComponent<MeshRenderer>();
        coll = GetComponent<Collider>();
    }

    // <summary>
    // This is a sample code to lock the door
    // it can be use later like for animation or other things
    // </summary>
    public void Lock()
    {
        gameObject.SetActive(true);
        //Level 1 Door audio implementation
        AudioManager.TryPlayOneShot("PrisonDoorClose", gameObject);
    }
    // <summary>
    // This is a sample code to unlock the door
    // it can be use later like for animation or other things
    // </summary>
    public void Unlock()
    {
        //kept the whole object active because the door sound is spatialized and needs the object to be active.
        render.enabled = false;
        coll.enabled = false;
        //Level 1 Door audio implementation
        AudioManager.TryPlayOneShot("PrisonDoorOpen",gameObject);
    }
}
