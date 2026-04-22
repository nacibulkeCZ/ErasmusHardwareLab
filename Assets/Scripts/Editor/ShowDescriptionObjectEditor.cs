using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ShowDescriptionObject))]
[CanEditMultipleObjects]
public class ShowDescriptionObjectEditor : Editor
{
    private SerializedProperty objectNameProp;
    private SerializedProperty descriptionProp;
    private SerializedProperty objectImageProp;
    private SerializedProperty dubbingClipProp;

    private void OnEnable()
    {
        objectNameProp = serializedObject.FindProperty("objectName");
        descriptionProp = serializedObject.FindProperty("description");
        objectImageProp = serializedObject.FindProperty("objectImage");
        dubbingClipProp = serializedObject.FindProperty("dubbingClip");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // --- Custom Styles Definition ---
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            fixedHeight = 40,
            normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.7f, 0.9f, 1f) : new Color(0.1f, 0.3f, 0.6f) }
        };

        GUIStyle groupStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(15, 15, 15, 15),
            margin = new RectOffset(10, 10, 10, 15)
        };

        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            margin = new RectOffset(0, 0, 5, 10)
        };

        GUIStyle descriptionStyle = new GUIStyle(EditorStyles.textArea)
        {
            wordWrap = true,
            padding = new RectOffset(10, 10, 10, 10),
            fontSize = 12
        };

        // --- 1. Header Area ---
        EditorGUILayout.Space(10);
        GUILayout.Label("Item Description System", titleStyle);
        EditorGUILayout.Space(5);
        DrawLine();
        EditorGUILayout.Space(15);

        // --- 2. Main Content Group ---
        GUI.backgroundColor = new Color(0.85f, 0.95f, 1f);
        EditorGUILayout.BeginVertical(groupStyle);
        GUI.backgroundColor = Color.white;

        EditorGUILayout.LabelField("Primary Content", headerStyle);
        EditorGUILayout.Space(5);

        EditorGUILayout.PropertyField(objectNameProp, new GUIContent("Title", "The core name/title of this object."));

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Description", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        EditorGUI.showMixedValue = descriptionProp.hasMultipleDifferentValues;
        string newDescription = EditorGUILayout.TextArea(descriptionProp.stringValue, descriptionStyle, GUILayout.MinHeight(80));
        EditorGUI.showMixedValue = false;

        if (EditorGUI.EndChangeCheck())
        {
            descriptionProp.stringValue = newDescription;
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // --- 3. Media / Audio Group ---
        GUI.backgroundColor = new Color(0.95f, 0.85f, 1f);
        EditorGUILayout.BeginVertical(groupStyle);
        GUI.backgroundColor = Color.white;

        EditorGUILayout.LabelField("Media & Dubbing", headerStyle);
        EditorGUILayout.Space(5);

        EditorGUILayout.PropertyField(objectImageProp, new GUIContent("Object Image", "Optional material or icon."));
        EditorGUILayout.Space(10);
        EditorGUILayout.PropertyField(dubbingClipProp, new GUIContent("Voice Over Dubbing", "An audio clip containing the spoken description."));

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(15);
        DrawLine();
        EditorGUILayout.Space(10);

        // --- 4. Utilities ---
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);
        if (GUILayout.Button("Clear Fields", GUILayout.Width(120), GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Clear Fields", "Are you sure you want to clear all the description fields?", "Yes", "Cancel"))
            {
                objectNameProp.stringValue = string.Empty;
                descriptionProp.stringValue = string.Empty;
                objectImageProp.objectReferenceValue = null;
                dubbingClipProp.objectReferenceValue = null;
            }
        }
        GUI.backgroundColor = Color.white;

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawLine()
    {
        Rect rect = EditorGUILayout.GetControlRect(false, 2f);
        rect.height = 2f;
        EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin ? new Color(0.3f, 0.3f, 0.3f) : new Color(0.7f, 0.7f, 0.7f));
    }
}
