using UnityEngine;

public class BobAndRotate : MonoBehaviour
{
    [Header("Bob Settings")]
    public float bobHeight = 0.5f;   // how high it moves up/down
    public float bobSpeed = 2f;      // how fast it bobs

    [Header("Rotation Settings")]
    public Vector3 rotationSpeed = new Vector3(0f, 50f, 0f); // degrees per second

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        // Bobbing up and down
        float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(startPos.x, newY, startPos.z);

        // Endless rotation
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }
}
