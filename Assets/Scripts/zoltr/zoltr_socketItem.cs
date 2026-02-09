using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Rigidbody))]
public class zoltr_socketItem : MonoBehaviour
{
    public object_id socket_type;

    private Rigidbody rb;
    private XRGrabInteractable grabInteractable;
    private zoltr_socket hoverSocket;
    private zoltr_socket attachedSocket;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();
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
    }

    private void OnTriggerEnter(Collider other)
    {
        zoltr_socket socket = other.GetComponent<zoltr_socket>();
        if (socket != null && socket.socket_type == socket_type && !socket.used)
        {
            hoverSocket = socket;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        zoltr_socket socket = other.GetComponent<zoltr_socket>();
        if (socket != null && socket == hoverSocket)
        {
            hoverSocket = null;
        }
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        // When object is in the socket, grabbing it should make it dynamic and unsnap it from the socket
        if (attachedSocket != null)
        {
            attachedSocket.used = false;
            attachedSocket = null;
        }

        // Always unparent when grabbed so it doesn't stay attached to hierarchy
        transform.SetParent(null);
        
        // Restore dynamic physics
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.detectCollisions = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        // Case 1: Released into a valid socket
        if (hoverSocket != null && !hoverSocket.used)
        {
            attachedSocket = hoverSocket;
            attachedSocket.used = true;
            
            // Parent to the socket so it moves with it
            transform.SetParent(attachedSocket.transform);
            
            // Make it static (kinematic) and snap to position
            rb.isKinematic = true;          // Disable physics simulation
            rb.useGravity = false;          // No gravity
            rb.detectCollisions = false;    // Disable collisions
            rb.interpolation = RigidbodyInterpolation.None; // VITAL: Prevents jitter/lag when parent moves
            
            // Snap position
            transform.localPosition = attachedSocket.snapOffset;
            
            // Snap rotation based on the selected axis
            Vector3 alignAxis = Vector3.forward;
            switch (attachedSocket.snapAxis)
            {
                case SnapAxisDirection.Back: alignAxis = Vector3.back; break;
                case SnapAxisDirection.Up: alignAxis = Vector3.up; break;
                case SnapAxisDirection.Down: alignAxis = Vector3.down; break;
                case SnapAxisDirection.Left: alignAxis = Vector3.left; break;
                case SnapAxisDirection.Right: alignAxis = Vector3.right; break;
            }
            transform.localRotation = Quaternion.FromToRotation(alignAxis, Vector3.forward) * Quaternion.Euler(attachedSocket.snapRotation);
        }
        // Case 2: Released into thin air
        else
        {
            // Ensure proper unparenting
            transform.SetParent(null);

            // Ensure gravity and physics are restored
            rb.isKinematic = false;
            rb.useGravity = true;           // Explicitly enable gravity
            rb.detectCollisions = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }
}
