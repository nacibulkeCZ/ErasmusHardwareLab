using UnityEngine;

public class SnapHoldingComponent : MonoBehaviour
{
    [Header("Put this script on the object to snap")]
    public Transform movingObjectTransform;
    public Transform snapPoint;
    public GameObject SnappingObject;
    public float snapDistance = 0.1f; 

    private Quaternion initialRotation;
    private Vector3 initialSnapPointPosition;

    

    private bool isSnapped = false;
    private void Update()
    {
        if (movingObjectTransform == null || snapPoint == null || SnappingObject == null) return;

        float distance = Vector3.Distance(movingObjectTransform.position, snapPoint.position);
        if (distance <= snapDistance && !isSnapped)
        {
            initialSnapPointPosition = snapPoint.position;
            initialRotation = SnappingObject.transform.rotation;
            isSnapped = true;
        }
        else if (distance > snapDistance && isSnapped)
        {
            isSnapped = false;
            SnappingObject.transform.localPosition = Vector3.zero;
            SnappingObject.transform.rotation = initialRotation;
            snapPoint.position = initialSnapPointPosition;
        }

        if (isSnapped)
        {
            SnappingObject.transform.position = snapPoint.position;
            SnappingObject.transform.rotation = snapPoint.rotation;
        }
    }

    public void OnDrawGizmos()
    {
        if (snapPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(snapPoint.position, snapDistance);
        }
    }
}
