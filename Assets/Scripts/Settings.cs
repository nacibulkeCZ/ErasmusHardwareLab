using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class Settings : MonoBehaviour
{
    private const string LanguagePreferenceKey = "SelectedLanguage";

    [Header("Language")]
    [SerializeField] private TextMeshProUGUI languageButtonText;
    [SerializeField] private string stringTableName = "LocalisationTable2";
    [SerializeField] private string languageButtonKey = "Settings.Language";
    [SerializeField] private string languageButtonFallback = "Language:";

    [Header("Controls PNG")]
    [SerializeField] private GameObject controlsObjectToShow;
    [SerializeField] private GameObject settingsObjectToHide;
    [SerializeField] private Image controlsImage;
    [SerializeField] private Sprite controlsSprite;
    [SerializeField] private bool hideControlsOnStart = true;

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
    }

    private void Start()
    {
        if (hideControlsOnStart)
        {
            SetControlsVisible(false);
        }

        string savedLanguage = PlayerPrefs.GetString(LanguagePreferenceKey, string.Empty);
        if (!string.IsNullOrEmpty(savedLanguage))
        {
            ChangeLanguage(savedLanguage);
        }
        else
        {
            StartCoroutine(UpdateLanguageButtonTextRoutine());
        }
    }

    private void OnSelectedLocaleChanged(Locale locale)
    {
        StartCoroutine(UpdateLanguageButtonTextAfterLocalizationRoutine());
    }

    public void ChangeScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("Settings.ChangeScene was called without a scene name.");
            return;
        }

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadSceneSmooth(sceneName);
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    public void ChangeLanguage(string languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            Debug.LogWarning("Settings.ChangeLanguage was called without a language code.");
            return;
        }

        StartCoroutine(ChangeLanguageRoutine(languageCode));
    }

    public void ChangeToNextLanguage()
    {
        StartCoroutine(ChangeToNextLanguageRoutine());
    }

    public void ShowControlsPng()
    {
        if (controlsImage != null && controlsSprite != null)
        {
            controlsImage.sprite = controlsSprite;
        }

        SetControlsVisible(true);
    }

    public void HideControlsPng()
    {
        SetControlsVisible(false);
        StartCoroutine(UpdateLanguageButtonTextAfterLocalizationRoutine());
    }

    private System.Collections.IEnumerator ChangeLanguageRoutine(string languageCode)
    {
        yield return LocalizationSettings.InitializationOperation;

        Locale selectedLocale = null;
        foreach (Locale locale in LocalizationSettings.AvailableLocales.Locales)
        {
            if (locale.Identifier.Code == languageCode)
            {
                selectedLocale = locale;
                break;
            }
        }

        if (selectedLocale == null)
        {
            Debug.LogWarning($"No locale with code '{languageCode}' was found.");
            yield break;
        }

        LocalizationSettings.SelectedLocale = selectedLocale;
        PlayerPrefs.SetString(LanguagePreferenceKey, languageCode);
        PlayerPrefs.Save();
        UpdateLanguageButtonText();
    }

    private System.Collections.IEnumerator ChangeToNextLanguageRoutine()
    {
        yield return LocalizationSettings.InitializationOperation;

        var locales = LocalizationSettings.AvailableLocales.Locales;
        if (locales == null || locales.Count == 0)
        {
            Debug.LogWarning("No localization locales are available.");
            yield break;
        }

        int currentIndex = locales.IndexOf(LocalizationSettings.SelectedLocale);
        int nextIndex = (currentIndex + 1) % locales.Count;
        Locale nextLocale = locales[nextIndex];

        LocalizationSettings.SelectedLocale = nextLocale;
        PlayerPrefs.SetString(LanguagePreferenceKey, nextLocale.Identifier.Code);
        PlayerPrefs.Save();
        UpdateLanguageButtonText();
    }

    private System.Collections.IEnumerator UpdateLanguageButtonTextRoutine()
    {
        yield return LocalizationSettings.InitializationOperation;
        UpdateLanguageButtonText();
    }

    private System.Collections.IEnumerator UpdateLanguageButtonTextAfterLocalizationRoutine()
    {
        yield return null;
        yield return null;
        UpdateLanguageButtonText();
    }

    private void UpdateLanguageButtonText()
    {
        if (languageButtonText == null || LocalizationSettings.SelectedLocale == null)
        {
            return;
        }

        string languageLabel = LocalizationSettings.StringDatabase.GetLocalizedString(stringTableName, languageButtonKey);
        if (string.IsNullOrEmpty(languageLabel))
        {
            languageLabel = languageButtonFallback;
        }

        languageButtonText.text = languageLabel + " " + LocalizationSettings.SelectedLocale.LocaleName;
    }

    private void SetControlsVisible(bool visible)
    {
        if (controlsObjectToShow != null)
        {
            controlsObjectToShow.SetActive(visible);
        }

        if (settingsObjectToHide != null)
        {
            settingsObjectToHide.SetActive(!visible);
        }

        if (controlsObjectToShow == null && controlsImage != null)
        {
            controlsImage.gameObject.SetActive(visible);
        }
    }
}
