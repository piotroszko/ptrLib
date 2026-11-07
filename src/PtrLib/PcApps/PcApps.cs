using System;
using System.Collections.Generic;
using Il2Cpp;
using PtrLib.Diagnostics;

namespace PtrLib.PcApps;

public static class PcApps
{
    private static readonly List<PcAppDefinition> Definitions = new List<PcAppDefinition>();
    private static readonly Dictionary<int, List<PcAppInstance>> Installed =
        new Dictionary<int, List<PcAppInstance>>();

    public static bool AnyRegistered => Definitions.Count > 0;

    public static void Register(PcAppDefinition app)
    {
        if (app == null)
            throw new ArgumentNullException(nameof(app));

        foreach (var existing in Definitions)
        {
            if (string.Equals(existing.Id, app.Id, StringComparison.Ordinal))
                throw new InvalidOperationException($"Duplicate PC app id '{app.Id}'.");
        }

        Definitions.Add(app);
    }

    public static void Reset()
    {
        Installed.Clear();
    }

    public static void EnsureInstalled(ComputerShop shop, ModuleLog log)
    {
        if (shop == null || shop.mainScreen == null || !AnyRegistered) return;

        int shopId = shop.GetInstanceID();
        if (!Installed.TryGetValue(shopId, out var instances))
        {
            instances = new List<PcAppInstance>();
            Installed[shopId] = instances;
        }

        for (int i = 0; i < instances.Count; i++)
        {
            var instance = instances[i];
            if (instance.Panel != null && instance.Panel.activeSelf &&
                instance.Shop.mainScreen != null && instance.Shop.mainScreen.activeSelf)
            {
                instance.Close();
            }
        }

        var main = shop.mainScreen;
        var grid = main.transform.Find("Button Grid");
        if (grid == null)
        {
            log.Warning("Computer desktop grid was not found.");
            return;
        }

        foreach (var definition in Definitions)
        {
            bool installed = false;
            for (int i = 0; i < instances.Count; i++)
            {
                if (string.Equals(instances[i].Definition.Id, definition.Id, StringComparison.Ordinal))
                {
                    installed = true;
                    break;
                }
            }

            if (installed) continue;

            var instance = PcAppInstaller.Install(shop, main, grid, definition, log);
            if (instance != null)
                instances.Add(instance);
        }
    }
}
