using System.Collections;
using TMPro;
using UnityEngine;

public class ShowDescriptionObjectManager : MonoBehaviour
{
    public Material noiseMat;
    public Material defaultMat;
    public GameObject display;
    public TMP_Text nameText;
    public TMP_Text descriptionText;

    [HideInInspector] public ShowDescriptionObject currentlyShowObject;

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
        display.GetComponent<Renderer>().material = noiseMat;
        nameText.text = "";
        descriptionText.text = "";
        yield return new WaitForSeconds(0.5f);
        if (obj != null)
        {
            nameText.text = obj.objectName;
            descriptionText.text = obj.description;
            if (obj.objectImage != null)
                display.GetComponent<Renderer>().material = obj.objectImage;
            else
                display.GetComponent<Renderer>().material = defaultMat;
        } else
        {
            display.GetComponent<Renderer>().material = defaultMat;
        }
            currentlyShowObject = obj;
        Debug.Log("Switched display to object: " + (obj != null ? obj.objectName : "None"));
    }
}
