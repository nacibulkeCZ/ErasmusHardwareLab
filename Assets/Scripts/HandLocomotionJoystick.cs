using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands; // Requires XR Hands Package
using UnityEngine.XR.Management;
using UnityEngine.XR.Interaction.Toolkit; // Requires XRI 3.x
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

/// <summary>
/// Implements a "Virtual Joystick" for XR Hands.
/// Pinch (Index+Thumb) to anchor the joystick, then move your hand to drive movement.
/// </summary>
public class HandLocomotionJoystick : LocomotionProvider
{
    [Header("Hand Settings")]
    [Tooltip("Which hand controls the movement?")]
    public Handedness handType = Handedness.Left;
    
    [Tooltip("Distance between thumb and index tip to consider as a 'Pinch'")]
    public float pinchThreshold = 0.02f;

    [Header("Movement Configuration")]
    [Tooltip("How far you have to drag your hand to reach max speed (in meters)")]
    public float maxDragDistance = 0.15f; 
    
    [Tooltip("Movement speed multiplier")]
    public float moveSpeed = 2.0f;
    
    [Tooltip("Deadzone to prevent drift when holding still")]
    public float deadzone = 0.02f;

    [Header("Orientation")]
    [Tooltip("The object that defines 'Forward' (usually the Main Camera)")]
    public Transform forwardSource;
    
    [Tooltip("The XR Origin (Reference Frame)")]
    public Transform referenceFrame;

    [Header("Visual Feedback")]
    [Tooltip("Prefab instantiated at the start point. Used for both Base and Knob.")]
    public GameObject joystickVisualPrefab; 
    
    [Tooltip("Material used for the line connecting base and knob.")]
    public Material lineMaterial;
    
    [Tooltip("Color when movement is active (outside deadzone).")]
    public Color activeColor = Color.green;
    
    [Tooltip("Color when within deadzone.")]
    public Color deadzoneColor = Color.gray;

    // Internal State
    private XRHandSubsystem _handSubsystem;
    private bool _isPinching = false;
    private Vector3 _anchorPosition; // The point in space where we started pinching
    private Vector3 _currentHandPosition;

    // Visual instances
    private GameObject _visualBase;
    private GameObject _visualKnob;
    private LineRenderer _lineRenderer;

    protected override void Awake()
    {
        base.Awake();
        if (forwardSource == null && Camera.main != null)
            forwardSource = Camera.main.transform;
    }

    protected void Start()
    {
        // Try to find the subsystem if it wasn't initialized in Awake
        GetHandSubsystem();
    }

    private void Update()
    {
        if (_handSubsystem == null)
        {
            GetHandSubsystem();
            return;
        }

        // 1. Get Hand Data
        XRHand hand = (handType == Handedness.Left) ? _handSubsystem.leftHand : _handSubsystem.rightHand;

        if (!hand.isTracked)
        {
            if (_isPinching) ReleaseJoystick();
            return;
        }

        // Get Joint Positions (Index Tip & Thumb Tip for pinch detection)
        // Note: Using "Root" or "Palm" for the actual movement origin is often more stable than tips.
        var palmJoint = hand.GetJoint(XRHandJointID.Palm);
        var indexTip = hand.GetJoint(XRHandJointID.IndexTip);
        var thumbTip = hand.GetJoint(XRHandJointID.ThumbTip);

        if (!palmJoint.TryGetPose(out Pose palmPose) || 
            !indexTip.TryGetPose(out Pose indexPose) || 
            !thumbTip.TryGetPose(out Pose thumbPose))
        {
            return;
        }

        // 2. Detect Pinch
        float pinchDist = Vector3.Distance(indexPose.position, thumbPose.position);
        bool currentlyPinching = pinchDist < pinchThreshold;

        _currentHandPosition = palmPose.position;

        // 3. State Machine
        if (currentlyPinching)
        {
            if (!_isPinching)
            {
                // Just started pinching -> Set Anchor
                StartJoystick(_currentHandPosition);
            }
            
            // Logic: Process Movement
            ProcessMovement();
        }
        else
        {
            if (_isPinching)
            {
                // Just released -> Stop
                ReleaseJoystick();
            }
        }
    }

    private void StartJoystick(Vector3 startPos)
    {
        _isPinching = true;
        _anchorPosition = startPos;
        
        // Spawn Visuals
        if (joystickVisualPrefab != null)
        {
            // 1. Base (Anchor)
            _visualBase = Instantiate(joystickVisualPrefab, _anchorPosition, Quaternion.identity);
            
            // 2. Knob (Handle) - scaled down slightly
            _visualKnob = Instantiate(joystickVisualPrefab, _anchorPosition, Quaternion.identity);
            _visualKnob.transform.localScale *= 0.7f;

            // 3. Line Renderer setup
            _lineRenderer = _visualBase.AddComponent<LineRenderer>();
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.positionCount = 2;
            _lineRenderer.startWidth = 0.005f;
            _lineRenderer.endWidth = 0.005f;
            
            if (lineMaterial != null)
            {
                _lineRenderer.material = lineMaterial;
            }
            else
            {
                // Fallback simple material if none provided
                _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            }
        }
    }

    private void ReleaseJoystick()
    {
        _isPinching = false;
        
        // Cleanup Visuals
        if (_visualBase != null) Destroy(_visualBase);
        if (_visualKnob != null) Destroy(_visualKnob);
        // Line renderer is on base, so it gets destroyed with it
    }

    private void ProcessMovement()
    {
        if (locomotionPhase != LocomotionPhase.Idle && locomotionPhase != LocomotionPhase.Moving)
            return;

        // Calculate drag vector (Hand - Anchor)
        Vector3 dragVector = _currentHandPosition - _anchorPosition;

        // Update Visuals before logic
        UpdateVisuals(dragVector);

        // Apply Deadzone
        if (dragVector.magnitude < deadzone) return;

        // Calculate Input Intensity (0 to 1 based on MaxDragDistance)
        float inputMagnitude = Mathf.Clamp01(dragVector.magnitude / maxDragDistance);
        
        // Normalize direction
        Vector3 dragDirection = dragVector.normalized;

        // Convert world drag vector to "Player-Relative" joystick input (X, Y)
        // We project the drag vector onto the camera's flat plane.
        
        Vector3 forward = forwardSource.forward;
        Vector3 right = forwardSource.right;
        
        // Flatten to XZ plane (ignore looking up/down affecting speed)
        forward.y = 0; right.y = 0;
        forward.Normalize(); right.Normalize();

        // Dot product determines how much of the drag is Forward vs Right
        float moveX = Vector3.Dot(dragDirection, right) * inputMagnitude;
        float moveZ = Vector3.Dot(dragDirection, forward) * inputMagnitude;

        Vector2 inputVector = new Vector2(moveX, moveZ);

        // Send to XRI Locomotion System
        MoveRig(inputVector);
    }

    private void UpdateVisuals(Vector3 dragVector)
    {
        if (_visualBase == null || _visualKnob == null) return;

        // Clamp visual handle to max distance so it doesn't fly away too far
        Vector3 clampedOffset = Vector3.ClampMagnitude(dragVector, maxDragDistance);
        _visualKnob.transform.position = _anchorPosition + clampedOffset;

        // Update Line
        _lineRenderer.SetPosition(0, _anchorPosition);
        _lineRenderer.SetPosition(1, _visualKnob.transform.position);

        // Update Color based on Deadzone
        bool isActive = dragVector.magnitude > deadzone;
        Color targetColor = isActive ? activeColor : deadzoneColor;

        var knobRenderer = _visualKnob.GetComponent<Renderer>();
        if (knobRenderer != null)
        {
            knobRenderer.material.color = targetColor;
        }
    }

    private void MoveRig(Vector2 input)
    {
        // Ensure we have permission to move
        if (BeginLocomotion())
        {
            // Apply speed multiplier
            float speed = moveSpeed * Time.deltaTime;
            
            // Calculate translation in World Space relative to the Rig's orientation (or Camera's)
            
            Vector3 movement = (forwardSource.right * input.x + forwardSource.forward * input.y) * speed;
            movement.y = 0; // Keep movement grounded

            // CharacterControllerDriver or direct Transform move
            CharacterController characterController = referenceFrame.GetComponent<CharacterController>();
            if (characterController != null)
            {
                characterController.Move(movement);
            }
            else
            {
                referenceFrame.Translate(movement, Space.World);
            }

            EndLocomotion();
        }
    }

    private void GetHandSubsystem()
    {
        var subsystems = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);
        if (subsystems.Count > 0)
        {
            _handSubsystem = subsystems[0];
            if (!_handSubsystem.running)
            {
                _handSubsystem.Start();
            }
        }
    }
}