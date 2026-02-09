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
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        if (hoverSocket != null && !hoverSocket.used)
        {
            attachedSocket = hoverSocket;
            attachedSocket.used = true;
            transform.SetParent(attachedSocket.transform);
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.detectCollisions = false;
            rb.interpolation = RigidbodyInterpolation.None;

            transform.localPosition = attachedSocket.snapOffset;

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
        else
        {
            transform.SetParent(null);
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.detectCollisions = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            if (hoverSocket != null)
            {
                Handles.color = Color.magenta;
                Handles.DrawDottedLine(transform.position, hoverSocket.transform.position, 5f);
                Handles.Label(Vector3.Lerp(transform.position, hoverSocket.transform.position, 0.5f), "Hovering");
            }
            if (attachedSocket != null)
            {
                Handles.color = Color.green;
                Handles.DrawLine(transform.position, attachedSocket.transform.position);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 1. Label
        GUIStyle style = new GUIStyle();
        style.normal.textColor = new Color(0.5f, 0.8f, 1f);
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 12;
        Handles.Label(transform.position + Vector3.up * 0.15f, $"Item: {socket_type}", style);

        // 2. Draw bounds of the item itself (Blue)
        // This helps compare the item size to the socket "Ghost" size
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.2f);
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;

            if (col is BoxCollider box)
            {
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }

            Gizmos.matrix = oldMatrix;
        }
    }
#endif
}