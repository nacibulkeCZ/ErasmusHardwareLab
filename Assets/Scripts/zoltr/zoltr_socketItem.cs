using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Rigidbody))]
public class zoltr_socketItem : MonoBehaviour
{
    public object_id socket_type;

    [Header("Ghost Visualization")]
    [Tooltip("Assign a custom material here. If left empty, a default transparent purple material will be used.")]
    public Material customGhostMaterial;

    private Rigidbody rb;
    private XRGrabInteractable grabInteractable;
    private zoltr_socket hoverSocket;
    private zoltr_socket attachedSocket;

    // Ghost object variables
    private GameObject currentGhost;
    private Material activeGhostMaterial;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();

        // Setup the material used for the ghost
        if (customGhostMaterial != null)
        {
            activeGhostMaterial = customGhostMaterial;
        }
        else
        {
            // Create default purple transparent material
            activeGhostMaterial = new Material(Shader.Find("Standard"));
            activeGhostMaterial.SetFloat("_Mode", 2); // 2 = Fade
            activeGhostMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            activeGhostMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            activeGhostMaterial.SetInt("_ZWrite", 0);
            activeGhostMaterial.DisableKeyword("_ALPHATEST_ON");
            activeGhostMaterial.EnableKeyword("_ALPHABLEND_ON");
            activeGhostMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            activeGhostMaterial.renderQueue = 3000;

            // Default color: Purple with 50% opacity
            activeGhostMaterial.color = new Color(0.5f, 0f, 0.5f, 0.5f);
        }
    }

    void OnEnable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrab);
            grabInteractable.selectExited.AddListener(OnRelease);
        }
    }

    void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrab);
            grabInteractable.selectExited.RemoveListener(OnRelease);
        }
        DestroyGhost(); // Cleanup if disabled
    }

    private void Update()
    {
        // Update the ghost position every frame while hovering
        if (currentGhost != null && hoverSocket != null)
        {
            UpdateGhostTransform();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        zoltr_socket socket = other.GetComponent<zoltr_socket>();
        if (socket != null && socket.socket_type == socket_type && !socket.used)
        {
            hoverSocket = socket;

            // CHECK: Only create ghost if the player is currently HOLDING the object
            if (grabInteractable != null && grabInteractable.isSelected)
            {
                CreateGhost();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        zoltr_socket socket = other.GetComponent<zoltr_socket>();
        if (socket != null && socket == hoverSocket)
        {
            hoverSocket = null;
            DestroyGhost(); // Remove visual when leaving
        }
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        if (attachedSocket != null)
        {
            attachedSocket.used = false;
            attachedSocket = null;
        }
        transform.SetParent(null);
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.detectCollisions = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // CHECK: If we grab the object while it's already inside a socket trigger, show the ghost now
        if (hoverSocket != null && !hoverSocket.used)
        {
            CreateGhost();
        }
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        DestroyGhost(); // Ensure ghost is gone when released

        if (hoverSocket != null && !hoverSocket.used)
        {
            attachedSocket = hoverSocket;
            attachedSocket.used = true;
            transform.SetParent(attachedSocket.transform);
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.detectCollisions = false;
            rb.interpolation = RigidbodyInterpolation.None;

            // Snap logic
            transform.localPosition = attachedSocket.snapOffset;
            Quaternion targetRot = CalculateSnapRotation(attachedSocket);
            transform.localRotation = targetRot;
        }
        else
        {
            transform.SetParent(null);
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.detectCollisions = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    // --- Ghost Helper Methods ---

    private void CreateGhost()
    {
        if (currentGhost != null) return;

        // Instantiate a full copy of the current object to preserve hierarchy and child meshes
        currentGhost = Instantiate(gameObject);
        currentGhost.name = $"{gameObject.name}_Ghost";

        // --- STRIP COMPONENTS ---
        // We need to remove logic and physics so the ghost is just visual

        // Remove Rigidbody
        Rigidbody ghostRb = currentGhost.GetComponent<Rigidbody>();
        if (ghostRb) Destroy(ghostRb);

        // Remove Colliders (from root and all children)
        foreach (var c in currentGhost.GetComponentsInChildren<Collider>())
            Destroy(c);

        // Remove this script itself
        foreach (var s in currentGhost.GetComponentsInChildren<zoltr_socketItem>())
            Destroy(s);

        // Remove Interaction components
        foreach (var i in currentGhost.GetComponentsInChildren<XRGrabInteractable>())
            Destroy(i);

        // Remove Handle scripts if present
        foreach (var h in currentGhost.GetComponentsInChildren<zoltr_itemHandle>())
            Destroy(h);

        // --- APPLY MATERIAL ---
        // Find all renderers in the ghost hierarchy and apply the ghost material
        Renderer[] renderers = currentGhost.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            r.material = activeGhostMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // Ghosts shouldn't cast shadows
        }

        // Initial positioning
        UpdateGhostTransform();
    }

    private void DestroyGhost()
    {
        if (currentGhost != null)
        {
            Destroy(currentGhost);
            currentGhost = null;
        }
    }

    private void UpdateGhostTransform()
    {
        if (hoverSocket == null || currentGhost == null) return;

        // Position: Socket Position + Socket Rotation * Offset
        Vector3 targetPos = hoverSocket.transform.TransformPoint(hoverSocket.snapOffset);

        // Rotation: Combined socket rotation + offset rotation
        Quaternion relativeRot = CalculateSnapRotation(hoverSocket); // Local rotation relative to socket
        Quaternion targetRot = hoverSocket.transform.rotation * relativeRot; // World rotation

        currentGhost.transform.position = targetPos;
        currentGhost.transform.rotation = targetRot;
    }

    private Quaternion CalculateSnapRotation(zoltr_socket socket)
    {
        Vector3 alignAxis = Vector3.forward;
        switch (socket.snapAxis)
        {
            case SnapAxisDirection.Back: alignAxis = Vector3.back; break;
            case SnapAxisDirection.Up: alignAxis = Vector3.up; break;
            case SnapAxisDirection.Down: alignAxis = Vector3.down; break;
            case SnapAxisDirection.Left: alignAxis = Vector3.left; break;
            case SnapAxisDirection.Right: alignAxis = Vector3.right; break;
        }
        return Quaternion.FromToRotation(alignAxis, Vector3.forward) * Quaternion.Euler(socket.snapRotation);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        GUIStyle style = new GUIStyle();
        style.normal.textColor = new Color(0.5f, 0.8f, 1f);
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 12;
        Handles.Label(transform.position + Vector3.up * 0.15f, $"Item: {socket_type}", style);
    }
#endif
}