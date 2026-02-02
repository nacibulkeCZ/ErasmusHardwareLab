using UnityEngine;

[DisallowMultipleComponent]
public class SnapPoint : MonoBehaviour
{
    [Tooltip("Optional child transform used for final object position/rotation. If empty, this GameObject transform is used.")]
    public Transform attachTransform;

    [HideInInspector] public bool occupied = false;

    void Reset()
    {
        if (attachTransform == null) attachTransform = transform;
    }

    void OnValidate()
    {
        if (attachTransform == null) attachTransform = transform;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = occupied ? new Color(1f, 0.5f, 0.5f) : Color.green;
        var at = attachTransform != null ? attachTransform : transform;
        Gizmos.DrawWireSphere(at.position, 0.05f);
        Gizmos.DrawLine(transform.position, at.position);
    }
}
