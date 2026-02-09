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
        // --- 1. Visualize the Physical Socket Trigger (Base) ---
        Collider col = GetComponent<Collider>();
        Gizmos.color = used ? new Color(1f, 0.2f, 0.2f, 0.2f) : new Color(0.2f, 1f, 0.2f, 0.15f); // Red/Green transparent

        if (col != null)
        {
            // Draw the actual collider on the object
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
            // Fallback visualization if no collider
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one * 0.05f);
        }

        // --- 2. Calculate Snap Target Transforms ---
        // This is the specific point in World Space where the Item's pivot will snap to
        Vector3 worldSnapPos = transform.TransformPoint(snapOffset);

        // Calculate the rotation exactly as the item script does
        Vector3 alignAxis = Vector3.forward;
        switch (snapAxis)
        {
            case SnapAxisDirection.Back: alignAxis = Vector3.back; break;
            case SnapAxisDirection.Up: alignAxis = Vector3.up; break;
            case SnapAxisDirection.Down: alignAxis = Vector3.down; break;
            case SnapAxisDirection.Left: alignAxis = Vector3.left; break;
            case SnapAxisDirection.Right: alignAxis = Vector3.right; break;
        }

        Quaternion relativeRot = Quaternion.FromToRotation(alignAxis, Vector3.forward) * Quaternion.Euler(snapRotation);
        Quaternion finalRot = transform.rotation * relativeRot;

        // --- 3. Draw the "Ghost Item" at Snap Position ---
        Gizmos.color = new Color(1f, 0.92f, 0.016f, 1f); // Solid Yellow
        Matrix4x4 oldMatrix = Gizmos.matrix;

        // We set the matrix to the SNAP position and FINAL rotation
        Gizmos.matrix = Matrix4x4.TRS(worldSnapPos, finalRot, transform.lossyScale);

        // Determine a size for the ghost box
        Vector3 displaySize = new Vector3(0.1f, 0.1f, 0.1f); // Default size
        if (col is BoxCollider b) displaySize = b.size;      // Match socket size if box

        // FIX: Draw at Vector3.zero (The Pivot) instead of col.center.
        // This ensures the box rotates around the snap point, not orbiting it.
        Gizmos.DrawWireCube(Vector3.zero, displaySize);

        // Optional: Faint fill
        Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.15f);
        Gizmos.DrawCube(Vector3.zero, displaySize);

        // Draw a small "Forward" pointer inside the ghost box so you know which way is facing out
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(Vector3.zero, Vector3.forward * (displaySize.z * 0.6f));

        Gizmos.matrix = oldMatrix; // Restore matrix

        // --- 4. Draw Connection Line ---
        Handles.color = new Color(1f, 0.92f, 0.016f, 0.5f); // Yellow transparent
        Handles.DrawDottedLine(transform.position, worldSnapPos, 4f);

        // --- 5. Draw Text Label ---
        GUIStyle style = new GUIStyle();
        style.normal.textColor = used ? Color.red : Color.green;
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 12;
        style.fontStyle = FontStyle.Bold;

        string status = used ? "[USED]" : "[EMPTY]";
        Handles.Label(transform.position + Vector3.up * 0.2f, $"{socket_type}\n{status}", style);
    }
#endif
}