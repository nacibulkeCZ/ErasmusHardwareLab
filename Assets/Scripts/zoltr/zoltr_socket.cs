using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum SnapAxisDirection
{
    Forward,
    Back,
    Up,
    Down,
    Left,
    Right
}

public class zoltr_socket : MonoBehaviour
{
    public object_id socket_type;
    public bool used;

    [Tooltip("The axis of the item that should align with the socket's forward direction.")]
    public SnapAxisDirection snapAxis = SnapAxisDirection.Forward;

    [Tooltip("Position offset for the snapped item relative to the socket.")]
    public Vector3 snapOffset;

    [Tooltip("Rotation offset for the snapped item (in degrees).")]
    public Vector3 snapRotation;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // --- 1. Visualize the Physical Socket Trigger ---
        Collider col = GetComponent<Collider>();
        Gizmos.color = used ? new Color(1f, 0.2f, 0.2f, 0.2f) : new Color(0.2f, 1f, 0.2f, 0.15f); // Red/Green transparent
        
        if (col != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            if (col is BoxCollider box)
            {
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.color = used ? Color.red : Color.green;
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawSphere(sphere.center, sphere.radius);
                Gizmos.color = used ? Color.red : Color.green;
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }
        }
        else
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one * 0.05f);
        }

        // --- 2. Draw Simple Connection Line to Center ---
        // Just a simple line so you know where the center is, but no box.
        Gizmos.matrix = Matrix4x4.identity;
        Vector3 worldSnapPos = transform.TransformPoint(snapOffset);
        
        Handles.color = new Color(1f, 0.92f, 0.016f, 0.5f); // Yellow transparent
        Handles.DrawDottedLine(transform.position, worldSnapPos, 4f);
        
        // --- 3. Draw Text Label ---
        GUIStyle style = new GUIStyle();
        style.normal.textColor = used ? Color.red : Color.green;
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 12;
        style.fontStyle = FontStyle.Bold;

        string status = used ? "[USED]" : "[EMPTY]";
        Handles.Label(transform.position, $"{socket_type}\n{status}", style);
    }
#endif
}