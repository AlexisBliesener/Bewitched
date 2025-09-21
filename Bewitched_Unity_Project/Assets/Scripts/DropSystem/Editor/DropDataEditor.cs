#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(DropData))]
public class DropDataDrawer : PropertyDrawer
{
    /// <summary>
    /// This is override the default inspector GUI and draw the rarity dropdown in the editor 
    /// It will only show the rarity dropdown if the drop system has rarities
    /// </summary>
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        EditorGUILayout.BeginVertical(GUI.skin.box);


        // Redraw the default inspector
        EditorGUILayout.PropertyField(property.FindPropertyRelative("dropName"));
        EditorGUILayout.PropertyField(property.FindPropertyRelative("description"));
        EditorGUILayout.PropertyField(property.FindPropertyRelative("dropID"));
        EditorGUILayout.PropertyField(property.FindPropertyRelative("icon"));

        // create a rarity dropdown which convert the index to a string and that's only in the editor
        SerializedProperty rarityIndexProp = property.FindPropertyRelative("rarityIndex");
        DropSystem dropSystem = Object.FindObjectOfType<DropSystem>();

        if (dropSystem != null && dropSystem.availableRarities.Count > 0)
        {
            string[] rarityNames = new string[dropSystem.availableRarities.Count];
            for (int i = 0; i < dropSystem.availableRarities.Count; i++)
            {
                rarityNames[i] = $"{dropSystem.availableRarities[i].displayName} ({dropSystem.availableRarities[i].dropChance}%)";
            }

            if (rarityIndexProp.intValue >= rarityNames.Length)
                rarityIndexProp.intValue = 0;

            // Create the dropdown with the rarity names and the index
            rarityIndexProp.intValue = EditorGUILayout.Popup("Rarity", rarityIndexProp.intValue, rarityNames);
        }
        else
        {

            EditorGUILayout.LabelField("Rarity", "Add rarities first!");

        }



        EditorGUILayout.PropertyField(property.FindPropertyRelative("dropScript"));


        EditorGUILayout.EndVertical();
        EditorGUI.EndProperty();
    }
}
#endif
