using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class FuturisticMenuParticles : MonoBehaviour
{
    [Header("Placement")]
    [SerializeField] private bool placeInFrontOfCameraOnStart = true;
    [SerializeField] private float distanceFromCamera = 2.8f;
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 0.2f, 0f);

    [Header("Particle Field")]
    [SerializeField] private Vector3 volumeSize = new Vector3(5f, 2.6f, 1.6f);
    [SerializeField] private int maxParticles = 260;
    [SerializeField] private float emissionRate = 24f;
    [SerializeField] private float particleLifetime = 12f;
    [SerializeField] private Vector2 particleSizeRange = new Vector2(0.008f, 0.028f);
    [SerializeField] private Vector2 particleSpeedRange = new Vector2(0.015f, 0.08f);

    [Header("Motion")]
    [SerializeField] private Vector3 airDrift = new Vector3(0.015f, 0.035f, 0.005f);
    [SerializeField] private float noiseStrength = 0.08f;
    [SerializeField] private float noiseFrequency = 0.18f;
    [SerializeField] private float noiseScrollSpeed = 0.12f;

    [Header("Color")]
    [SerializeField] private Color particleColor = new Color(0.1f, 0.75f, 1f, 0.42f);

    private ParticleSystem particleSystemComponent;
    private Material runtimeMaterial;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private void Awake()
    {
        particleSystemComponent = GetComponent<ParticleSystem>();
        ConfigureParticleSystem();
    }

    private void Start()
    {
        if (placeInFrontOfCameraOnStart)
        {
            PlaceInFrontOfCamera();
        }

        if (!particleSystemComponent.isPlaying)
        {
            particleSystemComponent.Play();
        }
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }
    }

    [ContextMenu("Configure Particle System")]
    private void ConfigureParticleSystem()
    {
        if (particleSystemComponent == null)
        {
            particleSystemComponent = GetComponent<ParticleSystem>();
        }

        var main = particleSystemComponent.main;
        main.loop = true;
        main.playOnAwake = true;
        main.maxParticles = maxParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = particleLifetime;
        main.startSpeed = new ParticleSystem.MinMaxCurve(particleSpeedRange.x, particleSpeedRange.y);
        main.startSize = new ParticleSystem.MinMaxCurve(particleSizeRange.x, particleSizeRange.y);
        main.startColor = particleColor;
        main.gravityModifier = 0f;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var emission = particleSystemComponent.emission;
        emission.enabled = true;
        emission.rateOverTime = emissionRate;

        var shape = particleSystemComponent.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = volumeSize;
        shape.randomDirectionAmount = 0.45f;

        var velocity = particleSystemComponent.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = airDrift.x;
        velocity.y = airDrift.y;
        velocity.z = airDrift.z;

        var colorOverLifetime = particleSystemComponent.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(particleColor, 0f),
                new GradientColorKey(new Color(0.45f, 0.95f, 1f), 0.55f),
                new GradientColorKey(particleColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(particleColor.a, 0.2f),
                new GradientAlphaKey(particleColor.a * 0.75f, 0.75f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        var sizeOverLifetime = particleSystemComponent.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.35f),
            new Keyframe(0.25f, 1f),
            new Keyframe(1f, 0.55f)
        );
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var noise = particleSystemComponent.noise;
        noise.enabled = true;
        noise.strength = noiseStrength;
        noise.frequency = noiseFrequency;
        noise.scrollSpeed = noiseScrollSpeed;
        noise.quality = ParticleSystemNoiseQuality.Medium;

        var renderer = particleSystemComponent.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.sortingOrder = -10;
        renderer.minParticleSize = 0.001f;
        renderer.maxParticleSize = 0.06f;
        renderer.sharedMaterial = GetParticleMaterial();
    }

    private Material GetParticleMaterial()
    {
        if (runtimeMaterial != null)
        {
            return runtimeMaterial;
        }

        Shader shader = Shader.Find("Custom/FuturisticParticleUnlit");
        if (shader == null)
        {
            Debug.LogWarning("Custom/FuturisticParticleUnlit shader was not found. Reimport Assets/Shaders/FuturisticParticleUnlit.shader.");
            return null;
        }

        runtimeMaterial = new Material(shader)
        {
            name = "Runtime Futuristic Blue Particles"
        };

        if (runtimeMaterial.HasProperty(BaseColorId))
        {
            runtimeMaterial.SetColor(BaseColorId, Color.white);
        }

        if (runtimeMaterial.HasProperty(ColorId))
        {
            runtimeMaterial.SetColor(ColorId, Color.white);
        }

        return runtimeMaterial;
    }

    private void PlaceInFrontOfCamera()
    {
        Camera targetCamera = Camera.main;
        if (targetCamera == null)
        {
            targetCamera = FindFirstObjectByType<Camera>();
        }

        if (targetCamera == null)
        {
            return;
        }

        Transform cameraTransform = targetCamera.transform;
        transform.position = cameraTransform.position
            + cameraTransform.forward * distanceFromCamera
            + cameraTransform.right * localOffset.x
            + cameraTransform.up * localOffset.y
            + cameraTransform.forward * localOffset.z;
        transform.rotation = cameraTransform.rotation;
    }
}
