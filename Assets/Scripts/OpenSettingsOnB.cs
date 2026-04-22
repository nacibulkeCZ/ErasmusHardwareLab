using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class OpenSettingsOnB : MonoBehaviour
{
    [SerializeField] private string settingsSceneName = "Settings";

    private readonly List<UnityEngine.XR.InputDevice> rightHandDevices = new List<UnityEngine.XR.InputDevice>();
    private bool wasBPressed;

#if ENABLE_INPUT_SYSTEM
    private InputAction rightSecondaryButtonAction;
#endif

    private void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM
        rightSecondaryButtonAction = new InputAction("Open Settings", InputActionType.Button);
        rightSecondaryButtonAction.AddBinding("<XRController>{RightHand}/secondaryButton");
        rightSecondaryButtonAction.AddBinding("<OculusTouchController>{RightHand}/secondaryButton");
        rightSecondaryButtonAction.Enable();
#endif
    }

    private void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM
        rightSecondaryButtonAction?.Disable();
        rightSecondaryButtonAction?.Dispose();
        rightSecondaryButtonAction = null;
#endif
    }

    private void Update()
    {
        bool isBPressed = IsRightControllerBPressed() || IsInputSystemBPressed();

        if (isBPressed && !wasBPressed)
        {
            Debug.Log("Oculus B button pressed. Opening Settings scene.");
            OpenSettings();
        }

        wasBPressed = isBPressed;
    }

    private bool IsRightControllerBPressed()
    {
        if (rightHandDevices.Count == 0 || !rightHandDevices[0].isValid)
        {
            rightHandDevices.Clear();
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller,
                rightHandDevices
            );
        }

        foreach (UnityEngine.XR.InputDevice device in rightHandDevices)
        {
            if (device.isValid &&
                device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out bool pressed) &&
                pressed)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsInputSystemBPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return rightSecondaryButtonAction != null && rightSecondaryButtonAction.IsPressed();
#else
        return false;
#endif
    }

    private void OpenSettings()
    {
        if (string.IsNullOrWhiteSpace(settingsSceneName))
        {
            Debug.LogWarning("OpenSettingsOnB has no settings scene name assigned.");
            return;
        }

        if (SceneManager.GetActiveScene().name == settingsSceneName)
        {
            return;
        }

        SceneManager.LoadScene(settingsSceneName);
    }
}
