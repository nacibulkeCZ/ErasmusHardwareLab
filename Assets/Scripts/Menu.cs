using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR;
using TMPro;

    public class Menu : MonoBehaviour
    {
        [Serializable]
        public class VRPanelConfig
        {
            [Tooltip("The RectTransform of the UI panel.")]
            public RectTransform panel;
            [Tooltip("The name of the scene to load upon clicking.")]
            public string targetScene;
            
            [Header("Content Injection")]
            [Tooltip("The UI Text element for the panel's title.")]
            public TextMeshProUGUI titleText;
            [Tooltip("The UI Text element for the panel's description.")]
            public TextMeshProUGUI descriptionText;
            [Tooltip("The text string to automatically overwrite into the titleText element.")]
            public string titleContent;
            [TextArea]
            [Tooltip("The text string to automatically overwrite into the descriptionText element.")]
            public string descriptionContent;
            [Tooltip("Optional 3D object to act as the floating holographic icon for this panel.")]
            public Transform icon3D;
        }

        [Serializable]
        public class NarrationHighlightTiming
        {
            [Tooltip("Index of the panel in the panels array to highlight.")]
            public int panelIndex;
            [Tooltip("Audio timestamp to start the highlight.")]
            public float startTime;
            [Tooltip("Audio timestamp to end the highlight.")]
            public float endTime;
        }

    [Tooltip("List of UI Panels to handle. Configure your 3 panels alongside each other here.")]
    public VRPanelConfig[] panels = new VRPanelConfig[3];

    [Header("Narration Sequence Defaults")]
    [Tooltip("Optional background voiceover clip that auto-plays on Scene Start.")]
    public AudioClip narrationClip;
    [Tooltip("Define at what timestamp each panel should light up (soft glow) while the voiceover speaks.")]
    public List<NarrationHighlightTiming> narrationTimings = new List<NarrationHighlightTiming>();

    [Header("Environment VFX")]
    [Tooltip("The primary ambient light in the scene that reacts to menu states.")]
    public Light ambientLight;
    [Tooltip("The default color of the ambient light when no panel is focused.")]
    public Color normalAmbientColor = new Color(0.1f, 0.2f, 0.4f, 1f);
    [Tooltip("The color the ambient light transitions to when a panel is interacted with.")]
    public Color interactionAmbientColor = new Color(0.2f, 0.4f, 0.8f, 1f);
    [Tooltip("Optional particle system acting as digital data streams in the background.")]
    public ParticleSystem dataStreamParticles;
    [Tooltip("How fast the ambient light changes colors when a panel is focused.")]
    public float ambientTransitionSpeed = 5f;

    [Header("Animation Physics")]
    [Tooltip("How much the panel moves up on hover.")]
    public float hoverTranslationY = 15f;
    [Tooltip("Scale multiplier when hovering.")]
    public float hoverScale = 1.05f;
    [Tooltip("Scale multiplier when pressing down.")]
    public float pressScale = 0.93f;
    [Tooltip("How fast the UI element transitions using exponential damping. Higher value = faster snap.")]
    public float smoothSpeed = 16f;
    
    [Header("Focus State Settings")]
    [Tooltip("Opacity of the other panels when focusing on one specific panel.")]
    [Range(0f, 1f)] public float unfocusedAlpha = 0.4f;

    [Header("Audio & Haptics")]
    [Tooltip("Audio clip for hover enter.")]
    public AudioClip hoverClip;
    [Tooltip("Audio clip for click confirm.")]
    public AudioClip clickClip;

    [Tooltip("Intensity of the controller vibration on hover.")]
    [Range(0f, 1f)] public float hoverHapticAmplitude = 0.15f;
    [Tooltip("Intensity of the controller vibration on click.")]
    [Range(0f, 1f)] public float clickHapticAmplitude = 0.6f;
    [Tooltip("Duration of the controller vibration in seconds.")]
    public float hapticDuration = 0.1f;

    private VRPanelController currentlyHoveredPanel;
    private AudioSource activeNarrationSource;
    private VRPanelController[] activeControllers;

    /// <summary>
    /// Initializes all configured panels by attaching the runtime interaction controller.
    /// </summary>
    private void Start()
    {
        activeControllers = new VRPanelController[panels.Length];
        for (int i = 0; i < panels.Length; i++)
        {
            var config = panels[i];
            if (config == null || config.panel == null) continue;
            
            // Auto inject text content if defined
            if (config.titleText != null && !string.IsNullOrEmpty(config.titleContent))
                config.titleText.text = config.titleContent;
            
            if (config.descriptionText != null && !string.IsNullOrEmpty(config.descriptionContent))
                config.descriptionText.text = config.descriptionContent;

            var controller = config.panel.gameObject.AddComponent<VRPanelController>();
            controller.Initialize(this, config);
            activeControllers[i] = controller;
        }

        // Start Narration Sequence
        if (narrationClip != null && SoundManager.Instance != null)
        {
            activeNarrationSource = SoundManager.Instance.PlaySFX2D(narrationClip, 0.9f);
            if (activeNarrationSource != null)
            {
                StartCoroutine(NarrationSyncRoutine());
            }
        }

        if (dataStreamParticles != null)
        {
            dataStreamParticles.Play();
        }
    }

    private void Update()
    {
        if (ambientLight != null)
        {
            Color targetColor = IsAnyPanelFocused() ? interactionAmbientColor : normalAmbientColor;
            // Adds a very subtle pulsing mathematical wave over the target color
            float pulse = Mathf.Sin(Time.unscaledTime * 2f) * 0.1f + 0.9f;
            targetColor *= pulse;
            ambientLight.color = Color.Lerp(ambientLight.color, targetColor, Time.unscaledDeltaTime * ambientTransitionSpeed);
        }
    }

    /// <summary>
    /// A masterful complex sequence that ensures perfect timing alignment with audio frames.
    /// </summary>
    private IEnumerator NarrationSyncRoutine()
    {
        while (activeNarrationSource != null && activeNarrationSource.isPlaying)
        {
            float currentTime = activeNarrationSource.time;

            for (int i = 0; i < activeControllers.Length; i++)
            {
                if (activeControllers[i] == null) continue;

                // Check and apply timings
                bool shouldHighlight = false;
                foreach (var timing in narrationTimings)
                {
                    if (timing.panelIndex == i && currentTime >= timing.startTime && currentTime <= timing.endTime)
                    {
                        shouldHighlight = true;
                        break;
                    }
                }
                
                activeControllers[i].SetNarrationHighlight(shouldHighlight);
            }

            yield return null;
        }

        // Reset all narration highlights cleanly just in case
        foreach (var controller in activeControllers)
        {
            if (controller != null) controller.SetNarrationHighlight(false);
        }
    }
    
    /// <summary>
    /// Informs the manager which panel is exclusively focused.
    /// </summary>
    public void SetHoveredPanel(VRPanelController panel)
    {
        currentlyHoveredPanel = panel;
    }

    /// <summary>
    /// Unregisters the panel only if it's currently marked as focused.
    /// </summary>
    public void ClearHoveredPanel(VRPanelController panel)
    {
        if (currentlyHoveredPanel == panel)
        {
            currentlyHoveredPanel = null;
        }
    }
    
    /// <summary>
    /// Checks if a global active hover selection exists to inform opacity logic.
    /// </summary>
    public bool IsAnyPanelFocused()
    {
        return currentlyHoveredPanel != null;
    }

    /// <summary>
    /// Plays feedback and loads the target scene using the transition manager.
    /// </summary>
    public void ExecuteClick(string targetScene)
    {
        PlaySound(clickClip);
        TriggerHapticFeedback(clickHapticAmplitude, hapticDuration);
        if (SceneTransitionManager.Instance != null && !string.IsNullOrEmpty(targetScene))
        {
            SceneTransitionManager.Instance.LoadSceneSmooth(targetScene);
        }
        else if (!string.IsNullOrEmpty(targetScene))
        {
            // Fallback just in case SceneTransitionManager is missing
            UnityEngine.SceneManagement.SceneManager.LoadScene(targetScene);
        }
    }

    /// <summary>
    /// Plays an assigned audio clip globally through the SoundManager.
    /// </summary>
    public void PlaySound(AudioClip clip)
    {
        if (clip != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX2D(clip, 1f);
        }
    }

    /// <summary>
    /// Sends a haptic vibration impulse to all active XR controllers cleanly.
    /// </summary>
    public void TriggerHapticFeedback(float amplitude, float duration)
    {
        if (amplitude <= 0f) return;
        
        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller, devices);

        foreach (var device in devices)
        {
            if (device.TryGetHapticCapabilities(out HapticCapabilities capabilities) && capabilities.supportsImpulse)
            {
                device.SendHapticImpulse(0u, amplitude, duration);
            }
        }
    }
}

/// <summary>
/// Runtime UI controller responsible for tracking standard EventSystem calls and interpolating visuals smoothly.
/// </summary>
public class VRPanelController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    private Menu menuManager;
    private string targetScene;
    private RectTransform rectTransform;
    private Transform icon3D;
    
    private Graphic panelGraphic;
    private Material panelMaterial;
    private CanvasGroup fadeGroup;

    private Vector3 defaultPosition;
    private Vector3 defaultScale;
    private Vector3 defaultIconLocalPos;
    
    private Vector3 targetPosition;
    private Vector3 targetScale;
    private float targetHighlight = 0f;
    private float targetAlpha = 1f;

    private float currentHighlight = 0f;
    private float currentAlpha = 1f;

    private bool isHovered = false;
    private bool isPressed = false;
    private bool isNarrationHighlighted = false;
    private float iconTimeOffset = 0f;

    /// <summary>
    /// Programmatically triggers the visual cue without overriding real hover events.
    /// </summary>
    public void SetNarrationHighlight(bool active)
    {
        isNarrationHighlighted = active;
        UpdateTargetState();
    }

    /// <summary>
    /// Registers the manager references and caches the starting anchored values.
    /// </summary>
    public void Initialize(Menu manager, Menu.VRPanelConfig config)
    {
        menuManager = manager;
        targetScene = config.targetScene;
        rectTransform = config.panel;
        icon3D = config.icon3D;

        defaultPosition = rectTransform.anchoredPosition3D;
        defaultScale = rectTransform.localScale;

        targetPosition = defaultPosition;
        targetScale = defaultScale;
        
        if (icon3D != null) defaultIconLocalPos = icon3D.localPosition;
        
        iconTimeOffset = UnityEngine.Random.Range(0f, 10f); // Organic desynchronized bobbing
        
        fadeGroup = GetComponent<CanvasGroup>();
        if (fadeGroup == null)
        {
            fadeGroup = gameObject.AddComponent<CanvasGroup>();
        }

        panelGraphic = GetComponent<Graphic>();
        if (panelGraphic != null)
        {
            Shader highlightShader = Shader.Find("UI/VRPanelHighlight");
            if (highlightShader != null)
            {
                panelMaterial = new Material(highlightShader);
                panelMaterial.CopyPropertiesFromMaterial(panelGraphic.material);
                panelGraphic.material = panelMaterial;
            }
            else
            {
                Debug.LogWarning("VRPanelHighlight shader not found.");
            }
        }
        else
        {
            Debug.LogWarning($"VRPanelController requires a Graphic component (Image/Text) to catch raycasts on {gameObject.name}");
        }
    }
    
    /// <summary>
    /// Standardized verification to guarantee it only triggers when interacting with Left Click mappings (XR standard trigger).
    /// </summary>
    private bool IsValidXRInteraction(PointerEventData eventData)
    {
        return eventData.button == PointerEventData.InputButton.Left;
    }

    /// <summary>
    /// Exponential Lerp calculation of UI values every frame towards their desired interaction state for max responsiveness.
    /// </summary>
    private void Update()
    {
        if (rectTransform == null) return;
        
        UpdateTargetState();
        
        // Frame-rate independent exponential interpolation constant 
        // Example: Exp(-speed * dt) guarantees the curve speed stays identical across 30fps and 144fps.
        float t = 1f - Mathf.Exp(-menuManager.smoothSpeed * Time.unscaledDeltaTime);
        
        rectTransform.anchoredPosition3D = Vector3.Lerp(rectTransform.anchoredPosition3D, targetPosition, t);
        rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, targetScale, t);

        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, t);
        if (fadeGroup != null) fadeGroup.alpha = currentAlpha;

        if (panelMaterial != null)
        {
            currentHighlight = Mathf.Lerp(currentHighlight, targetHighlight, t);
            panelMaterial.SetFloat("_HighlightAmount", currentHighlight);
            
            float aspect = rectTransform.rect.width / Mathf.Max(rectTransform.rect.height, 1f);
            if (aspect > 0 && !float.IsInfinity(aspect)) 
            {
                panelMaterial.SetFloat("_AspectRatio", aspect);
            }
        }

        // Manage floating 3D Icon Physics independently of UI
        if (icon3D != null)
        {
            float bobSpeed = (isHovered || isNarrationHighlighted) ? 3f : 1.5f;
            float bobHeight = (isHovered || isNarrationHighlighted) ? 0.08f : 0.03f;
            float rotSpeed = (isHovered || isNarrationHighlighted) ? 60f : 20f;
            
            // Continuous rotational flow
            icon3D.Rotate(Vector3.up * (rotSpeed * Time.unscaledDeltaTime), Space.World);
            
            // Smooth sinusoidal vertical bob based on time offset
            float newY = Mathf.Sin((Time.unscaledTime + iconTimeOffset) * bobSpeed) * bobHeight;
            // The icon transform is childed to the UI Rect, local position applies the offset directly
            icon3D.localPosition = new Vector3(defaultIconLocalPos.x, defaultIconLocalPos.y + (newY * 100f), defaultIconLocalPos.z); // scaled up for UI space
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        menuManager.SetHoveredPanel(this);
        menuManager.PlaySound(menuManager.hoverClip);
        menuManager.TriggerHapticFeedback(menuManager.hoverHapticAmplitude, menuManager.hapticDuration);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        isPressed = false;
        menuManager.ClearHoveredPanel(this);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsValidXRInteraction(eventData)) return;
        isPressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!IsValidXRInteraction(eventData)) return;
        isPressed = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsValidXRInteraction(eventData)) return;
        if (!string.IsNullOrEmpty(targetScene))
        {
            menuManager.ExecuteClick(targetScene);
        }
    }

    /// <summary>
    /// Re-evaluates local scales, positional offsets, alphas, and shader highlights based on pointer interactions.
    /// </summary>
    private void UpdateTargetState()
    {
        if (isPressed)
        {
            targetScale = defaultScale * menuManager.pressScale;
            targetPosition = defaultPosition + new Vector3(0, menuManager.hoverTranslationY * 0.4f, 0);
            targetHighlight = 1.0f;
            targetAlpha = 1.0f;
        }
        else if (isHovered)
        {
            targetScale = defaultScale * menuManager.hoverScale;
            targetPosition = defaultPosition + new Vector3(0, menuManager.hoverTranslationY, 0);
            targetHighlight = 1.0f;
            targetAlpha = 1.0f;
        }
        else if (isNarrationHighlighted)
        {
            // Pseudo-hover visual cue without blocking real focus overrides
            targetScale = defaultScale * 1.02f;
            targetPosition = defaultPosition + new Vector3(0, menuManager.hoverTranslationY * 0.5f, 0);
            targetHighlight = 1.0f;
            targetAlpha = 1.0f;
        }
        else
        {
            targetScale = defaultScale;
            targetPosition = defaultPosition;
            targetHighlight = 0f;
            
            if (menuManager.IsAnyPanelFocused())
            {
                targetAlpha = menuManager.unfocusedAlpha;
            }
            else
            {
                targetAlpha = 1f;
            }
        }
    }
}
