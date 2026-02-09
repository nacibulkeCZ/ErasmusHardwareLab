using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
    public TMP_Dropdown sceneDropDown;
    public Button playButton;
    public string[] sceneNames = { "SceneA", "SceneB", "SceneC" };

    private void Start()
    {
        playButton.onClick.AddListener(OnPlayButtonClicked);
    }

    public void OnPlayButtonClicked()
    {
        int selectedIndex = sceneDropDown.value;
        string selectedSceneName;
        switch (selectedIndex)
        {
            case 0: selectedSceneName = sceneNames[0]; break;
            case 1: selectedSceneName = sceneNames[1]; break;
            case 2: selectedSceneName = sceneNames[2]; break;
            default: selectedSceneName = sceneNames[0]; break;
        }
        UnityEngine.SceneManagement.SceneManager.LoadScene(selectedSceneName);
    }
}
