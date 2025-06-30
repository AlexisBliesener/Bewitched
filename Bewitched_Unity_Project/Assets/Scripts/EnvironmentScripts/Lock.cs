using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lock : MonoBehaviour
{
    [Tooltip("The Number of Enemies to Kill Before the Door Opens")]
    public int threshold;

    private LockManager lockManager;

    private int numKilled = 0;

    void Start()
    {
        lockManager = GameObject.FindGameObjectWithTag("Lock Manager").GetComponent<LockManager>();
        lockManager.AddToDoors(this);
    }

    
    public void IncrementKill()
    {
        numKilled++;
        if (numKilled >= threshold)
        {
            // Using a coroutine so that lockManager can finish iterating through the list before removing the door
            StartCoroutine(OpenDoor()); 
        }
    }

    private IEnumerator OpenDoor()
    {
        yield return new WaitForSeconds(0.5f);

        lockManager.RemoveFromDoors(this);
        // In the future, start animation to open doors for handling opening
        // For now though we just destroy it
        Destroy(gameObject);
    }
}
