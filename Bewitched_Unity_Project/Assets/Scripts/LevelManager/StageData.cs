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
    [Tooltip("List of level names associated with this stage")]
    public List<string> levels;
}