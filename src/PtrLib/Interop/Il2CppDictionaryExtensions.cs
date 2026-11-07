using Il2CppSystem.Collections.Generic;

namespace PtrLib.Interop;

/// <summary>Small convenience helpers for Il2Cpp dictionaries.</summary>
public static class Il2CppDictionaryExtensions
{
    public static TValue ValueOrDefault<TKey, TValue>(
        this Dictionary<TKey, TValue>? dictionary,
        TKey key,
        TValue fallback)
    {
        if (dictionary == null)
            return fallback;

        return dictionary.TryGetValue(key, out TValue value) ? value : fallback;
    }
}
