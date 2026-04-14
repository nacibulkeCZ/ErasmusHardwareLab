using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRGrabInteractable))]
public class VRInspectRotate : MonoBehaviour
{
    [Header("Rotation")]
    public Transform referenceFrame;
    public float yawDegreesPerMeter = 160f;
    public float pitchDegreesPerMeter = 140f;
    public bool lockPositionWhileInspecting = true;

    private XRGrabInteractable grabInteractable;
    private IXRSelectInteractor activeInteractor;
    private bool inspectEnabled;
    private bool isSelected;
    private Vector3 lastInteractorPosition;
    private Vector3 lockedPosition;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        if (grabInteractable == null)
            grabInteractable = GetComponent<XRGrabInteractable>();

        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnSelectEntered);
            grabInteractable.selectExited.AddListener(OnSelectExited);
        }
    }

    private void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
            grabInteractable.selectExited.RemoveListener(OnSelectExited);
        }

        activeInteractor = null;
        isSelected = false;
    }

    private void Update()
    {
        if (!inspectEnabled || !isSelected || activeInteractor == null)
            return;

        Transform interactorTransform = activeInteractor.transform;
        if (interactorTransform == null)
            return;

        Vector3 currentInteractorPosition = interactorTransform.position;
        Vector3 delta = currentInteractorPosition - lastInteractorPosition;
        lastInteractorPosition = currentInteractorPosition;

        Transform frame = referenceFrame != null ? referenceFrame : (Camera.main != null ? Camera.main.transform : transform);
        float yaw = Vector3.Dot(delta, frame.right) * yawDegreesPerMeter;
        float pitch = -Vector3.Dot(delta, frame.up) * pitchDegreesPerMeter;

        transform.Rotate(frame.up, yaw, Space.World);
        transform.Rotate(frame.right, pitch, Space.World);

        if (lockPositionWhileInspecting)
            transform.position = lockedPosition;
    }

    public void SetInspectEnabled(bool enabled)
    {
        inspectEnabled = enabled;

        if (!inspectEnabled)
        {
            activeInteractor = null;
            isSelected = false;
        }
        else
        {
            lockedPosition = transform.position;
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (!inspectEnabled)
            return;

        activeInteractor = args.interactorObject as IXRSelectInteractor;
        isSelected = true;
        lockedPosition = transform.position;

        if (activeInteractor != null && activeInteractor.transform != null)
            lastInteractorPosition = activeInteractor.transform.position;
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        if (!inspectEnabled)
            return;

        activeInteractor = null;
        isSelected = false;
    }
}
