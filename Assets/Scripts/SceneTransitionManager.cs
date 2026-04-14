using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionManager : MonoBehaviour
{
    private static SceneTransitionManager _instance;
    public static SceneTransitionManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<SceneTransitionManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("SceneTransitionManager");
                    _instance = go.AddComponent<SceneTransitionManager>();
                }
            }
            return _instance;
        }
    }

    [Header("VR Transition settings")]
    public float fadeDuration = 0.5f;
    public Color fadeColor = Color.black;
    public bool use3DFade = true;

    [Header("UI Fallback / Loading")]
    public Canvas transitionCanvas;
    public CanvasGroup fadeGroup;
    public Image fadeImage;
    public GameObject loadingContainer;
    public Image progressBar;
    public TextMeshProUGUI loadingText;

    public bool IsTransitioning { get; private set; } = false;

    private GameObject fadeSphere;
    private Material fadeMaterial;
    private float currentFadeT = 1f; // 1 = fully black, 0 = clear
    private Coroutine activeFadeRoutine;

    private Transform targetCameraTransform;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            // Převedení odkazů na existující persistující instanci bez smazání
            if (_instance.loadingContainer == null && this.loadingContainer != null)
            {
                _instance.loadingContainer = this.loadingContainer;
                _instance.progressBar = this.progressBar;
                _instance.loadingText = this.loadingText;
                _instance.transitionCanvas = this.transitionCanvas;
                _instance.fadeGroup = this.fadeGroup;
                _instance.fadeImage = this.fadeImage;

                if (this.transitionCanvas != null) this.transitionCanvas.transform.SetParent(_instance.transform);
                if (this.loadingContainer != null) this.loadingContainer.transform.SetParent(_instance.transform);

                if (_instance.loadingContainer != null) _instance.loadingContainer.SetActive(false);
                if (_instance.transitionCanvas != null) _instance.transitionCanvas.gameObject.SetActive(false);
            }
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeManagers();
    }

    private void InitializeManagers()
    {
        if (use3DFade) CreateFadeSphere();

        if (transitionCanvas != null) transitionCanvas.gameObject.SetActive(false);
        if (loadingContainer != null) loadingContainer.SetActive(false);

        SetFadeAmount(1f);
    }

    private void CreateFadeSphere()
    {
        if (fadeSphere != null) return;

        fadeSphere = new GameObject("VR_Fade_Sphere");
        DontDestroyOnLoad(fadeSphere);

        MeshFilter mf = fadeSphere.AddComponent<MeshFilter>();
        MeshRenderer mr = fadeSphere.AddComponent<MeshRenderer>();

        // Inversní sféra, abychom viděli zevnitř ven
        GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Mesh mesh = tempCube.GetComponent<MeshFilter>().sharedMesh;

        Vector3[] normals = mesh.normals;
        int[] triangles = mesh.triangles;
        for (int i = 0; i < normals.Length; i++) normals[i] = -normals[i];
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int tempVert = triangles[i];
            triangles[i] = triangles[i + 1];
            triangles[i + 1] = tempVert;
        }

        Mesh invertedMesh = new Mesh();
        invertedMesh.vertices = mesh.vertices;
        invertedMesh.normals = normals;
        invertedMesh.uv = mesh.uv;
        invertedMesh.triangles = triangles;
        mf.mesh = invertedMesh;
        Destroy(tempCube);

        Shader shader = Shader.Find("UI/Default") ?? Shader.Find("Sprites/Default");
        fadeMaterial = new Material(shader);
        fadeMaterial.SetInt("_ZWrite", 0);
        fadeMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        fadeMaterial.renderQueue = 5000;

        Color startColor = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);
        if (fadeMaterial.HasProperty("_Color")) fadeMaterial.SetColor("_Color", startColor);
        if (fadeMaterial.HasProperty("_TintColor")) fadeMaterial.SetColor("_TintColor", startColor);

        mr.material = fadeMaterial;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.allowOcclusionWhenDynamic = false;
        mr.sortingOrder = 32767;

        fadeSphere.layer = LayerMask.NameToLayer("UI");
        fadeSphere.SetActive(false);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        Application.onBeforeRender += UpdatePosition;
        RenderPipelineManager.beginCameraRendering += OnCameraRender;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Application.onBeforeRender -= UpdatePosition;
        RenderPipelineManager.beginCameraRendering -= OnCameraRender;
    }

    private void OnCameraRender(ScriptableRenderContext context, Camera cam)
    {
        if (cam.cameraType == CameraType.Game || cam.cameraType == CameraType.VR)
        {
            targetCameraTransform = cam.transform;
            UpdatePosition();
        }
    }

    private void UpdatePosition()
    {
        if (targetCameraTransform == null)
        {
            Camera cam = Camera.main;
            if (cam == null) cam = FindFirstObjectByType<Camera>();
            if (cam != null) targetCameraTransform = cam.transform;
        }

        if (targetCameraTransform != null)
        {
            if (use3DFade && fadeSphere != null)
            {
                fadeSphere.transform.position = targetCameraTransform.position;
            }

            if (transitionCanvas != null && transitionCanvas.renderMode == RenderMode.WorldSpace)
            {
                transitionCanvas.transform.position = targetCameraTransform.position + targetCameraTransform.forward * 1.5f;
                transitionCanvas.transform.rotation = targetCameraTransform.rotation;
            }
        }
    }

    private void Update()
    {
        UpdatePosition();
    }

    private void LateUpdate()
    {
        UpdatePosition();
    }

    private void Start()
    {
        // Spuštění automatického rozetmívání hned po spuštění hry/manažeru,
        // ale pouze pokud ještě neděláme jinou tranzici (např. jsme ho nevytvořili staticky za běhu pro odchod)
        if (!IsTransitioning)
        {
            SetFadeAmount(1f);
            if (activeFadeRoutine != null) StopCoroutine(activeFadeRoutine);
            activeFadeRoutine = StartCoroutine(FadeRoutine(0f));
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdatePosition();

        if (!IsTransitioning)
        {
            // Okamžité zatmění, aby nevznikl pochybný první frame - perfektní i když není použito LoadSceneSmooth, 
            // ale prostý přechod z např. LoadingScreen do MainMenu
            SetFadeAmount(1f); 
            if (activeFadeRoutine != null) StopCoroutine(activeFadeRoutine);
            activeFadeRoutine = StartCoroutine(FadeRoutine(0f));
        }
    }

    public void LoadSceneSmooth(string sceneName, bool useBackgroundLoading = true)
    {
        if (IsTransitioning) return;
        StartCoroutine(TransitionRoutine(sceneName, useBackgroundLoading));
    }

    private IEnumerator TransitionRoutine(string sceneName, bool useBackgroundLoading)
    {
        IsTransitioning = true;

        if (activeFadeRoutine != null) StopCoroutine(activeFadeRoutine);

        // 1. Zahájení načítání scény asynchronně na pozadí
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        if (SoundManager.Instance != null) SoundManager.Instance.GlobalFadeOut(fadeDuration / 2f); // Cut audio faster

        if (useBackgroundLoading && loadingContainer != null) 
            loadingContainer.SetActive(true);

        // Zapnout prvky pro stmívání dřív, než začneme měnit alpha
        if (use3DFade && fadeSphere != null) fadeSphere.SetActive(true);
        if (!use3DFade && transitionCanvas != null) transitionCanvas.gameObject.SetActive(true);

        float time = 0f;
        float startFade = currentFadeT;
        float targetFade = 1f;
        bool fadeFinished = false;

        // 2. Paralelní ztmavování obrazu a aktualizace průběhu načítání
        while (!fadeFinished || op.progress < 0.9f)
        {
            // Plynulá interpolace clony (SmoothStep)
            if (time < fadeDuration)
            {
                time += Time.unscaledDeltaTime;
                float rawT = Mathf.Clamp01(time / fadeDuration);
                float smoothT = rawT * rawT * (3f - 2f * rawT);
                currentFadeT = Mathf.Lerp(startFade, targetFade, smoothT);
                SetFadeAmount(currentFadeT);
            }
            else if (!fadeFinished)
            {
                currentFadeT = targetFade;
                SetFadeAmount(currentFadeT);
                fadeFinished = true;
            }

            // UI feedback načítání
            if (useBackgroundLoading)
            {
                // Mírné zjemnění nárůstu pruhu z reálného progresu (0 - 0.9) na (0 - 1)
                float progress = Mathf.Clamp01(op.progress / 0.9f);
                if (progressBar != null) progressBar.fillAmount = Mathf.MoveTowards(progressBar.fillAmount, progress, Time.unscaledDeltaTime * 2f);
                if (loadingText != null) loadingText.text = $"Loading... {(progress * 100):0}%";
            }

            UpdatePosition();
            yield return null;
        }

        // Pojistka plného zatmění pro nekompromisní zakrytí transitionu
        currentFadeT = 1f;
        SetFadeAmount(1f);

        // Případný finální kosmetický wait, když se dosáhne 100% u progres baru
        if (useBackgroundLoading)
        {
            if (progressBar != null) progressBar.fillAmount = 1f;
            if (loadingText != null) loadingText.text = "Loading... 100%";

            float waitTime = 0.25f; // Krátká pauza, ať uživatel vůbec mrkne na těch 100%
            while (waitTime > 0f)
            {
                UpdatePosition();
                waitTime -= Time.unscaledDeltaTime;
                yield return null;
            }

            if (loadingContainer != null) loadingContainer.SetActive(false);
        }

        // 3. Aktivace nové nažhavené scény
        op.allowSceneActivation = true;

        while (!op.isDone)
        {
            UpdatePosition();
            yield return null;
        }

        // Zabezpečení fyziky a vr trackingu před zrušením clony (čekání pár snímků v nové scéně)
        for (int i = 0; i < 3; i++)
        {
            yield return null;
            UpdatePosition();
        }

        // 4. Rozetmívání v nové scéně
        if (SoundManager.Instance != null) SoundManager.Instance.GlobalFadeIn(fadeDuration);
        yield return FadeRoutine(0f);

        IsTransitioning = false;
    }

    private IEnumerator FadeRoutine(float target)
    {
        if (use3DFade && fadeSphere != null) fadeSphere.SetActive(true);
        if (transitionCanvas != null && !use3DFade) transitionCanvas.gameObject.SetActive(true);

        float start = currentFadeT;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.unscaledDeltaTime;
            float rawT = Mathf.Clamp01(time / fadeDuration);
            float smoothT = rawT * rawT * (3f - 2f * rawT); // Jemný SmoothStep vzoreček
            currentFadeT = Mathf.Lerp(start, target, smoothT);

            SetFadeAmount(currentFadeT);
            yield return null;
        }

        currentFadeT = target;
        SetFadeAmount(currentFadeT);

        if (currentFadeT <= 0f)
        {
            // Vypnout elementy, pokud už nejsou vidět pro max úsporu framů
            if (use3DFade && fadeSphere != null) fadeSphere.SetActive(false);
            if (!use3DFade && transitionCanvas != null) transitionCanvas.gameObject.SetActive(false);
        }
    }

    private void SetFadeAmount(float t)
    {
        currentFadeT = t;

        if (use3DFade && fadeSphere != null)
        {
            // Interpolace velikosti: 
            // - Když je t=1 (tmavý), sféra je malá a perfektně obejme kameru k vykrytí i toho nejhoršího stříhání 
            // - Když se roztmívá (t=0), sféra se roztahuje velkosně ven (jako portálový efekt)
            float targetScale = Mathf.Lerp(50f, 1.5f, t); 
            fadeSphere.transform.localScale = Vector3.one * targetScale;

            if (fadeMaterial != null)
            {
                Color c = new Color(fadeColor.r, fadeColor.g, fadeColor.b, t);
                if (fadeMaterial.HasProperty("_Color")) fadeMaterial.SetColor("_Color", c);
                if (fadeMaterial.HasProperty("_TintColor")) fadeMaterial.SetColor("_TintColor", c);
            }

            if (targetCameraTransform != null)
            {
                // Přidá malinké kroutivé rotace kolem hráče při otevírání
                fadeSphere.transform.rotation = targetCameraTransform.rotation * Quaternion.Euler(t * 15f, t * 90f, t * 25f);
            }
        }
        else if (fadeGroup != null)
        {
            fadeGroup.alpha = t;
        }
    }
}
