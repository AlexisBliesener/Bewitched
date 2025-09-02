using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SampleDoor : MonoBehaviour, IDoor
{
    // <summary>
    // This is a sample code to lock the door
    // it can be use later like for animation or other things
    // </summary>
    public void Lock()
    {
        gameObject.SetActive(true);
    }
    // <summary>
    // This is a sample code to unlock the door
    // it can be use later like for animation or other things
    // </summary>
    public void Unlock()
    {
        gameObject.SetActive(false);
    }
}
