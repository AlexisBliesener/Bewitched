using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class that allows me to serialize a sorted dictionary so I do not bake at runtime
/// </summary>
/// <typeparam name="TKey"> Key type </typeparam>
/// <typeparam name="TValue"> Value type </typeparam>
[System.Serializable]
public class SerializableDictionary<TKey, TValue> : SortedDictionary<TKey, TValue>, ISerializationCallbackReceiver
{
    [SerializeField] private List<TKey> keys = new List<TKey>();
    [SerializeField] private List<TValue> values = new List<TValue>();

    /// <summary>
    /// Handles pre-serialziation actions
    /// </summary>
    public void OnBeforeSerialize()
    {
        keys.Clear();
        values.Clear();
        foreach (var pair in this)
        {
            keys.Add(pair.Key);
            values.Add(pair.Value);
        }
    }

    /// <summary>
    /// Handles post deserialization actions
    /// </summary>
    public void OnAfterDeserialize()
    {
        Clear();
        for (int i = 0; i < keys.Count; i++)
        {
            if (!ContainsKey(keys[i]))
            {
                Add(keys[i], values[i]);
            }
        }
    }
}
