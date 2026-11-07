using System;
using System.Collections.Generic;

namespace PtrLib.Settings;

public sealed class ModuleSettingsRegistration
{
    public ModuleSettingsRegistration(string owner, ModSettings settings)
    {
        Owner = owner;
        Settings = settings;
    }

    public string Owner { get; }
    public ModSettings Settings { get; }
}

public static class ModuleSettingsRegistry
{
    private static readonly List<ModuleSettingsRegistration> Registrations = new List<ModuleSettingsRegistration>();

    public static event Action<ModuleSettingsRegistration>? Registered;

    public static IReadOnlyList<ModuleSettingsRegistration> All => Registrations;

    public static void Register(string owner, ModSettings settings)
    {
        if (string.IsNullOrWhiteSpace(owner))
            throw new ArgumentException("Owner must not be empty.", nameof(owner));
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));

        var registration = new ModuleSettingsRegistration(owner, settings);
        Registrations.Add(registration);
        Registered?.Invoke(registration);
    }
}
