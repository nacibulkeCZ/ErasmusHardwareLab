#if UNITY_EDITOR && ENABLE_INPUT_SYSTEM
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

public sealed class EditorSimulatedHapticsGuard : MonoBehaviour
{
    const string SimulatedControllerLayoutName = "XRSimulatedController";
    static readonly WaitForSeconds ScanInterval = new WaitForSeconds(1f);
    static bool s_Initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureInstance()
    {
        if (s_Initialized)
            return;

        var guard = new GameObject(nameof(EditorSimulatedHapticsGuard));
        guard.hideFlags = HideFlags.HideAndDontSave;
        DontDestroyOnLoad(guard);
        guard.AddComponent<EditorSimulatedHapticsGuard>();
        s_Initialized = true;
    }

    void Awake()
    {
        StartCoroutine(WatchForSimulatedController());
    }

    IEnumerator WatchForSimulatedController()
    {
        while (true)
        {
            if (HasSimulatedController())
                DisableHapticImpulsePlayers();

            yield return ScanInterval;
        }
    }

    static bool HasSimulatedController()
    {
        foreach (var device in InputSystem.devices)
        {
            if (device.layout == SimulatedControllerLayoutName)
                return true;
        }

        return false;
    }

    static void DisableHapticImpulsePlayers()
    {
        var players = FindObjectsByType<HapticImpulsePlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (var i = 0; i < players.Length; ++i)
        {
            if (players[i] != null && players[i].enabled)
                players[i].enabled = false;
        }
    }
}
#endif