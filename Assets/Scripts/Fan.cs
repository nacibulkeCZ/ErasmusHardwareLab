using UnityEngine;
using UnityEngine.Events;

public class Fan : MonoBehaviour
{
    [Header("Rotor Configuration")]
    [SerializeField] private Transform rotorObject;
    [SerializeField] private Vector3 rotationAxis = Vector3.forward;
    [SerializeField] private bool allowReverseRotation = true;

    [Header("Operation Settings")]
    [SerializeField] private bool startOnAwake = false;
    [SerializeField] private float defaultTargetSpeed = 500f;
    [SerializeField] private float accelerationRate = 200f;
    [SerializeField] private float decelerationRate = 150f;

    [Header("Events")]
    public UnityEvent onFanStarted;
    public UnityEvent onFanStopped;
    public UnityEvent onTargetSpeedReached;

    private bool isRunning;
    private float currentSpeed;
    private float currentTargetSpeed;
    private float speedMultiplier = 1f;
    private bool hasReachedTargetSpeed;

    public bool IsRunning => isRunning;
    public float CurrentSpeed => currentSpeed;

    private void Awake()
    {
        if (rotorObject == null)
        {
            rotorObject = transform;
        }

        currentTargetSpeed = defaultTargetSpeed;
    }

    private void Start()
    {
        if (startOnAwake)
        {
            StartFan();
        }
    }

    private void Update()
    {
        ProcessSpeed();
        ProcessRotation();
    }

    private void ProcessSpeed()
    {
        float desiredSpeed = isRunning ? (currentTargetSpeed * speedMultiplier) : 0f;

        if (Mathf.Approximately(currentSpeed, desiredSpeed))
        {
            if (isRunning && !hasReachedTargetSpeed)
            {
                hasReachedTargetSpeed = true;
                onTargetSpeedReached?.Invoke();
            }
            return;
        }

        hasReachedTargetSpeed = false;

        float rate = (Mathf.Abs(currentSpeed) < Mathf.Abs(desiredSpeed)) ? accelerationRate : decelerationRate;
        currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, rate * Time.deltaTime);
    }

    private void ProcessRotation()
    {
        if (Mathf.Abs(currentSpeed) > 0.01f && rotorObject != null)
        {
            rotorObject.Rotate(rotationAxis.normalized, currentSpeed * Time.deltaTime, Space.Self);
        }
    }

    public void StartFan()
    {
        if (isRunning) return;

        isRunning = true;
        hasReachedTargetSpeed = false;
        onFanStarted?.Invoke();
    }

    public void StopFan()
    {
        if (!isRunning) return;

        isRunning = false;
        onFanStopped?.Invoke();
    }

    public void ToggleFan()
    {
        if (isRunning) StopFan();
        else StartFan();
    }

    public void SetSpeed(float newSpeed)
    {
        currentTargetSpeed = allowReverseRotation ? newSpeed : Mathf.Max(0f, newSpeed);
        hasReachedTargetSpeed = false;
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = multiplier;
        hasReachedTargetSpeed = false;
    }

    public void ReverseDirection()
    {
        if (allowReverseRotation)
        {
            currentTargetSpeed *= -1f;
            hasReachedTargetSpeed = false;
        }
    }

    public void ImmediateStop()
    {
        isRunning = false;
        currentSpeed = 0f;
        onFanStopped?.Invoke();
    }
}
