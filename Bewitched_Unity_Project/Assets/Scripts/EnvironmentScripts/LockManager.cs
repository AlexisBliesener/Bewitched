using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;

public class LockManager : MonoBehaviour
{
    List<Lock> lockedDoors = new List<Lock>();

    public void AddToDoors(Lock door)
    {
        lockedDoors.Add(door);
    }

    public void RemoveFromDoors(Lock door)
    {
        lockedDoors.Remove(door);
    }

    public void IncrementKills()
    {
        foreach (Lock door in lockedDoors)
        {
            door.IncrementKill();
        }
    }
}
