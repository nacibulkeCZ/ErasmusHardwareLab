using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

[RequireComponent(typeof(LineRenderer))]
public class Cables : MonoBehaviour
{
    [Header("Anchors")]
    [SerializeField] private Transform endPoint;
    [SerializeField] private Transform startOverride;
    [SerializeField] private NavMeshSurface navSurface; // optional: ensures we use the right agent type

    [Header("Shape")]
    [SerializeField] private float segmentSpacing = 0.5f;
    [SerializeField] private float contactOffset = 0.02f;
    [SerializeField] private float surfaceSnapDistance = 2f;
    [SerializeField] private LayerMask surfaceMask = ~0;

    [Header("Visuals - Tube")]
    [SerializeField] private bool useTubeMesh = true;
    [SerializeField] private float tubeRadius = 0.01f;
    [SerializeField] private int tubeSides = 8;
    [SerializeField] private Material tubeMaterial;
    [SerializeField] private bool hideLineRendererWhenTubed = false;

    [Header("Growth")]
    [SerializeField] private float growthSpeed = 2f;
    [SerializeField] private bool autoStart = true;

    [Header("Edges & Links")]
    [SerializeField] private float edgeClearance = 0.1f;
    [SerializeField] private float edgeVerticalThreshold = 0.05f;

    private LineRenderer line;
    private MeshFilter tubeFilter;
    private MeshRenderer tubeRenderer;
    private Mesh tubeMesh;
    private readonly List<Vector3> tubePositions = new List<Vector3>(128);
    private Coroutine growRoutine;
    private NavMeshQueryFilter filter;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        SetupTube();
        filter = BuildFilter();
    }

    private void OnEnable()
    {
        if (autoStart)
        {
            StartCable();
        }
    }

    [ContextMenu("Start Cable")]
    public void StartCable()
    {
        if (endPoint == null)
        {
            Debug.LogWarning($"{nameof(Cables)}: End point not assigned.");
            return;
        }

        if (growRoutine != null)
        {
            StopCoroutine(growRoutine);
        }

        var path = BuildSnappedPath();
        if (path.Count < 2)
        {
            Debug.LogWarning($"{nameof(Cables)}: Path could not be built.");
            return;
        }

        growRoutine = StartCoroutine(GrowCable(path));
    }

    private void SetupTube()
    {
        if (!useTubeMesh) return;

        // Build the tube on a child GameObject so we don't override any MeshRenderer on the anchor object.
        const string tubeName = "CableMesh";
        Transform child = transform.Find(tubeName);
        if (child == null)
        {
            var go = new GameObject(tubeName);
            go.transform.SetParent(transform, false);
            child = go.transform;
        }

        child.localPosition = Vector3.zero;
        child.localRotation = Quaternion.identity;
        child.localScale = Vector3.one;

        tubeFilter = child.GetComponent<MeshFilter>();
        if (tubeFilter == null) tubeFilter = child.gameObject.AddComponent<MeshFilter>();

        tubeRenderer = child.GetComponent<MeshRenderer>();
        if (tubeRenderer == null) tubeRenderer = child.gameObject.AddComponent<MeshRenderer>();

        if (tubeMaterial != null)
        {
            tubeRenderer.sharedMaterial = tubeMaterial;
        }

        if (tubeMesh == null)
        {
            tubeMesh = new Mesh { name = "CableTubeMesh" };
            tubeMesh.MarkDynamic();
        }

        tubeFilter.sharedMesh = tubeMesh;
    }

    private List<Vector3> BuildSnappedPath()
    {
        var points = new List<Vector3>();

        Vector3 startPos = startOverride != null ? startOverride.position : transform.position;
        Vector3 endPos = endPoint.position;

        Vector3 navStart = SampleNavMesh(startPos, out bool startOnNav);
        Vector3 navEnd = SampleNavMesh(endPos, out bool endOnNav);

        var navPath = new NavMeshPath();
        bool hasNavPath = startOnNav && endOnNav && NavMesh.CalculatePath(navStart, navEnd, filter, navPath);

        if (hasNavPath && navPath.corners != null && navPath.corners.Length >= 2)
        {
            for (int i = 0; i < navPath.corners.Length - 1; i++)
            {
                AppendSegment(navPath.corners[i], navPath.corners[i + 1], points);
            }
        }
        else
        {
            AppendSegment(startPos, endPos, points);
        }

        ImproveEdgeClearance(points);
        return points;
    }

    private void AppendSegment(Vector3 a, Vector3 b, List<Vector3> points)
    {
        float distance = Vector3.Distance(a, b);
        int steps = Mathf.Max(1, Mathf.CeilToInt(distance / Mathf.Max(0.01f, segmentSpacing)));

        int startIndex = points.Count > 0 ? 1 : 0; // skip the first point when continuing a chain

        for (int i = startIndex; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector3 sample = Vector3.Lerp(a, b, t);
            Vector3 snapped = SnapToSurface(sample);

            if (points.Count == 0 || Vector3.SqrMagnitude(points[points.Count - 1] - snapped) > 0.0001f)
            {
                points.Add(snapped);
            }
        }
    }

    private Vector3 SnapToSurface(Vector3 point)
    {
        // Cast down from above the point to find a nearby collider to sit on.
        float radius = Mathf.Max(0.01f, useTubeMesh ? tubeRadius : contactOffset);
        Vector3 origin = point + Vector3.up * surfaceSnapDistance;
        if (Physics.SphereCast(origin, radius, Vector3.down, out var hit, surfaceSnapDistance * 2f, surfaceMask, QueryTriggerInteraction.Ignore))
        {
            float lift = contactOffset + (useTubeMesh ? tubeRadius : 0f);
            return hit.point + hit.normal * lift;
        }

        return point;
    }

    private void ImproveEdgeClearance(List<Vector3> pts)
    {
        if (pts.Count < 2 || edgeClearance <= 0f) return;

        for (int i = 0; i < pts.Count - 1; i++)
        {
            Vector3 a = pts[i];
            Vector3 b = pts[i + 1];
            float vertical = Mathf.Abs(b.y - a.y);
            if (vertical < edgeVerticalThreshold) continue;

            float clearance = Mathf.Max(edgeClearance, tubeRadius + contactOffset);

            Vector3 pushA = Vector3.zero;
            Vector3 pushB = Vector3.zero;

            // Push back along incoming direction, or along the horizontal of this segment if no previous.
            if (i > 0)
            {
                Vector3 prevDir = pts[i] - pts[i - 1];
                prevDir.y = 0f;
                if (prevDir.sqrMagnitude > 0.0001f)
                {
                    pushA -= prevDir.normalized * clearance;
                }
            }
            else
            {
                Vector3 horiz = new Vector3(b.x - a.x, 0f, b.z - a.z);
                if (horiz.sqrMagnitude > 0.0001f)
                {
                    pushA -= horiz.normalized * clearance;
                }
            }

            // Push forward along outgoing direction, or along this segment if no next.
            if (i + 2 < pts.Count)
            {
                Vector3 nextDir = pts[i + 2] - pts[i + 1];
                nextDir.y = 0f;
                if (nextDir.sqrMagnitude > 0.0001f)
                {
                    pushB += nextDir.normalized * clearance;
                }
            }
            else
            {
                Vector3 horiz = new Vector3(b.x - a.x, 0f, b.z - a.z);
                if (horiz.sqrMagnitude > 0.0001f)
                {
                    pushB += horiz.normalized * clearance;
                }
            }

            pts[i] = SnapToSurface(a + pushA);
            pts[i + 1] = SnapToSurface(b + pushB);
        }
    }

    private Vector3 SampleNavMesh(Vector3 position, out bool success)
    {
        success = NavMesh.SamplePosition(position, out var hit, 2f, filter.areaMask);
        return success ? hit.position : position;
    }

    private NavMeshQueryFilter BuildFilter()
    {
        int typeId = GetAgentTypeId();
        var f = new NavMeshQueryFilter
        {
            agentTypeID = typeId,
            areaMask = NavMesh.AllAreas
        };
        return f;
    }

    private int GetAgentTypeId()
    {
        if (navSurface != null)
        {
            return navSurface.agentTypeID;
        }

        if (NavMesh.GetSettingsCount() > 0)
        {
            return NavMesh.GetSettingsByIndex(0).agentTypeID;
        }

        return 0;
    }

    private IEnumerator GrowCable(IReadOnlyList<Vector3> path)
    {
        line.positionCount = 1;
        line.SetPosition(0, path[0]);
        // Keep line visible until tube has at least 2 points.
        line.enabled = true;

        Vector3 current = path[0];
        for (int i = 1; i < path.Count; i++)
        {
            Vector3 target = path[i];
            float segmentLength = Vector3.Distance(current, target);

            if (segmentLength < 0.001f)
            {
                continue;
            }

            line.positionCount += 1;
            int idx = line.positionCount - 1;
            float travelled = 0f;

            while (travelled < segmentLength)
            {
                travelled += growthSpeed * Time.deltaTime;
                float alpha = Mathf.Clamp01(travelled / segmentLength);
                Vector3 pos = Vector3.Lerp(current, target, alpha);
                line.SetPosition(idx, pos);
                UpdateTubeMesh();
                yield return null;
            }

            current = target;
            line.SetPosition(idx, target);
            UpdateTubeMesh();
        }

        UpdateTubeMesh();

        if (hideLineRendererWhenTubed && useTubeMesh)
        {
            line.enabled = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (endPoint == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(startOverride != null ? startOverride.position : transform.position, 0.05f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(endPoint.position, 0.05f);
    }

    private void UpdateTubeMesh()
    {
        if (!useTubeMesh || tubeMesh == null) return;
        int count = line.positionCount;
        if (count < 2)
        {
            tubeMesh.Clear();
            return;
        }

        tubePositions.Clear();
        for (int i = 0; i < count; i++)
        {
            tubePositions.Add(line.GetPosition(i));
        }

        int sides = Mathf.Max(3, tubeSides);
        int rings = tubePositions.Count;
        int vertCount = rings * sides;
        int triCount = (rings - 1) * sides * 6;

        var vertices = new Vector3[vertCount];
        var normals = new Vector3[vertCount];
        var uvs = new Vector2[vertCount];
        var tris = new int[triCount];

        // Precompute lengths for v coordinate
        float totalLen = 0f;
        var lengths = new float[rings];
        lengths[0] = 0f;
        for (int i = 1; i < rings; i++)
        {
            totalLen += Vector3.Distance(tubePositions[i - 1], tubePositions[i]);
            lengths[i] = totalLen;
        }
        float invTotalLen = totalLen > 0.0001f ? 1f / totalLen : 0f;

        for (int i = 0; i < rings; i++)
        {
            Vector3 prev = i > 0 ? tubePositions[i - 1] : tubePositions[i];
            Vector3 next = i < rings - 1 ? tubePositions[i + 1] : tubePositions[i];
            Vector3 forward = (next - prev).normalized;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }

            Vector3 up = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > 0.99f ? Vector3.right : Vector3.up;
            Vector3 right = Vector3.Cross(forward, up).normalized;
            up = Vector3.Cross(right, forward).normalized;

            float v = lengths[i] * invTotalLen;

            for (int s = 0; s < sides; s++)
            {
                float ang = (s / (float)sides) * Mathf.PI * 2f;
                Vector3 offset = Mathf.Cos(ang) * right * tubeRadius + Mathf.Sin(ang) * up * tubeRadius;
                int idx = i * sides + s;
                // Convert to tube local space so it stays aligned if the parent moves/rotates.
                Vector3 worldPos = tubePositions[i] + offset;
                vertices[idx] = tubeFilter.transform.InverseTransformPoint(worldPos);
                normals[idx] = tubeFilter.transform.InverseTransformDirection(offset.normalized);
                uvs[idx] = new Vector2(s / (float)sides, v);
            }
        }

        int tri = 0;
        for (int i = 0; i < rings - 1; i++)
        {
            for (int s = 0; s < sides; s++)
            {
                int curr = i * sides + s;
                int next = i * sides + ((s + 1) % sides);
                int currNextRing = curr + sides;
                int nextNextRing = next + sides;

                tris[tri++] = curr;
                tris[tri++] = next;
                tris[tri++] = currNextRing;

                tris[tri++] = next;
                tris[tri++] = nextNextRing;
                tris[tri++] = currNextRing;
            }
        }

        tubeMesh.Clear();
        tubeMesh.vertices = vertices;
        tubeMesh.normals = normals;
        tubeMesh.uv = uvs;
        tubeMesh.triangles = tris;
        tubeMesh.RecalculateBounds();
    }
}
