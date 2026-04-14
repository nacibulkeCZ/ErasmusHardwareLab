using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

[RequireComponent (typeof(HandAnimationHandlerer))]
public class HandAnimationInputer : MonoBehaviour
{
    [SerializeField] private XRInputValueReader<Vector2> thumbstickInput = new XRInputValueReader<Vector2>("Thumbstick");
    [SerializeField] private XRInputValueReader<float> triggerInput = new XRInputValueReader<float>("Trigger");
    [SerializeField] private XRInputValueReader<float> gripInput = new XRInputValueReader<float>("Grip");

    private HandAnimationHandlerer handAnimationHandlerer;
    private void Start()
    {
        handAnimationHandlerer = GetComponent<HandAnimationHandlerer>();
    }

    private float oldTriggerValue = 0f;
    private float oldGripValue = 0f;
    private float smoothTime = 0.01f;
    private void Update()
    {
        Vector2 thumbstickValue = Vector2.zero;
        float triggerValue = 0f;
        float gripValue = 0f;

        if (thumbstickInput != null) { thumbstickValue = thumbstickInput.ReadValue(); }
        if (triggerInput != null) { triggerValue = triggerInput.ReadValue(); }
        if (gripInput != null) { gripValue = gripInput.ReadValue(); }

        triggerValue = Mathf.SmoothDamp(oldTriggerValue, triggerValue, ref oldTriggerValue, smoothTime);
        gripValue = Mathf.SmoothDamp(oldGripValue, gripValue, ref oldGripValue, smoothTime);
        oldTriggerValue = triggerValue;
        oldGripValue = gripValue;

        if (gripValue > 0.1f && triggerValue > 0.1f)
        {
            handAnimationHandlerer.palecValue = Mathf.Clamp01((gripValue + triggerValue) / 2);
        }
        handAnimationHandlerer.prostrednicekValue = Mathf.Clamp01(gripValue);
        handAnimationHandlerer.prstenicekValue = Mathf.Clamp01(gripValue);
        handAnimationHandlerer.malicekValue = Mathf.Clamp01(gripValue);
        handAnimationHandlerer.ukazovacekValue = Mathf.Clamp01(triggerValue);
    }
}
