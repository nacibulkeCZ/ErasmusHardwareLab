using UnityEngine;

public class GrabableHandler : MonoBehaviour
{
    public Transform target;
    Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (target != null)
        {
            rb.MovePosition(target.position);
        }
    }
}
