using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ShatteringGlass : MonoBehaviour
{
    [Header("Settings")]
    public GameObject shatteringGlassObject;
    public float shatteringLimit = 2f; // Minimum velocity to shatter

    private void OnCollisionEnter(Collision collision)
    {
        // Try to find a Rigidbody first
        Rigidbody rb = collision.rigidbody;

        // If there's no Rigidbody, check if it's an XR object with its own Rigidbody
        if (rb == null)
        {
            var interactable = collision.gameObject.GetComponent<XRGrabInteractable>();
            if (interactable != null)
                rb = interactable.GetComponent<Rigidbody>();
        }

        // Now check velocity
        if (rb != null)
        {
            float impactSpeed = rb.linearVelocity.magnitude * rb.mass;

            if (impactSpeed > shatteringLimit)
            {
                Shatter();
            }
        }
    }

    private void Shatter()
    {
        if (shatteringGlassObject != null)
        {
            shatteringGlassObject.SetActive(true);
        }

        gameObject.SetActive(false);
    }
}
