using HarmonyLib;
using Il2Cpp;
using UnityEngine;

namespace PtrLib.Items;

internal static class ModItemPatches
{
    private static HarmonyLib.Harmony? _harmony;
    private static bool _installed;

    internal static void Install()
    {
        if (_installed) return;

        _harmony = new HarmonyLib.Harmony("PtrLib.ModItems");
        _harmony.PatchAll(typeof(ModItemPrefabPatch));
        _harmony.PatchAll(typeof(ModItemPickupPatch));
        _installed = true;
    }
}

[HarmonyPatch(typeof(ComputerShop), nameof(ComputerShop.GetPrefabForItem))]
internal static class ModItemPrefabPatch
{
    [HarmonyPrefix]
    private static bool Prefix(
        int __0,
        PlayerManager.ObjectInHand __1,
        ref GameObject __result)
    {
        var prefab = ModItemRegistry.GetPrefab(__0);
        if (prefab == null) return true;

        __result = prefab;
        return false;
    }
}

[HarmonyPatch(typeof(UsableObject), nameof(UsableObject.MoveToHand))]
internal static class ModItemPickupPatch
{
    [HarmonyPostfix]
    private static void Postfix(UsableObject __instance)
    {
        if (!ModItemRegistry.IsRegistered(__instance)) return;

        __instance.maxInHand = 1;
        __instance.objectInHands = true;

        var player = PlayerManager.instance;
        if (player != null)
            player.objectInHand = __instance.objectInHandType;
    }
}
