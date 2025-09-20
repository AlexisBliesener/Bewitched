using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Priority queue class for iterating through objects in specific orders
/// </summary>
/// <typeparam name="T"> Object type held in queue </typeparam>
public class PriorityQueue<T>
{
    [Tooltip("Priority queue dictionary")]
    private SortedDictionary<int, List<T>> priorityQueue;

    private int count;

    /// <summary>
    /// Constructor
    /// </summary>
    public PriorityQueue()
    {
        priorityQueue = new SortedDictionary<int, List<T>>();
        count = 0;
    }

    /// <summary>
    /// Add an item to the queue
    /// </summary>
    /// <param name="item"> Item in the queue </param>
    /// <param name="cost"> Priority of item </param>
    public void Enqueue(T item, int cost)
    {
        if (!priorityQueue.ContainsKey(cost))
        {
            priorityQueue[cost] = new List<T>();
        }
        priorityQueue[cost].Add(item);
        count++;
    }

    /// <summary>
    /// Checks if the queue is empty
    /// </summary>
    /// <returns> True if empty </returns>
    public bool IsEmpty()
    {
        if (priorityQueue.Count == 0) return true;
        return false;
    }

    /// <summary>
    /// Dequeue an item from the queue
    /// </summary>
    /// <returns> Dequeued item </returns>
    public T Dequeue()
    {
        if (IsEmpty()) return default(T);

        int lowest = priorityQueue.Keys.First();
        T item = priorityQueue[lowest][0];
        priorityQueue[lowest].RemoveAt(0);

        if (priorityQueue[lowest].Count == 0)
        {
            priorityQueue.Remove(lowest);
        }

        count--;
        return item;
    }

    /// <summary>
    /// Checks if an item exists
    /// </summary>
    /// <param name="item"> Item looking for </param>
    /// <param name="f"> F value of node </param>
    /// <returns> True if node exists </returns>
    public bool Contains(T item, int f)
    {
        if (priorityQueue.ContainsKey(f))
        {
            if (priorityQueue[f].Contains(item))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Replaces a item in the priority queue based on a new priority
    /// </summary>
    /// <param name="item"> Item to replace </param>
    /// <param name="oldVal"> Old value </param>
    /// <param name="newVal"> New value </param>
    public void Replace(T item, int oldVal, int newVal)
    {
        priorityQueue[oldVal].Remove(item);
        if (priorityQueue[oldVal].Count == 0)
        {
            priorityQueue.Remove(oldVal);
        }

        Enqueue(item, newVal);
    }

    /// <summary>
    /// Gets the count of the priority queue
    /// </summary>
    public int Count => count;
}
