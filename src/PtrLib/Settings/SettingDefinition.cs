using System;
using MelonLoader;

namespace PtrLib.Settings;

public abstract class SettingDefinition
{
    protected SettingDefinition(string key, string label)
    {
        Key = key;
        Label = label;
    }

    public string Key { get; }
    public string Label { get; }
}

public sealed class ToggleSetting : SettingDefinition
{
    public ToggleSetting(
        string key,
        string label,
        MelonPreferences_Entry<bool> entry,
        Action<bool>? changed)
        : base(key, label)
    {
        Entry = entry;
        Changed = changed;
    }

    public MelonPreferences_Entry<bool> Entry { get; }
    public Action<bool>? Changed { get; }

    public bool Value => Entry.Value;

    public void Apply(bool value)
    {
        if (Entry.Value == value) return;
        Entry.Value = value;
        Changed?.Invoke(value);
    }
}

public sealed class IntSliderSetting : SettingDefinition
{
    public IntSliderSetting(
        string key,
        string label,
        MelonPreferences_Entry<int> entry,
        int min,
        int max,
        string format,
        Action<int>? changed)
        : base(key, label)
    {
        Entry = entry;
        Min = min;
        Max = max;
        Format = string.IsNullOrEmpty(format) ? "{0}" : format;
        Changed = changed;
    }

    public MelonPreferences_Entry<int> Entry { get; }
    public int Min { get; }
    public int Max { get; }
    public int Value => Entry.Value;
    public string Format { get; }
    public Action<int>? Changed { get; }

    public void Apply(int value)
    {
        value = Math.Clamp(value, Min, Max);
        if (Entry.Value == value) return;
        Entry.Value = value;
        Changed?.Invoke(value);
    }

    public string FormatValue(int value) => string.Format(Format, value);
}
