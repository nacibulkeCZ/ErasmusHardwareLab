using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System.Collections.Generic;

public class SnapComponentVR : MonoBehaviour
{
    public Socket socket;
    public float dragAwayThreshold = 0.5f;

    [System.Serializable]
    public class DependencyRequirement
    {
        public MonoBehaviour dependency;
        public bool shouldBeDismantled = true;
    }

    [Header("Dependencies")]
    public List<DependencyRequirement> dependencies = new List<DependencyRequirement>();

    [Header("Screw Logic")]
    public bool isScrew = false;
    private SnapPoint snapPointComponent;

    private Socket hoveredSocket = null;
    private Socket assignedSocket;
    private Rigidbody rb;
    private Transform originalParent;
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    void Start()
    {
        assignedSocket = socket;
        socket = null;

        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = true;
            rb.isKinematic = false;
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = false;

        originalParent = transform.parent;
        originalPosition = transform.position;
        originalRotation = transform.rotation;

        snapPointComponent = GetComponent<SnapPoint>();
        isScrew = snapPointComponent != null;
        if (snapPointComponent != null)
            snapPointComponent.occupied = true;
    }

    private void Update()
    {
        // --- NEW: make locked object follow socket transform ---
        if (socket != null)
        {
            transform.position = socket.transform.position;
            transform.rotation = socket.transform.rotation;
        }
        // -------------------------------------------------------

        if (AreAllDependenciesDismantled() && rb.constraints.Equals(RigidbodyConstraints.FreezeAll))
            GetComponent<Collider>().isTrigger = false;

        if (AreAllDependenciesDismantled())
            GetComponent<XRGrabInteractable>().enabled = true;
        else if (socket != null)
            GetComponent<XRGrabInteractable>().enabled = false;

        Debug.Log("Dismantled: " + AreAllDependenciesDismantled());
    }

    private bool AreAllDependenciesDismantled()
    {
        if (dependencies == null || dependencies.Count == 0)
            return true;

        foreach (var dep in dependencies)
        {
            if (dep.dependency == null) continue;
            var type = dep.dependency.GetType();
            var field = type.GetField("dismantled",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic);
            if (field == null) continue;
            bool value = (bool)field.GetValue(dep.dependency);
            if (value != dep.shouldBeDismantled)
                return false;
        }
        return true;
    }

    public void TrySnapToHoveredSocket()
    {
        if (hoveredSocket == null) return;
        if (hoveredSocket != assignedSocket)
        {
            hoveredSocket = null;
            return;
        }

        if (AreAllDependenciesDismantled())
        {
            transform.SetParent(hoveredSocket.transform);
            transform.position = hoveredSocket.transform.position;
            transform.rotation = hoveredSocket.transform.rotation;
            socket = hoveredSocket;

            Collider col = GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;

            if (rb != null)
            {
                rb.useGravity = false;
                rb.constraints = RigidbodyConstraints.FreezeAll;
            }

            if (isScrew && snapPointComponent != null)
                snapPointComponent.occupied = false;
        }

        hoveredSocket = null;
    }

    public void ReleaseFromSocket()
    {
        if (socket == null) return;

        if (AreAllDependenciesDismantled())
        {
            Vector3 worldPos = transform.position;
            Quaternion worldRot = transform.rotation;

            transform.SetParent(originalParent, true); // Changed false to true to keep world transform safer
            transform.position = worldPos;
            transform.rotation = worldRot;
            socket = null;

            Collider col = GetComponent<Collider>();
            if (col != null)
                col.isTrigger = false;

            if (rb != null)
            {
                rb.constraints = RigidbodyConstraints.None;
                rb.useGravity = true;
                rb.isKinematic = false; 

                rb.WakeUp();
            }

            if (isScrew && snapPointComponent != null)
                snapPointComponent.occupied = true;
        }
        else
        {
            transform.position = originalPosition;
            transform.rotation = originalRotation;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Socket>(out Socket s))
            hoveredSocket = s;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<Socket>(out Socket s) && hoveredSocket == s)
            hoveredSocket = null;
    }
}
