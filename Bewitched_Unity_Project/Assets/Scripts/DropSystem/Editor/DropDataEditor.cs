#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(DropData))]
public class DropDataDrawer : PropertyDrawer
{
    private static DropSystem cachedDropSystem;
    private static string[] cachedRarityNames;
    private static int lastRarityCount = -1;

    /// <summary>
    /// This is override the default inspector GUI and draw the rarity dropdown in the editor 
    /// It will only show the rarity dropdown if the drop system has rarities
    /// </summary>
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), 
            property.isExpanded, label, true);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            
            float yPos = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // draw all properties except rarityIndex and dropScript so when we add a new property to DropData it will be automatically drawn!
            SerializedProperty prop = property.Copy();
            SerializedProperty endProperty = prop.GetEndProperty();
            // This is to draw the first property and then enter the children
            bool enterChildren = true;

            while (prop.NextVisible(enterChildren) && !SerializedProperty.EqualContents(prop, endProperty))
            {
                enterChildren = false; // Only enter children on first iteration
                
                if (prop.name == "rarityIndex" || prop.name == "dropScript")
                    continue;

                float propHeight = EditorGUI.GetPropertyHeight(prop);
                EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, propHeight), prop, true);
                yPos += propHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            // create a rarity dropdown which convert the index to a string and that's only in the editor
            SerializedProperty rarityIndexProp = property.FindPropertyRelative("rarityIndex");
            
            // cache the drop system and rarity name to avoid finding it every frame 
            if (cachedDropSystem == null)
            {
                cachedDropSystem = Object.FindObjectOfType<DropSystem>();
            }

            if (cachedDropSystem != null && cachedDropSystem.availableRarities.Count > 0)
            {
                cachedRarityNames = new string[cachedDropSystem.availableRarities.Count];
                for (int i = 0; i < cachedDropSystem.availableRarities.Count; i++)
                {
                    cachedRarityNames[i] = $"{cachedDropSystem.availableRarities[i].displayName} ({cachedDropSystem.availableRarities[i].dropChance}%)";
                }

                if (rarityIndexProp.intValue >= cachedRarityNames.Length)
                    rarityIndexProp.intValue = 0;

                // Create the dropdown with the rarity names and the index
                rarityIndexProp.intValue = EditorGUI.Popup(new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight),"Rarity", rarityIndexProp.intValue, cachedRarityNames);
            }else{
                EditorGUI.LabelField(new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight), "Rarity", "Add rarities first!");
            }


            yPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            SerializedProperty dropScriptProp = property.FindPropertyRelative("dropScript");
            float scriptHeight = EditorGUI.GetPropertyHeight(dropScriptProp);
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, scriptHeight), dropScriptProp);

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;

        float height = EditorGUIUtility.singleLineHeight; // if it was expanded then it will have a foldout
        
        // Calculate height for all properties automatically
        SerializedProperty prop = property.Copy();
        SerializedProperty endProperty = prop.GetEndProperty();
        bool enterChildren = true;

        while (prop.NextVisible(enterChildren) && !SerializedProperty.EqualContents(prop, endProperty))
        {
            enterChildren = false;
            if (prop.name == "rarityIndex")
            {
                height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }
            else if (prop.name == "dropScript")
            {
                height += EditorGUI.GetPropertyHeight(prop) + EditorGUIUtility.standardVerticalSpacing;
            }
            else
            {
                height += EditorGUI.GetPropertyHeight(prop) + EditorGUIUtility.standardVerticalSpacing;
            }
        }

        return height;
    }
}
#endif