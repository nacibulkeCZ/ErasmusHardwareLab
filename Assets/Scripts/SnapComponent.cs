using UnityEngine;

public class SnapComponent : MonoBehaviour
{
    public Socket socket;
    public float dragAwayThreshold = 0.5f;

    [Header("Dependencies")]
    public MonoBehaviour[] dependenciesToDismantle;

    private Vector3 offset;
    private float zCoord;
    private Socket hoveredSocket = null;
    private bool isDragging = false;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Transform originalParent;
    private bool mousePressed = false;
    private Socket assignedSocket;
    private Rigidbody rb;

    void Start()
    {
        // Store the originally assigned socket from inspector
        assignedSocket = socket;

        // Component starts free, not in socket
        socket = null;

        // Get rigidbody reference and configure physics
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = true; // Always use gravity
            rb.isKinematic = false; // Allow physics
        }

        // Enable collisions when free (not in socket)
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = false;
        }
    }

    void OnMouseDown()
    {
        // If in socket and dependencies not met, don't allow any dragging
        if (socket != null && !AreAllDependenciesDismantled())
        {
            Debug.Log($"Blocking drag - socket: {socket?.name}, dependencies dismantled: {AreAllDependenciesDismantled()}");
            return;
        }

        zCoord = Camera.main.WorldToScreenPoint(transform.position).z;
        offset = transform.position - GetMouseWorldPos();
        isDragging = true;

        // Enable collisions while dragging
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = false;
        }

        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalParent = transform.parent;

        // Update socket reference based on current parent
        if (transform.parent != null)
        {
            Socket parentSocket = transform.parent.GetComponent<Socket>();
            if (parentSocket != null)
            {
                socket = parentSocket;
            }
        }
        else
        {
            socket = null;
        }

    }

    void OnMouseDrag()
    {
        Vector3 newPosition = GetMouseWorldPos() + offset;

        // Use rigidbody movement to maintain physics interactions
        if (rb != null)
        {
            rb.MovePosition(newPosition);
        }
        else
        {
            transform.position = newPosition;
        }

        float distance = Vector3.Distance(originalPosition, newPosition);

        if (socket != null && distance > dragAwayThreshold)
        {
            // Only allow removal from socket if dependencies are dismantled
            if (AreAllDependenciesDismantled())
            {
                // Preserve world position when unparenting
                Vector3 worldPosition = transform.position;
                Quaternion worldRotation = transform.rotation;

                transform.SetParent(originalParent, false);
                transform.position = worldPosition;
                transform.rotation = worldRotation;
                socket = null;

                // Enable collisions and physics when removed from socket
                Collider col = GetComponent<Collider>();
                if (col != null)
                {
                    col.isTrigger = false;
                }
                if (rb != null)
                {
                    rb.isKinematic = false;
                }
            }
            else
            {
                // Dependencies not met, snap back to socket
                transform.position = originalPosition;
            }
        }
    }

    void OnMouseUp()
    {
        isDragging = false;
        if (hoveredSocket != null)
        {
            // Only snap if dependencies are dismantled
            if (AreAllDependenciesDismantled())
            {
                // Snap to the socket's transform position and rotation, and parent to the socket
                transform.SetParent(hoveredSocket.transform);
                transform.position = hoveredSocket.transform.position;
                transform.rotation = hoveredSocket.transform.rotation;
                socket = hoveredSocket;

                // Disable collisions and physics when in socket
                Collider col = GetComponent<Collider>();
                if (col != null)
                {
                    col.isTrigger = true;
                }
                if (rb != null)
                {
                    rb.isKinematic = true;
                }

                // Ensure the collider is still active for mouse events
                Collider collider = GetComponent<Collider>();
                if (collider != null)
                {
                    collider.enabled = true;
                }
            }
        }
    }

    private Vector3 GetMouseWorldPos()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = zCoord;
        return Camera.main.ScreenToWorldPoint(mousePoint);
    }

    private bool AreAllDependenciesDismantled()
    {
        if (dependenciesToDismantle == null || dependenciesToDismantle.Length == 0)
            return true;

        foreach (MonoBehaviour dependency in dependenciesToDismantle)
        {
            if (dependency == null) continue;

            if (dependency is OpenComponent openComponent)
            {
                if (!openComponent.dismantled)
                    return false;
            }
            // Add more component types here later (ScrewComponent, etc.)
        }

        return true;
    }

    void OnTriggerEnter(Collider other)
    {
        if ((isDragging || mousePressed) && other.TryGetComponent<Socket>(out Socket s))
        {
            hoveredSocket = s;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<Socket>(out Socket s) && hoveredSocket == s)
        {
            hoveredSocket = null;
        }
    }

    void Update()
    {
        // Handle mouse input manually when socket might be blocking
        if (Input.GetMouseButtonDown(0))
        {
            CheckMouseClick();
        }

        if (mousePressed && Input.GetMouseButton(0))
        {
            HandleDrag();
        }

        if (Input.GetMouseButtonUp(0))
        {
            HandleMouseUp();
        }
    }

    void CheckMouseClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray);

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.gameObject == gameObject)
            {
                // If in socket and dependencies not met, don't allow any dragging
                if (socket != null && !AreAllDependenciesDismantled())
                {
                    Debug.Log($"Blocking manual drag - socket: {socket?.name}, dependencies dismantled: {AreAllDependenciesDismantled()}");
                    return;
                }

                mousePressed = true;
                zCoord = Camera.main.WorldToScreenPoint(transform.position).z;
                offset = transform.position - GetMouseWorldPos();
                isDragging = true;

                // Enable collisions while dragging
                Collider col = GetComponent<Collider>();
                if (col != null)
                {
                    col.isTrigger = false;
                }

                originalPosition = transform.position;
                originalRotation = transform.rotation;
                originalParent = transform.parent;

                // Update socket reference based on current parent
                if (transform.parent != null)
                {
                    Socket parentSocket = transform.parent.GetComponent<Socket>();
                    if (parentSocket != null)
                    {
                        socket = parentSocket;
                    }
                }
                else
                {
                    socket = null;
                }

                break;
            }
        }
    }

    void HandleDrag()
    {
        if (!mousePressed) return;

        Vector3 newPosition = GetMouseWorldPos() + offset;
        transform.position = newPosition;

        float distance = Vector3.Distance(originalPosition, newPosition);

        if (socket != null && distance > dragAwayThreshold)
        {
            // Only allow removal from socket if dependencies are dismantled
            if (AreAllDependenciesDismantled())
            {
                // Preserve world position when unparenting
                Vector3 worldPosition = transform.position;
                Quaternion worldRotation = transform.rotation;

                transform.SetParent(originalParent, false);
                transform.position = worldPosition;
                transform.rotation = worldRotation;
                socket = null;
                hoveredSocket = null;

                // Enable collisions and physics when removed from socket
                Collider col = GetComponent<Collider>();
                if (col != null)
                {
                    col.isTrigger = false;
                }
                if (rb != null)
                {
                    rb.isKinematic = false;
                }
            }
            else
            {
                // Dependencies not met, snap back to socket
                transform.position = originalPosition;
            }
        }
    }

    void HandleMouseUp()
    {
        if (!mousePressed) return;

        mousePressed = false;
        isDragging = false;

        // Check if we're near the originally assigned socket
        if (assignedSocket != null)
        {
            float distanceToAssignedSocket = Vector3.Distance(transform.position, assignedSocket.transform.position);

            if (distanceToAssignedSocket <= 1f)
            {
                // Only snap into socket if dependencies are dismantled
                if (AreAllDependenciesDismantled())
                {
                    // Snap back to the originally assigned socket
                    transform.SetParent(assignedSocket.transform);
                    transform.position = assignedSocket.transform.position;
                    transform.rotation = assignedSocket.transform.rotation;
                    socket = assignedSocket;

                    // Disable collisions and physics when in socket
                    Collider col = GetComponent<Collider>();
                    if (col != null)
                    {
                        col.isTrigger = true;
                    }
                    if (rb != null)
                    {
                        rb.isKinematic = true;
                    }
                }
                else
                {
                    // Dependencies not met, stay free
                    transform.SetParent(originalParent);
                    socket = null;
                }
            }
            else
            {
                // Too far from assigned socket, make it free
                transform.SetParent(originalParent);
                socket = null;
            }
        }
        else
        {
            // No assigned socket, make sure we're unparented
            transform.SetParent(originalParent);
            socket = null;
        }

        // Reset hoveredSocket after processing
        hoveredSocket = null;
    }
}
