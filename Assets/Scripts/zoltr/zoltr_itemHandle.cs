using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Rigidbody))]
public class zoltr_itemHandle : MonoBehaviour
{
    public BoxCollider handleCollider;
    [Tooltip("Interaction layer for the handle - make sure this matches your Direct Interactor layer")]
    public string interactionLayerName = "Default";

    private Rigidbody rb;
    private XRGrabInteractable grabInteractable;
    private Transform attachPoint;
    private int originalLayer;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        originalLayer = gameObject.layer;

        if (handleCollider == null)
        {
            Debug.LogError("Handle Collider is not assigned on " + gameObject.name);
            return;
        }

        if (handleCollider.gameObject == gameObject)
        {
            Debug.LogWarning("HandleCollider should be on a child object for best results. Consider moving it to a separate GameObject.");
        }

        handleCollider.isTrigger = true;

        int interactionLayer = LayerMask.NameToLayer(interactionLayerName);
        if (interactionLayer == -1)
        {
            Debug.LogWarning("Interaction layer '" + interactionLayerName + "' not found. Using Default layer.");
            interactionLayer = 0;
        }
        handleCollider.gameObject.layer = interactionLayer;

        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable == null)
        {
            grabInteractable = gameObject.AddComponent<XRGrabInteractable>();
        }

        if (grabInteractable.attachTransform == null)
        {
            GameObject attachObj = new GameObject("AttachPoint");
            attachObj.transform.SetParent(handleCollider.transform);
            attachObj.transform.localPosition = Vector3.zero;
            attachObj.transform.localRotation = Quaternion.identity;
            grabInteractable.attachTransform = attachObj.transform;
            attachPoint = attachObj.transform;
        }

        grabInteractable.colliders.Clear();
        grabInteractable.colliders.Add(handleCollider);

        grabInteractable.useDynamicAttach = true;
        grabInteractable.interactionLayers = InteractionLayerMask.GetMask(interactionLayerName);

        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
    }

    void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrab);
            grabInteractable.selectExited.RemoveListener(OnRelease);
        }
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        // Haptic feedback logic here
    }

    void OnRelease(SelectExitEventArgs args)
    {
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (handleCollider != null)
        {
            // --- 1. Draw the "Grab Zone" ---
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = handleCollider.transform.localToWorldMatrix;

            // Semi-transparent fill
            Gizmos.color = new Color(0f, 1f, 1f, 0.2f); // Cyan fill
            Gizmos.DrawCube(handleCollider.center, handleCollider.size);

            // Wireframe outline
            Gizmos.color = new Color(0f, 1f, 1f, 1f); // Cyan solid
            Gizmos.DrawWireCube(handleCollider.center, handleCollider.size);

            Gizmos.matrix = oldMatrix;

            // --- 2. Draw the Attach Point Logic ---
            // If the attach point hasn't been created yet (Edit mode), we simulate where it will be
            Vector3 attachPos = handleCollider.transform.TransformPoint(handleCollider.center);

            Handles.color = Color.white;
            Handles.DrawWireDisc(attachPos, handleCollider.transform.up, 0.05f);
            Handles.DrawWireDisc(attachPos, handleCollider.transform.right, 0.05f);

            // --- 3. Label ---
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.cyan;
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 12;

            Handles.Label(attachPos + Vector3.up * 0.15f, $"[Handle]\nLayer: {interactionLayerName}", style);
        }
        else
        {
            // Warning if collider is missing
            Handles.color = Color.red;
            Handles.Label(transform.position, "MISSING HANDLE COLLIDER!");
        }
    }
#endif
}