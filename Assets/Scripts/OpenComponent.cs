using UnityEngine;

public class OpenComponent : MonoBehaviour
{
    public Transform pivot;
    public Vector3 targetRotation;
    public float duration = 1f;
    public Axis axis = Axis.Y;
    public bool animateOnStart = false;

    [Header("Dismantled State")]
    public bool dismantled = false;

    private Quaternion originalRotation;
    private Quaternion openRotation;
    private Vector3 originalOffset;
    private float elapsed = 0f;
    private bool animating = false;
    private bool isOpen = false;

    public enum Axis { X, Y, Z }

    void Start()
    {
        if (pivot == null) pivot = transform;
        originalRotation = transform.rotation;
        originalOffset = transform.position - pivot.position;
        openRotation = GetOpenRotation();
        if (animateOnStart) StartAnimation();
    }

    void OnMouseDown()
    {
        if (!animating)
        {
            StartAnimation();
        }
    }

    void Update()
    {
        if (animating)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            Quaternion startRot = isOpen ? openRotation : originalRotation;
            Quaternion endRot = isOpen ? originalRotation : openRotation;

            Quaternion currentRot = Quaternion.Slerp(startRot, endRot, t);
            Vector3 rotatedOffset = currentRot * Quaternion.Inverse(originalRotation) * originalOffset;
            transform.position = pivot.position + rotatedOffset;
            transform.rotation = currentRot;

            if (t >= 1f)
            {
                animating = false;
                isOpen = !isOpen;
                dismantled = isOpen;
            }
        }
    }

    private void StartAnimation()
    {
        elapsed = 0f;
        animating = true;
    }

    private Quaternion GetOpenRotation()
    {
        Vector3 euler = originalRotation.eulerAngles;
        switch (axis)
        {
            case Axis.X: euler.x = targetRotation.x; break;
            case Axis.Y: euler.y = targetRotation.y; break;
            case Axis.Z: euler.z = targetRotation.z; break;
        }
        return Quaternion.Euler(euler);
    }
}
