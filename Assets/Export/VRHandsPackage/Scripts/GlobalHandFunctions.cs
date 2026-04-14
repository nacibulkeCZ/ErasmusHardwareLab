using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class GlobalHandFunctions : MonoBehaviour
{
    public XRBaseInteractor leftHandNearFarInteractor;
    public XRBaseInteractor rightHandNearFarInteractor;

    public static GlobalHandFunctions instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    public IEnumerator dropItems()
    {
        if (leftHandNearFarInteractor != null && rightHandNearFarInteractor != null)
        {
            leftHandNearFarInteractor.allowSelect = false;
            rightHandNearFarInteractor.allowSelect = false;
        }
        yield return null;
        yield return null;
        if (leftHandNearFarInteractor != null && rightHandNearFarInteractor != null)
        {
            leftHandNearFarInteractor.allowSelect = true;
            rightHandNearFarInteractor.allowSelect = true;
        }
    }

    public static void DropItemsStatic()
    {
       instance.StartCoroutine(instance.dropItems());
    }
}
