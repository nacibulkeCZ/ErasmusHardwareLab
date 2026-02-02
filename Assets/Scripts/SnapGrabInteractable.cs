using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class SnapGrabInteractable : XRGrabInteractable
{
    [Header("Snap settings")]
    [Tooltip("If empty, script will auto-find all SnapPoint components in scene.")]
    [HideInInspector]
    public SnapPoint[] snapPoints;

    [Tooltip("Max distance from a snap point to snap on release (meters).")]
    public float snapDistance = 0.12f;

    [Tooltip("Transform on THIS object that should be aligned to the snap point. " +
             "If empty, the object's root transform is used.")]
    public Transform objectAttachTransform;

    [Tooltip("If true, parent the object to the snap point's attach transform while snapped.")]
    public bool parentOnSnap = false;

    [Tooltip("Optional tag to filter snap points. If empty, all snap points are considered.")]
    public string snapPointTag = "";

    [Tooltip("Speed of smooth snapping transition. Higher = faster snap.")]
    public float snapSpeed = 10f;

    [Tooltip("Distance multiplier for unsnapping (prevents snap/unsnap loops).")]
    public float unsnapDistanceMultiplier = 2f;

    [Header("Screwing settings")]
    [Tooltip("Allow rotation around the object's forward axis while snapped (for screwing motion).")]
    public bool allowScrewingRotation = true;

    [Tooltip("Sensitivity of screwing rotation. Higher = more rotation per controller movement.")]
    public float screwingSensitivity = 2f;

    [Tooltip("Which axis to use for screwing rotation.")]
    public ScrewingAxis screwingAxis = ScrewingAxis.FromSocketZ;

    public enum ScrewingAxis
    {
        X,              // Use object's X axis (right)
        Y,              // Use object's Y axis (up)
        Z,              // Use object's Z axis (forward)
        FromSocketX,    // Use the socket's X axis (right)
        FromSocketY,    // Use the socket's Y axis (up)
        FromSocketZ     // Use the socket's Z axis (forward)
    }

    Rigidbody rb;

    // state
    SnapPoint currentSnapPoint = null;
    bool isSnapped = false;
    bool wasGrabbed = false;
    bool isSnapping = false; // smooth transition state
    Vector3 snapStartPos;
    Quaternion snapStartRot;
    Vector3 snapTargetPos;
    Quaternion snapTargetRot;
    float snapProgress = 0f;
    float lastSnapTime = 0f;
    float snapCooldown = 0.2f; // prevent rapid snap/unsnap

    // screwing motion state
    Vector3 lastControllerPosition;
    bool trackingControllerForScrewing = false;

    // original physics state
    bool originalKinematic;
    bool originalUseGravity;
    float originalMass;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody>();
        if (rb == null) Debug.LogError("SnapGrabInteractable needs a Rigidbody.");

        originalKinematic = rb.isKinematic;
        originalUseGravity = rb.useGravity;
        originalMass = rb.mass;

        snapPoints = FindObjectsByType<SnapPoint>(FindObjectsSortMode.None);
    }

    // When grabbed: if it was snapped, unsnap so player can move it freely
    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        wasGrabbed = true;
        trackingControllerForScrewing = false; // Reset screwing tracking
        
        if (isSnapped && !allowScrewingRotation)
        {
            Unsnap(); // Only unsnap if screwing is disabled
        }
    }

    // When released: try to snap to nearest free snap point
    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        wasGrabbed = false;
        trackingControllerForScrewing = false; // Stop screwing tracking
        TrySnapNearest();
    }

    // Continuous checking while being held
    void Update()
    {
        // Handle smooth snapping transition
        if (isSnapping)
        {
            UpdateSmoothSnap();
        }
        
        // Handle screwing motion when snapped and grabbed
        if (isSnapped && wasGrabbed && allowScrewingRotation && !isSnapping)
        {
            UpdateScrewingRotation();
        }
        
        // Only check for new snaps/unsnaps if not currently in transition and cooldown has passed
        if (!isSnapping && Time.time > lastSnapTime + snapCooldown)
        {
            if (wasGrabbed && !isSnapped)
            {
                TrySnapNearest();
            }
            else if (isSnapped && wasGrabbed)
            {
                // Always check unsnap when snapped and grabbed
                CheckUnsnap();
            }
        }
    }

    void UpdateSmoothSnap()
    {
        snapProgress += Time.deltaTime * snapSpeed;
        
        if (snapProgress >= 1f)
        {
            // Snap complete
            snapProgress = 1f;
            isSnapping = false;
            
            // Final positioning
            SetPositionAndRotationAroundSnapPoint(snapTargetPos, snapTargetRot);
            
            // Now actually mark as snapped and set physics
            FinalizeSnap();
        }
        else
        {
            // Smooth interpolation around the snap point
            Vector3 lerpedPos = Vector3.Lerp(snapStartPos, snapTargetPos, snapProgress);
            Quaternion lerpedRot = Quaternion.Lerp(snapStartRot, snapTargetRot, snapProgress);
            
            SetPositionAndRotationAroundSnapPoint(lerpedPos, lerpedRot);
        }
    }

    void SetPositionAndRotationAroundSnapPoint(Vector3 targetPos, Quaternion targetRot)
    {
        // Simply set the target position and rotation - the math is already done in SnapToPoint
        transform.position = targetPos;
        transform.rotation = targetRot;
    }

    void UpdateScrewingRotation()
    {
        if (interactorsSelecting.Count == 0 || currentSnapPoint == null) return;

        var grabber = interactorsSelecting[0];
        if (grabber == null || grabber.transform == null) return;

        Vector3 currentControllerPos = grabber.transform.position;

        if (!trackingControllerForScrewing)
        {
            // Start tracking
            lastControllerPosition = currentControllerPos;
            trackingControllerForScrewing = true;
            return;
        }

        // Calculate controller movement
        Vector3 controllerMovement = currentControllerPos - lastControllerPosition;
        lastControllerPosition = currentControllerPos;

        // Get the snap point position and determine the rotation axis
        Transform snapTransform = currentSnapPoint.attachTransform != null 
            ? currentSnapPoint.attachTransform 
            : currentSnapPoint.transform;

        Vector3 snapPos = snapTransform.position;
        Vector3 rotationAxis = GetScrewingAxis(snapTransform);

        // Project controller movement onto a plane perpendicular to the rotation axis
        Vector3 movementOnPlane = Vector3.ProjectOnPlane(controllerMovement, rotationAxis);

        // Calculate rotation around the axis based on circular movement
        if (movementOnPlane.magnitude > 0.001f)
        {
            // Get vector from snap point to controller
            Vector3 toController = currentControllerPos - snapPos;
            Vector3 toControllerOnPlane = Vector3.ProjectOnPlane(toController, rotationAxis);

            // Calculate the angular movement using cross product
            Vector3 cross = Vector3.Cross(toControllerOnPlane.normalized, movementOnPlane);
            float rotationAmount = Vector3.Dot(cross, rotationAxis) * screwingSensitivity;

            // Apply rotation around the chosen axis while keeping position locked
            Vector3 currentPos = transform.position;
            transform.RotateAround(snapPos, rotationAxis, rotationAmount * Mathf.Rad2Deg);
            
            // Lock position back to where it should be (in case RotateAround moved it slightly)
            if (objectAttachTransform != null)
            {
                Vector3 currentOffset = objectAttachTransform.position - transform.position;
                transform.position = snapPos - currentOffset;
            }
        }
    }

    Vector3 GetScrewingAxis(Transform socketTransform)
    {
        switch (screwingAxis)
        {
            case ScrewingAxis.X:
                return transform.right; // Use object's X axis (right)
            case ScrewingAxis.Y:
                return transform.up; // Use object's Y axis (up)
            case ScrewingAxis.Z:
                return transform.forward; // Use object's Z axis (forward)
            case ScrewingAxis.FromSocketX:
                return socketTransform.right; // Use socket's X axis (right)
            case ScrewingAxis.FromSocketY:
                return socketTransform.up; // Use socket's Y axis (up)
            case ScrewingAxis.FromSocketZ:
                return socketTransform.forward; // Use socket's Z axis (forward)
            default:
                return socketTransform.forward;
        }
    }

    // --- Snap logic ---

    void TrySnapNearest()
    {
        if (snapPoints == null || snapPoints.Length == 0) return;

        SnapPoint best = null;
        float bestDist = float.MaxValue;
        Vector3 myPos = GetObjectAttachWorldPosition();

        foreach (var sp in snapPoints)
        {
            if (sp == null) continue;
            if (sp.occupied) continue;

            // Check tag filter if specified
            if (!string.IsNullOrEmpty(snapPointTag) && !sp.CompareTag(snapPointTag)) continue;

            Transform at = sp.attachTransform != null ? sp.attachTransform : sp.transform;
            if (at == null) continue; // Additional safety check

            float dist = Vector3.Distance(myPos, at.position);

            if (dist <= snapDistance && dist < bestDist)
            {
                bestDist = dist;
                best = sp;
            }
        }

        if (best != null)
            SnapToPoint(best);
        else if (!wasGrabbed) // Only unsnap if not being held
            Unsnap(); // nothing to snap to -> make sure we're unsnapped
    }

    void SnapToPoint(SnapPoint point)
    {
        if (point == null || point.occupied) return;
        if (isSnapping || isSnapped) return; // prevent multiple snaps

        Transform at = point.attachTransform != null ? point.attachTransform : point.transform;
        if (at == null) 
        {
            Debug.LogWarning($"SnapPoint {point.name} has no valid attach transform.");
            return;
        }

        // Simple and direct approach: position object so attach point overlaps snap point
        Vector3 desiredPos;
        Quaternion desiredRot = at.rotation;

        if (objectAttachTransform == null)
        {
            // No specific attach point, snap object center to snap point
            desiredPos = at.position;
        }
        else
        {
            // Direct approach: move object so attach point is exactly at snap point
            // First, apply the rotation
            desiredRot = at.rotation;
            
            // Then calculate position: where should the object be so that attach point ends up at snap point?
            Vector3 currentOffset = objectAttachTransform.position - transform.position;
            desiredPos = at.position - currentOffset;
            
            // But we need to account for rotation! Rotate the offset by the difference in rotation
            Quaternion rotationDiff = desiredRot * Quaternion.Inverse(transform.rotation);
            Vector3 rotatedOffset = rotationDiff * currentOffset;
            desiredPos = at.position - rotatedOffset;
        }

        // Set up smooth transition
        snapStartPos = transform.position;
        snapStartRot = transform.rotation;
        snapTargetPos = desiredPos;
        snapTargetRot = desiredRot;
        snapProgress = 0f;
        isSnapping = true;
        
        // Reserve the snap point immediately
        point.occupied = true;
        currentSnapPoint = point;
        lastSnapTime = Time.time;
    }

    void FinalizeSnap()
    {
        if (currentSnapPoint == null) return;
        
        isSnapped = true;

        // Make rigidbody kinematic so it stays in place and disable gravity so it doesn't 'jump'.
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;

        // Optionally parent to the snap's attach transform (keeps transform relative)
        if (parentOnSnap && currentSnapPoint.attachTransform != null)
        {
            Transform at = currentSnapPoint.attachTransform != null ? currentSnapPoint.attachTransform : currentSnapPoint.transform;
            if (at != null)
                transform.SetParent(at, true);
        }
    }

    void Unsnap()
    {
        if (!isSnapped && !isSnapping) return;

        // If we're in the middle of snapping, cancel it
        if (isSnapping)
        {
            isSnapping = false;
            snapProgress = 0f;
        }

        // free snap point
        if (currentSnapPoint != null)
        {
            currentSnapPoint.occupied = false;
        }

        // If we were parented to snap, unparent to keep independent physics
        if (parentOnSnap)
            transform.SetParent(null, true);

        currentSnapPoint = null;
        isSnapped = false;
        lastSnapTime = Time.time;

        // restore physics (including gravity) so object falls correctly after detach
        rb.isKinematic = originalKinematic;
        rb.useGravity = originalUseGravity;
    }

    // Check if we should unsnap due to distance while being held
    void CheckUnsnap()
    {
        if (currentSnapPoint == null || !wasGrabbed) return;

        Transform snapAttach = currentSnapPoint.attachTransform != null 
            ? currentSnapPoint.attachTransform 
            : currentSnapPoint.transform;

        // When snapped and grabbed, check distance between controller and snap point
        // This allows the player to pull the object away to unsnap it
        if (interactorsSelecting.Count > 0)
        {
            var grabber = interactorsSelecting[0];
            if (grabber != null && grabber.transform != null)
            {
                Vector3 grabberPos = grabber.transform.position;
                Vector3 snapPos = snapAttach.position;
                float dist = Vector3.Distance(grabberPos, snapPos);

                // Use configurable distance multiplier to prevent snap/unsnap loops
                float unsnapDistance = snapDistance * unsnapDistanceMultiplier;
                
                // Debug info (remove this later if needed)
                if (dist > unsnapDistance * 0.8f) // Show warning when getting close to unsnap
                {
                    Debug.Log($"Unsnap distance check: {dist:F3} / {unsnapDistance:F3}");
                }
                
                if (dist > unsnapDistance)
                {
                    Debug.Log("Unsnapping due to distance!");
                    Unsnap();
                }
            }
        }
    }

    // Utility: get world position of the chosen attach on the object (or object's root if null)
    Vector3 GetObjectAttachWorldPosition()
    {
        if (objectAttachTransform == null) return transform.position;
        return objectAttachTransform.position;
    }

    // Public method to force unsnap (useful for external scripts or debugging)
    public void ForceUnsnap()
    {
        Unsnap();
    }

    // Public property to check if currently snapped
    public bool IsSnapped => isSnapped;

    // Public property to get current snap point
    public SnapPoint CurrentSnapPoint => currentSnapPoint;

    void OnDrawGizmosSelected()
    {
        if (snapPoints == null) return;
        Gizmos.color = Color.cyan;
        foreach (var sp in snapPoints)
        {
            if (sp == null) continue;
            Transform at = sp.attachTransform != null ? sp.attachTransform : sp.transform;
            Gizmos.DrawWireSphere(at.position, snapDistance);
        }

        // show where on this object the snap will align
        if (objectAttachTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(objectAttachTransform.position, 0.03f);
        }
    }
}
