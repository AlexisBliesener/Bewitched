using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data structure for level configuration
/// </summary>
[Serializable]
public class LevelData
{
    [Header("Level Configuration")]
    [Tooltip("List of stages in this level")]
    public List<StageData> stages = new List<StageData>();
}