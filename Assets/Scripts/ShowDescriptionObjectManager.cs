using System.Collections;
using TMPro;
using UnityEngine;

public class ShowDescriptionObjectManager : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text descriptionText;

    [Header("Sounds")]
    public SoundManager soundManager;
    public AudioSource audioSource;
    public AudioClip typeSound;
    public AudioClip wipeSound;

    [HideInInspector] public ShowDescriptionObject currentlyShowObject;

    private void Start()
    {
        if (soundManager == null)
            soundManager = FindFirstObjectByType<SoundManager>();
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public static void TurnOnObject(ShowDescriptionObject obj)
    {
        if (obj != null)
        {
            var manager = FindFirstObjectByType<ShowDescriptionObjectManager>();
            if (manager != null)
            {
                manager.StopAllCoroutines();
                manager.StartCoroutine(manager.SwitchToObject(obj));
                Debug.Log("Showing description for object: " + obj.objectName);
            } else
            {
                Debug.LogWarning("ShowDescriptionObjectManager not found in the scene.");
            }
        }
    }

    public static void TurnOffObject(ShowDescriptionObject obj)
    {
        var manager = FindFirstObjectByType<ShowDescriptionObjectManager>();
        if (manager != null)
        {
            if (manager.currentlyShowObject == obj)
            {
                manager.StopAllCoroutines();
                manager.StartCoroutine(manager.SwitchToObject(null));
                Debug.Log("Hiding description for object: " + obj.objectName);
            }
        } else
        {
            Debug.LogWarning("ShowDescriptionObjectManager not found in the scene.");
        }
    }

    public IEnumerator SwitchToObject(ShowDescriptionObject obj)
    {
        if (soundManager != null)
        {
            soundManager.PlaySFX3D(wipeSound, transform.position, 0.5f);
        }
        nameText.text = "";
        descriptionText.text = "";
        yield return new WaitForSeconds(0.5f);
        currentlyShowObject = obj;
        if (obj != null)
        {
            nameText.text = obj.objectName;
            descriptionText.text = obj.description;
            yield return StartCoroutine(StartWriteAnimation());
        }
    }

    public IEnumerator StartWriteAnimation()
    {
        string fullName = nameText.text;
        string fullDescription = descriptionText.text;
        nameText.text = "";
        descriptionText.text = "";
        foreach (char c in fullName)
        {
            nameText.text += c;
            if (soundManager != null)
            {
                soundManager.PlaySFX3D(typeSound, transform.position, 0.5f);
            }
            yield return new WaitForSeconds(0.05f);
        }
        foreach (char c in fullDescription)
        {
            descriptionText.text += c;
            if (soundManager != null)
            {
                soundManager.PlaySFX3D(typeSound, transform.position, 0.5f);
            }
            yield return new WaitForSeconds(0.02f);
        }
    }
}
