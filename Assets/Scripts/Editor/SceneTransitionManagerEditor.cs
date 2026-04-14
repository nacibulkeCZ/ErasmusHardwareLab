using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SceneTransitionManager))]
public class SceneTransitionManagerEditor : Editor
{
    private SerializedProperty fadeDurationProp;
    private SerializedProperty fadeColorProp;
    private SerializedProperty use3DFadeProp;
    
    private SerializedProperty transitionCanvasProp;
    private SerializedProperty fadeGroupProp;
    private SerializedProperty fadeImageProp;
    
    private SerializedProperty loadingContainerProp;
    private SerializedProperty progressBarProp;
    private SerializedProperty loadingTextProp;

    private void OnEnable()
    {
        fadeDurationProp = serializedObject.FindProperty("fadeDuration");
        fadeColorProp = serializedObject.FindProperty("fadeColor");
        use3DFadeProp = serializedObject.FindProperty("use3DFade");

        transitionCanvasProp = serializedObject.FindProperty("transitionCanvas");
        fadeGroupProp = serializedObject.FindProperty("fadeGroup");
        fadeImageProp = serializedObject.FindProperty("fadeImage");

        loadingContainerProp = serializedObject.FindProperty("loadingContainer");
        progressBarProp = serializedObject.FindProperty("progressBar");
        loadingTextProp = serializedObject.FindProperty("loadingText");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Custom Styling
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            margin = new RectOffset(0, 0, 10, 5)
        };

        Texture2D bannerTex = MakeTex(2, 2, new Color(0.15f, 0.15f, 0.15f, 1f));
        GUIStyle bannerStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = bannerTex },
            margin = new RectOffset(0, 0, 5, 10),
            padding = new RectOffset(10, 10, 10, 10)
        };
        
        // Banner
        GUILayout.BeginVertical(bannerStyle);
        EditorGUILayout.LabelField("Scene Transition Manager", new GUIStyle(EditorStyles.whiteLargeLabel) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold });
        EditorGUILayout.LabelField("Seamless and robust scene transitioning with asynchronous loading", new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter });
        GUILayout.EndVertical();

        // 1. Core Visual Settings
        EditorGUILayout.LabelField("VR Transition Core", headerStyle);
        EditorGUI.BeginChangeCheck();
        
        EditorGUILayout.PropertyField(fadeDurationProp, new GUIContent("Fade Duration (s)", "Time in seconds for fading in and out."));
        EditorGUILayout.PropertyField(fadeColorProp, new GUIContent("Fade Color", "The color to fade to. Mostly black."));
        EditorGUILayout.PropertyField(use3DFadeProp, new GUIContent("Use 3D Sphere Fade", "Should use a camera-attached 3D reverse sphere instead of Canvas for VR clipping?"));
        
        if (EditorGUI.EndChangeCheck())
        {
            if (fadeDurationProp.floatValue < 0.1f) fadeDurationProp.floatValue = 0.1f;
        }

        EditorGUILayout.Space(10);

        // 2. UI Fallback section
        EditorGUILayout.LabelField("Fade 2D / UI Render", headerStyle);
        GUI.enabled = !use3DFadeProp.boolValue; // Grey out if not used
        EditorGUILayout.HelpBox("Used when 3D fade is OFF. A screen-space or world-space canvas placed over the camera.", MessageType.Info);
        
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(transitionCanvasProp, new GUIContent("Transition Canvas"));
        EditorGUILayout.PropertyField(fadeGroupProp, new GUIContent("Fade Canvas Group"));
        EditorGUILayout.PropertyField(fadeImageProp, new GUIContent("Fade 2D Image"));
        EditorGUI.indentLevel--;
        
        GUI.enabled = true; // Re-enable

        EditorGUILayout.Space(10);

        // 3. Loading Screen
        EditorGUILayout.LabelField("Background Loading Screen", headerStyle);
        EditorGUILayout.HelpBox("These elements are shown during async loading process. Wait until progress hits 100% before transition.", MessageType.Info);
        
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(loadingContainerProp, new GUIContent("Loading Container (Parent)", "The parent GameObject grouping the UI widgets below"));
        EditorGUILayout.PropertyField(progressBarProp, new GUIContent("Progress Bar (Image)"));
        EditorGUILayout.PropertyField(loadingTextProp, new GUIContent("Loading Text (TMP)"));
        EditorGUI.indentLevel--;

        // Debug tools when playing
        if (Application.isPlaying)
        {
            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("Live Debug", headerStyle);
            
            SceneTransitionManager manager = (SceneTransitionManager)target;
            EditorGUILayout.HelpBox($"Transitioning State: {(manager.IsTransitioning ? "IN PROGRESS" : "IDLE")}", manager.IsTransitioning ? MessageType.Warning : MessageType.None);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}
