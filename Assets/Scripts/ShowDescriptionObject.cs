using UnityEngine;

public class ShowDescriptionObject : MonoBehaviour
{
    public string objectName;
    public string description;
    public Material objectImage;

    public void ShowObjectDescription()
    {
        ShowDescriptionObjectManager.TurnOnObject(this);
    }

    public void HideObjectDescription()
    {
        ShowDescriptionObjectManager.TurnOffObject(this);
    }
}
