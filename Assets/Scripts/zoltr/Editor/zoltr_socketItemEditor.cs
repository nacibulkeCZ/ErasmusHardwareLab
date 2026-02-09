using UnityEditor;

[CustomEditor(typeof(zoltr_socket))]
[CanEditMultipleObjects]
public class zoltr_socketEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty socketTypeProp = serializedObject.FindProperty("socket_type");
        SerializedProperty snapAxisProp = serializedObject.FindProperty("snapAxis");
        SerializedProperty snapOffsetProp = serializedObject.FindProperty("snapOffset");
        SerializedProperty snapRotationProp = serializedObject.FindProperty("snapRotation");

        EditorGUILayout.PropertyField(socketTypeProp);
        EditorGUILayout.PropertyField(snapAxisProp);
        EditorGUILayout.PropertyField(snapOffsetProp);
        EditorGUILayout.PropertyField(snapRotationProp);

        serializedObject.ApplyModifiedProperties();
    }
}
