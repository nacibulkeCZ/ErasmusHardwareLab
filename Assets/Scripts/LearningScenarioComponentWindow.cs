using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class LearningScenarioComponentWindow : MonoBehaviour
{
    public LearningScenarioStart scenario;
    public string componentId;
    public Transform componentRoot;

    private XRSimpleInteractable xrSimpleInteractable;

    private void Awake()
    {
        EnsureInteractable();
    }

    private void OnEnable()
    {
        EnsureInteractable();
        if (xrSimpleInteractable != null)
            xrSimpleInteractable.selectEntered.AddListener(OnXRSelect);
    }

    private void OnDisable()
    {
        if (xrSimpleInteractable != null)
            xrSimpleInteractable.selectEntered.RemoveListener(OnXRSelect);
    }

    public void SelectComponent()
    {
        if (scenario == null)
            scenario = FindFirstObjectByType<LearningScenarioStart>();

        if (scenario == null)
            return;

        if (!string.IsNullOrWhiteSpace(componentId))
        {
            scenario.SelectComponentById(componentId);
            return;
        }

        if (componentRoot != null)
        {
            scenario.SelectComponent(componentRoot);
            return;
        }

        scenario.SelectFromWindow(transform);
    }

    private void OnMouseDown()
    {
        SelectComponent();
    }

    private void OnXRSelect(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args)
    {
        SelectComponent();
    }

    private void EnsureInteractable()
    {
        xrSimpleInteractable = GetComponent<XRSimpleInteractable>();
        if (xrSimpleInteractable == null)
            xrSimpleInteractable = gameObject.AddComponent<XRSimpleInteractable>();

        if (GetComponent<Collider>() == null)
            gameObject.AddComponent<BoxCollider>();
    }
}
