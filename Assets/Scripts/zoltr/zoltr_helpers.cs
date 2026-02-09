using UnityEngine;

public enum object_id
{
    MOTHERBOARD,    // keys = 00 - 15
    CPU,            // keys = 16 - 31
    CPU_COOLER,     // keys = 32 - 47
    RAM,            // keys = 48 - 63
    GPU,            // keys = 64 - 79
    POWER_SUPPLY,   // keys = 80 - 95
    STORAGE,        // keys = 96 - 111
    CASE            // keys = 112 - 127
}

public static class ZoltrHelpers
{
    private const int KEYS_PER_COMPONENT = 16;
    public static int GetComponentKey(object_id component, int index = 0)
    {
        return ((int)component * KEYS_PER_COMPONENT) + Mathf.Clamp(index, 0, KEYS_PER_COMPONENT - 1);
    }
}