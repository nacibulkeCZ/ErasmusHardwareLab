using UnityEngine;

public class SceneManagerForUI : MonoBehaviour
{
    public void LoadScene(string sceneName)
    {
        SceneTransitionManager.Instance.LoadSceneSmooth(sceneName);
    }
    public void QuitApplication()
    {
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
