#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

[CustomEditor(typeof(LevelManager))]
/// <summary>
/// Custom editor for LevelManager to save and load json automatically
/// Also to make it easier for designers to add/remove stages and levels
/// </summary>
public class LevelManagerEditor : Editor
{
    [Tooltip("Reference to LevelManager")]
    private LevelManager manager;

    private void OnEnable()
    {
        manager = (LevelManager)target;
        // Load JSON when inspector opens to avoid conflicts from json and inspector edits
        manager.LoadFromJson();
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck(); // This tracks if any changes were made in the inspector so we can save it to json immediately

        for (int s = 0; s < manager.levelData.stages.Count; s++)
        {
            StageData stage = manager.levelData.stages[s];
            EditorGUILayout.BeginVertical("box");

            string stageLabel = $"Stage {s + 1} - Name";
            stage.stageName = EditorGUILayout.TextField(stageLabel, stage.stageName);

            stage.isRandomized = EditorGUILayout.Toggle("Randomize Stage ?", stage.isRandomized);

            EditorGUILayout.LabelField("Levels:");

            for (int l = 0; l < stage.levels.Count; l++)
            {
                string currentLevel = stage.levels[l];
                // This is to get all scene names in build settings
                string[] allScenes = Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings)
                                               .Select(i =>
                                               {
                                                   string path = UnityEditor.EditorBuildSettings.scenes[i].path;
                                                   return System.IO.Path.GetFileNameWithoutExtension(path);
                                               }).ToArray();

                int currentIndex = Mathf.Max(0, System.Array.IndexOf(allScenes, currentLevel));
                int newIndex = EditorGUILayout.Popup("Level " + (l + 1), currentIndex, allScenes);
                stage.levels[l] = allScenes[newIndex];
                
            }

            // Buttons to add and remove levels (horizontal layout)
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Level"))
            {
                if (stage.levels == null)
                    stage.levels = new System.Collections.Generic.List<string>();

                string[] allScenes = Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings)
                                            .Select(i =>
                                            {
                                                string path = UnityEditor.EditorBuildSettings.scenes[i].path;
                                                return System.IO.Path.GetFileNameWithoutExtension(path);
                                            }).ToArray();

                // add the first scene by defualt 
                string defaultScene = allScenes.Length > 0 ? allScenes[0] : "";
                stage.levels.Add(defaultScene);
            }


            if (GUILayout.Button("Remove Last Level") && stage.levels.Count > 0)
                stage.levels.RemoveAt(stage.levels.Count - 1);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Remove Stage"))
            {
                manager.levelData.stages.RemoveAt(s);
                s--; // adjust index after removal
                continue;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        if (GUILayout.Button("Add Stage"))
        {
            StageData newStage = new StageData();

            newStage.levels = new List<string>();
            newStage.stageName = $"Stage {manager.levelData.stages.Count + 1}";
            manager.levelData.stages.Add(newStage);
        }

        // If any change was made, then save it to JSON
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(manager);
            manager.SaveToJson();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
