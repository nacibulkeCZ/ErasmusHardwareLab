using UnityEngine;

[ExecuteAlways]
public class FixCloudCulling : MonoBehaviour
{
    [Header("Endless Settings")]
    [Tooltip("If true, the box will snap to the camera's X/Z position.")]
    public bool followCamera = true;
    
    [Tooltip("Snaps movement to this grid size to prevent texture jitter/shimmering.")]
    public float gridSnap = 64.0f; 

    void Update()
    {
        // 1. Culling Fix: Force huge bounds so Unity never hides the mesh
        var meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.localBounds = new Bounds(Vector3.zero, Vector3.one * 10000f);
        }

        // 2. Endless Fix: Move the box to follow the camera
        if (followCamera)
        {
            Camera cam = Camera.main;
#if UNITY_EDITOR
            // In Scene View, follow the Scene Camera for easier editing
            if (Application.isEditor && !Application.isPlaying)
            {
                cam = UnityEditor.SceneView.lastActiveSceneView?.camera;
            }
#endif

            if (cam != null)
            {
                Vector3 camPos = cam.transform.position;

                // We snap the position to a grid (e.g., every 64 units)
                // This prevents the noise from "swimming" or jittering as you move.
                float snapX = Mathf.Round(camPos.x / gridSnap) * gridSnap;
                float snapZ = Mathf.Round(camPos.z / gridSnap) * gridSnap;

                // Keep Y position fixed (use the Inspector value) so layers don't jump up/down
                // Only move X and Z
                transform.position = new Vector3(snapX, transform.position.y, snapZ);
            }
        }
    }
}