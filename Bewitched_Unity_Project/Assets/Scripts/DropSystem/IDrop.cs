using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// IDrop is an interface for all upgrade drops
/// </summary>
public interface IDrop
{                
    int stackNum { get; set; }

    /// <summary>
    /// Activate the drop
    /// </summary>
    void Activate();
}    