using UnityEngine;

public class Socket : MonoBehaviour
{
    // Returns the center of the collider attached to this socket or its children
    public Vector3 GetColliderCenter()
    {
        Collider col = GetComponent<Collider>();
        if (col == null)
            col = GetComponentInChildren<Collider>();
        if (col != null)
            return col.bounds.center;
        return transform.position;
    }
}
