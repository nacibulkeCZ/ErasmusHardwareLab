using UnityEngine;

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
}
