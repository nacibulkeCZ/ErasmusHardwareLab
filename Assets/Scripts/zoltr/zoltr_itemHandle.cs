using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

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
        
        // Ensure handle collider is set up properly
        if (handleCollider == null)
        {
            Debug.LogError("Handle Collider is not assigned on " + gameObject.name);
            return;
        }
        
        // Make sure handle is on a separate GameObject if it isn't already
        // This ensures only the handle responds to interactions
        if (handleCollider.gameObject == gameObject)
        {
            Debug.LogWarning("HandleCollider should be on a child object for best results. Consider moving it to a separate GameObject.");
        }
        
        handleCollider.isTrigger = true;
        
        // Set the handle collider's layer to the interaction layer
        int interactionLayer = LayerMask.NameToLayer(interactionLayerName);
        if (interactionLayer == -1)
        {
            Debug.LogWarning("Interaction layer '" + interactionLayerName + "' not found. Using Default layer.");
            interactionLayer = 0;
        }
        handleCollider.gameObject.layer = interactionLayer;
        
        // Setup XR Grab Interactable if not already present
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable == null)
        {
            grabInteractable = gameObject.AddComponent<XRGrabInteractable>();
        }
        
        // Position attach point at the handle's center
        if (grabInteractable.attachTransform == null)
        {
            GameObject attachObj = new GameObject("AttachPoint");
            attachObj.transform.SetParent(handleCollider.transform);
            attachObj.transform.localPosition = Vector3.zero;
            attachObj.transform.localRotation = Quaternion.identity;
            grabInteractable.attachTransform = attachObj.transform;
            attachPoint = attachObj.transform;
        }
        
        // CRITICAL: Configure the grab interactable to ONLY use the handle collider
        grabInteractable.colliders.Clear();
        grabInteractable.colliders.Add(handleCollider);
        
        // Enable dynamic attach so object keeps its rotation when grabbed
        grabInteractable.useDynamicAttach = true;
        
        // Set interaction layers - only respond to Direct Interactors
        grabInteractable.interactionLayers = InteractionLayerMask.GetMask(interactionLayerName);
        
        // Rigidbody settings
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
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
        // Optional: Add custom grab behavior here
    }

    void OnRelease(SelectExitEventArgs args)
    {
        // Optional: Add custom release behavior here
    }
}
