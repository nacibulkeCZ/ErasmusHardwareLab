using UnityEngine;

public class FanBladesManager : MonoBehaviour
{
    public Transform fanBlades;

    [Header("Fan blades operating behavior")]
    public bool fanIsOn;
    public float targetFanSpeed = 1000f;
    public float motorAcceleration = 1500f;

    [Header("Fan blades physics")]
    public bool usePhysics = true;
    [Tooltip("How strongly blade speed is pulled toward housing angular speed (1/s).")]
    [Min(0f)] public float friction = 1.5f;
    [Tooltip("Maximum blade speed change transferred from the body per second (deg/s^2).")]
    [Min(0f)] public float addedVelocityClamp = 500f;

    private float currentSpeed;
    private float bladeAbsoluteSpeed;
    private Quaternion lastRotation;

    private void Start()
    {
        // prevents physics explosion on initialization
        lastRotation = transform.rotation;
    }

    void FixedUpdate()
    {
        if (fanIsOn)
        {
            // accelerate towards target speed
            currentSpeed = Mathf.MoveTowards(currentSpeed, -targetFanSpeed, motorAcceleration * Time.fixedDeltaTime);
            bladeAbsoluteSpeed = currentSpeed;
            return;
        }
        else if (!fanIsOn && !usePhysics)
        {
            // stop the motor without physics
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, motorAcceleration * Time.fixedDeltaTime);
            bladeAbsoluteSpeed = currentSpeed;
            return;
        }

        if (!usePhysics) {
            // prevents physics explosions
            lastRotation = transform.rotation;
            return;
        }

        // calculate physics (skipped when fan is on!!)
        // when OFF, blades accelerate as the fan body is moved with
        // when ON, blades decelerate from the speed they are at
        float dt = Mathf.Max(Time.fixedDeltaTime, 0.0001f);
        Quaternion deltaRotation = transform.rotation * Quaternion.Inverse(lastRotation);
        deltaRotation.ToAngleAxis(out float deltaAngle, out Vector3 deltaAxis);

        if (deltaAngle > 180f)
            deltaAngle -= 360f;

        Vector3 axisWorld = fanBlades != null ? fanBlades.forward : transform.forward;
        float signedDeltaAngle = deltaAngle * Mathf.Sign(Vector3.Dot(deltaAxis, axisWorld));
        float housingAngularSpeed = signedDeltaAngle / dt;

        float desiredSpeedChange = (housingAngularSpeed - bladeAbsoluteSpeed) * friction * dt;
        float maxSpeedChange = addedVelocityClamp * dt;
        bladeAbsoluteSpeed += Mathf.Clamp(desiredSpeedChange, -maxSpeedChange, maxSpeedChange);
        currentSpeed = bladeAbsoluteSpeed - housingAngularSpeed;

        lastRotation = transform.rotation;
    }

    void Update()
    {
        if (fanBlades == null)
            return;

        // rotate blades by current speed (calculated by settings or physics)
        fanBlades.Rotate(0f, 0f, currentSpeed * Time.deltaTime, Space.Self);
    }

    public void StartFan()
    {
        
    }
}
