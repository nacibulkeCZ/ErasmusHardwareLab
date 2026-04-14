using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// A flawless, hyper-responsive globally persistent Sound Engine for XR and standard rendering.
/// Handles Object Pooling, 2D/3D dynamic spatial blending, Audio Pitch randomization, completely 
/// seamless double-buffer music crossfading, and exponential mathematical volume dampening.
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Serializable]
    public struct SceneMusic
    {
        public string sceneName;
        public AudioClip bgm;
    }

    [Serializable]
    public struct SoundOptions
    {
        [Range(0f, 1f)] public float volume;
        [Range(-3f, 3f)] public float pitch;
        [Range(0f, 1f)] public float pitchRandomness;
        [Range(0f, 1f)] public float spatialBlend; // 0 = 2D, 1 = 3D
        public float minDistance;
        public float maxDistance;
        public bool loop;
        public AudioRolloffMode rolloffMode;

        // Factory defaults for ultra-fast spawning
        public static SoundOptions Default2D(float vol = 1f) => new SoundOptions { volume = vol, pitch = 1f, pitchRandomness = 0f, spatialBlend = 0f, minDistance = 1f, maxDistance = 500f, loop = false, rolloffMode = AudioRolloffMode.Logarithmic };
        public static SoundOptions Default3D(float vol = 1f) => new SoundOptions { volume = vol, pitch = 1f, pitchRandomness = 0.05f, spatialBlend = 1f, minDistance = 1.5f, maxDistance = 25f, loop = false, rolloffMode = AudioRolloffMode.Logarithmic };
        public static SoundOptions SoftUI() => new SoundOptions { volume = 0.8f, pitch = 1f, pitchRandomness = 0.03f, spatialBlend = 0f, minDistance = 1f, maxDistance = 500f, loop = false, rolloffMode = AudioRolloffMode.Logarithmic };
    }

    [Header("Global Audio Settings")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    
    [Header("Scene Background Music")]
    [Tooltip("Map scene names to specific BGM clips for automatic playback")]
    public List<SceneMusic> sceneMusicMap = new List<SceneMusic>();
    public float sceneMusicCrossfadeDuration = 1.5f;

    [Header("Optimization")]
    [Tooltip("Initial number of AudioSources populated into the memory pool.")]
    public int initialSFXPoolSize = 15;

    // Music Double-Buffering for Seamless Crossfading 
    private AudioSource[] musicSources = new AudioSource[2];
    private int activeMusicSourceIndex = 0;

    // SFX Pooling
    private List<AudioSource> sfxPool = new List<AudioSource>();
    private GameObject sfxPoolContainer;

    // Fade states
    private float targetMasterVolume = 1f;
    private float currentMasterVolume = 1f;
    private float fadeSpeed = 10f; // Exponential speed for global fades

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeAudioSystem();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Check for specific scene music and transition into it naturally
        foreach (var mapping in sceneMusicMap)
        {
            if (mapping.sceneName == scene.name)
            {
                if (mapping.bgm != null)
                {
                    PlayMusic(mapping.bgm, sceneMusicCrossfadeDuration);
                }
                else
                {
                    StopMusic(sceneMusicCrossfadeDuration);
                }
                break;
            }
        }
    }

    private void InitializeAudioSystem()
    {
        // 1. Setup Dual-Channel Music Deck
        for (int i = 0; i < 2; i++)
        {
            GameObject bgmObj = new GameObject($"BGM_Channel_{i}");
            bgmObj.transform.SetParent(transform);
            musicSources[i] = bgmObj.AddComponent<AudioSource>();
            musicSources[i].loop = true;
            musicSources[i].playOnAwake = false;
            musicSources[i].spatialBlend = 0f; // Music is always mathematically strictly 2D
        }

        // 2. Setup SFX Memory Pool
        sfxPoolContainer = new GameObject("SFX_MemoryPool");
        sfxPoolContainer.transform.SetParent(transform);

        for (int i = 0; i < initialSFXPoolSize; i++)
        {
            CreateNewPoolSource();
        }

        targetMasterVolume = masterVolume;
        currentMasterVolume = masterVolume;
    }

    private AudioSource CreateNewPoolSource()
    {
        GameObject sfxObj = new GameObject($"SFX_Source_{sfxPool.Count}");
        sfxObj.transform.SetParent(sfxPoolContainer.transform);
        AudioSource source = sfxObj.AddComponent<AudioSource>();
        source.playOnAwake = false;
        sfxPool.Add(source);
        return source;
    }

    private void LateUpdate()
    {
        // Smooth dampening logic on Master Volume for seamless scene transitions
        currentMasterVolume = Mathf.Lerp(currentMasterVolume, targetMasterVolume, 1f - Mathf.Exp(-fadeSpeed * Time.unscaledDeltaTime));

        // Always clamp to user's camera to establish an absolute baseline for the audio listener context in XR
        Camera cam = Camera.main;
        if (cam == null) cam = FindFirstObjectByType<Camera>();
        if (cam != null)
        {
            transform.position = cam.transform.position;
        }

        // Update real-time volumes scaling off Master
        for (int i = 0; i < 2; i++)
        {
            // The active source plays at Music Vol * Master Vol. 
            // Crossfades happen independently via coroutines which touch the individual base volume limiters
            if (!isCrossfadingMusic[i])
            {
                if (i == activeMusicSourceIndex) 
                    musicSources[i].volume = musicSources[i].volume * 0f + (musicVolume * currentMasterVolume); // Immediate snap if not fading
            }
            else
            {
                // If it is crossfading, the coroutine controls its localized normalized envelope, we multiply it by global
                musicSources[i].volume = activeMusicFadeEnvelopes[i] * musicVolume * currentMasterVolume;
            }
        }
    }

    // =========================================================================================
    // SFX SYSTEM
    // =========================================================================================

    public AudioSource PlaySFX2D(AudioClip clip, float volume = 1f)
    {
        return PlaySFX(clip, transform.position, SoundOptions.Default2D(volume));
    }

    public AudioSource PlaySFX3D(AudioClip clip, Vector3 worldPosition, float volume = 1f)
    {
        return PlaySFX(clip, worldPosition, SoundOptions.Default3D(volume));
    }

    public AudioSource PlaySFX(AudioClip clip, Vector3 position, SoundOptions options)
    {
        if (clip == null) return null;

        AudioSource source = GetAvailableSFXSource();
        
        source.transform.position = position;
        source.clip = clip;
        source.volume = options.volume * sfxVolume * currentMasterVolume;
        
        // Pitch variation for organic audio
        float calculatedPitch = options.pitch;
        if (options.pitchRandomness > 0f)
            calculatedPitch += UnityEngine.Random.Range(-options.pitchRandomness, options.pitchRandomness);
        source.pitch = calculatedPitch;

        source.spatialBlend = options.spatialBlend;
        source.minDistance = options.minDistance;
        source.maxDistance = options.maxDistance;
        source.loop = options.loop;
        source.rolloffMode = options.rolloffMode;

        source.Play();

        // If it isn't looping, automatically return it to the pool baseline
        if (!options.loop)
        {
            StartCoroutine(ReturnSourceToPool(source, clip.length / Mathf.Max(0.01f, calculatedPitch)));
        }

        return source;
    }

    /// <summary>
    /// For looping SFX or specific VFX audio trails that need manual termination.
    /// </summary>
    public void StopSFX(AudioSource source, float fadeOutDuration = 0.5f)
    {
        if (source == null || !source.isPlaying) return;
        StartCoroutine(FadeOutSFXAndPool(source, fadeOutDuration));
    }

    private AudioSource GetAvailableSFXSource()
    {
        foreach (var source in sfxPool)
        {
            if (!source.isPlaying) return source;
        }
        // If all are busy, expand the memory pool dynamically
        return CreateNewPoolSource();
    }

    private IEnumerator ReturnSourceToPool(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (source != null && !source.loop) 
            source.Stop();
    }

    private IEnumerator FadeOutSFXAndPool(AudioSource source, float duration)
    {
        float startVol = source.volume;
        float time = 0;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(startVol, 0f, time / duration);
            yield return null;
        }
        source.Stop();
        source.volume = startVol; // Reset for next usage
    }

    // =========================================================================================
    // MUSIC SYSTEM (SEAMLESS DOUBLE-BUFFER CROSSFADING & REALTIME MANAGEMENT)
    // =========================================================================================

    private bool[] isCrossfadingMusic = new bool[2] { false, false };
    private float[] activeMusicFadeEnvelopes = new float[2] { 1f, 0f }; 
    private bool isMusicPaused = false;

    /// <summary>
    /// Smoothly transitions into a new Background Music track using a mathematical crossfade.
    /// </summary>
    public void PlayMusic(AudioClip clip, float crossfadeDuration = 1.5f)
    {
        if (clip == null) return;
        if (musicSources[activeMusicSourceIndex].clip == clip && (musicSources[activeMusicSourceIndex].isPlaying || isMusicPaused)) return;

        int newIndex = 1 - activeMusicSourceIndex; // Switch engine buffer (0 <-> 1)

        musicSources[newIndex].clip = clip;
        musicSources[newIndex].Play();
        isMusicPaused = false;

        StartCoroutine(CrossfadeMusicEngine(activeMusicSourceIndex, newIndex, crossfadeDuration));
        
        activeMusicSourceIndex = newIndex;
    }

    public void StopMusic(float fadeOutDuration = 1.5f)
    {
        StartCoroutine(FadeOutMusicEngine(activeMusicSourceIndex, fadeOutDuration));
        isMusicPaused = false;
    }

    /// <summary>
    /// Pause the active music channel.
    /// </summary>
    public void PauseMusic()
    {
        if (musicSources[activeMusicSourceIndex].isPlaying)
        {
            musicSources[activeMusicSourceIndex].Pause();
            isMusicPaused = true;
        }
    }

    /// <summary>
    /// Resumes the active music channel if it was paused.
    /// </summary>
    public void ResumeMusic()
    {
        if (isMusicPaused)
        {
            musicSources[activeMusicSourceIndex].UnPause();
            isMusicPaused = false;
        }
    }

    /// <summary>
    /// Realtime control over music pitch (ditch, fast forward effects).
    /// </summary>
    public void SetMusicPitch(float pitch)
    {
        for (int i = 0; i < 2; i++)
        {
            musicSources[i].pitch = pitch;
        }
    }

    /// <summary>
    /// Dynamically update max music volume, scaling over master volume.
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
    }

    /// <summary>
    /// Dynamically update master volume.
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        targetMasterVolume = masterVolume;
    }

    /// <summary>
    /// Dynamically update max SFX volume.
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }

    private IEnumerator CrossfadeMusicEngine(int oldIndex, int newIndex, float duration)
    {
        isCrossfadingMusic[oldIndex] = true;
        isCrossfadingMusic[newIndex] = true;

        float time = 0f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = time / duration;
            
            // Smoothstep curve for incredibly satisfying DJ-style blending
            float smoothT = t * t * (3f - 2f * t); 

            activeMusicFadeEnvelopes[oldIndex] = 1f - smoothT;
            activeMusicFadeEnvelopes[newIndex] = smoothT;
            yield return null;
        }

        activeMusicFadeEnvelopes[oldIndex] = 0f;
        activeMusicFadeEnvelopes[newIndex] = 1f;

        musicSources[oldIndex].Stop();

        isCrossfadingMusic[oldIndex] = false;
        isCrossfadingMusic[newIndex] = false;
    }

    private IEnumerator FadeOutMusicEngine(int index, float duration)
    {
        isCrossfadingMusic[index] = true;
        float startVol = activeMusicFadeEnvelopes[index];
        float time = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            activeMusicFadeEnvelopes[index] = Mathf.Lerp(startVol, 0f, time / duration);
            yield return null;
        }

        activeMusicFadeEnvelopes[index] = 0f;
        musicSources[index].Stop();
        isCrossfadingMusic[index] = false;
    }

    // =========================================================================================
    // SCENE TRANSITION GLOBAL HOOKS
    // =========================================================================================

    /// <summary>
    /// Pulls the entire master mix to 0 globally. Triggered inherently by SceneTransitionManager.
    /// </summary>
    public void GlobalFadeOut(float speedMultiplier = 10f)
    {
        fadeSpeed = speedMultiplier;
        targetMasterVolume = 0f;
    }

    /// <summary>
    /// Restores the entire master mix smoothly back to user parameters.
    /// </summary>
    public void GlobalFadeIn(float speedMultiplier = 10f)
    {
        fadeSpeed = speedMultiplier;
        targetMasterVolume = masterVolume;
    }
}
