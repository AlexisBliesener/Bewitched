using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data structure for stage configuration
/// </summary>
[Serializable]
public class StageData
{
    [Header("Stage Configuration")]
    [Tooltip("Name of the stage")]  
    public string stageName;
    [Tooltip("If true, a random level from the list will be selected")]  
    public bool isRandomized;
    [Tooltip("List of level names associated with this stage")]
    public List<string> levels;
}