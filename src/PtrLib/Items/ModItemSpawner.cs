using System;
using Il2Cpp;
using PtrLib.Diagnostics;
using UnityEngine;

namespace PtrLib.Items;

public sealed class ModItemSpawnResult
{
    internal ModItemSpawnResult(GameObject? item, string message)
    {
        Item = item;
        Message = message;
    }

    public GameObject? Item { get; }
    public string Message { get; }
    public bool Success => Item != null;
}

public static class ModItemSpawner
{
    private static int _spawnSequence;

    public static ModItemSpawnResult Spawn(
        ComputerShop shop,
        ModItemDefinition definition,
        ModuleLog log)
    {
        if (shop == null || definition == null)
            return new ModItemSpawnResult(null, "Item or shop was not available.");

        if (!ModItemRegistry.TryGetModId(definition.Folder, out int modId, out string reason))
        {
            log.Warning($"{definition.Label} purchase blocked: {reason}");
            return new ModItemSpawnResult(null, $"{definition.Label} is unavailable: {reason}");
        }

        try
        {
            var prefab = ModItemRegistry.GetPrefab(modId);
            if (prefab == null)
            {
                log.Warning($"{definition.Label} purchase blocked: prefab {modId} was not found.");
                return new ModItemSpawnResult(
                    null,
                    $"{definition.Label} is unavailable: its prefab was not loaded.");
            }

            log.Msg(
                $"Spawning {definition.Label} through ComputerShop.SpawnPhysicalItem " +
                $"(modId={modId}, price={definition.Price}, type={definition.ItemType}).");
            var spawn = shop.SpawnPhysicalItem(prefab, definition.Price, definition.ItemType);

            GameObject? item = null;
            int highestUid = -1;
            var items = shop.spawnedItems;
            if (items != null)
            {
                var enumerator = items.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var pair = enumerator.Current;
                    if (pair.Key <= highestUid) continue;
                    highestUid = pair.Key;
                    item = pair.Value;
                }
            }

            log.Msg(
                $"SpawnPhysicalItem returned {(spawn.HasValue ? spawn.Value.ToString() : "<none>")}; " +
                $"newest uid={highestUid}, object={(item != null ? item.name : "<none>")}.");

            if (item == null)
            {
                log.Warning($"{definition.Label} produced no spawned item object.");
                return new ModItemSpawnResult(
                    null,
                    $"Bought {definition.Label}, but the game returned no item.");
            }

            bool wasActive = item.activeSelf;
            if (!wasActive)
                item.SetActive(true);

            var originalParent = item.transform.parent;
            log.Msg(
                $"Detaching {item.name} from parent " +
                $"{(originalParent != null ? originalParent.name : "<none>")} " +
                $"(parentActive={(originalParent != null ? originalParent.gameObject.activeInHierarchy.ToString() : "<none>")}).");
            item.transform.SetParent(null, true);

            if (items != null && highestUid >= 0 && items.Remove(highestUid))
                log.Msg($"Removed uid={highestUid} from ComputerShop.spawnedItems so CloseShop will not destroy it.");

            var usable = item.GetComponent<UsableObject>();
            if (usable != null)
            {
                usable.isModObject = true;
                usable.modFolderName = definition.Folder;
                CoopLooseItems.Register(usable);
                ModItemRegistry.NormalizeExisting(usable);
            }

            _spawnSequence++;
            MoveToPickupPoint(item, _spawnSequence, log);

            var itemRenderers = item.GetComponentsInChildren<Renderer>(true);
            var mainRenderer = itemRenderers.Length > 0 ? itemRenderers[0] : null;
            log.Msg(
                $"{definition.Label} item active={wasActive}->{item.activeSelf}, at {item.transform.position}, " +
                $"layer={item.layer}, renderers={itemRenderers.Length}, " +
                $"rendererEnabled={(mainRenderer != null ? mainRenderer.enabled.ToString() : "<none>")}, " +
                $"bounds={(mainRenderer != null ? mainRenderer.bounds.size.ToString() : "<none>")}, " +
                $"usable={usable != null}.");

            foreach (var renderer in itemRenderers)
            {
                var material = renderer.material;
                log.Msg(
                    $"renderer={renderer.name} layer={renderer.gameObject.layer} " +
                    $"mat={(material != null ? material.name : "<none>")} " +
                    $"shader={(material != null && material.shader != null ? material.shader.name : "<none>")} " +
                    $"queue={(material != null ? material.renderQueue : -1)} " +
                    $"keywords={(material != null ? string.Join(",", material.shaderKeywords) : "<none>")} " +
                    $"color={(material != null ? material.color.ToString() : "<none>")} " +
                    $"outline={(usable != null && usable.outlineEffect != null ? usable.outlineEffect.enabled.ToString() : "none")}");
            }
            log.Msg(ModItemVisuals.NativeServerInfo());
            LogShopState(shop, "after spawn", log);
            LogSpawnedItems(shop, log);

            return new ModItemSpawnResult(
                item,
                $"Bought {definition.Label}. It is floating in front of you.");
        }
        catch (Exception exception)
        {
            log.Error($"Purchase of {definition.Label} failed.", exception);
            return new ModItemSpawnResult(
                null,
                $"Purchase of {definition.Label} failed; see the MelonLoader log.");
        }
    }

    private static void MoveToPickupPoint(GameObject item, int sequence, ModuleLog log)
    {
        float side = ((sequence - 1) % 4 - 1.5f) * 0.28f;
        var player = PlayerManager.instance;
        Transform? anchor = null;
        if (player != null && player.vcam != null)
            anchor = player.vcam.transform;
        else if (player != null && player.playerGO != null)
            anchor = player.playerGO.transform;

        Vector3 drop;
        if (anchor != null)
        {
            drop = anchor.position
                + anchor.forward * 0.5f
                + anchor.right * side;
            drop.y -= 0.1f;
        }
        else
        {
            var camera = Camera.main;
            if (camera == null) return;

            drop = camera.transform.position
                + camera.transform.forward * 0.8f
                + camera.transform.right * side;
        }

        item.transform.position = drop;

        var body = item.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.useGravity = false;
            body.isKinematic = true;
        }

        log.Msg($"Placed {item.name} at the player pickup point {drop}.");
    }

    private static void LogShopState(ComputerShop shop, string label, ModuleLog log)
    {
        var inUse = shop.itemsSpawnsInUse;
        string used = string.Empty;
        if (inUse != null)
        {
            for (int i = 0; i < inUse.Length; i++)
                used += inUse[i] == 0 ? "0" : "1";
        }

        log.Msg(
            $"{label}: cart={(shop.cartUIItems != null ? shop.cartUIItems.Count : -1)}, " +
            $"spawned={(shop.spawnedItems != null ? shop.spawnedItems.Count : -1)}, " +
            $"productSpawns={(shop.transformProductItemsSpawns != null ? shop.transformProductItemsSpawns.Length : -1)}, " +
            $"bigSpawns={(shop.transformBigItemSpawns != null ? shop.transformBigItemSpawns.Length : -1)}, " +
            $"inUse='{used}'.");
    }

    private static void LogSpawnedItems(ComputerShop shop, ModuleLog log)
    {
        var items = shop.spawnedItems;
        if (items == null) return;

        var enumerator = items.GetEnumerator();
        while (enumerator.MoveNext())
        {
            var pair = enumerator.Current;
            var item = pair.Value;
            log.Msg(
                $"  spawned uid={pair.Key}: " +
                $"{(item != null ? item.name + " at " + item.transform.position : "<destroyed>")}");
        }
    }
}
