using System;
using System.Collections.Generic;
using Il2Cpp;
using UnityEngine;

namespace PtrLib.Items;

public static class ModItemRegistry
{
    private static readonly List<ModItemDefinition> Definitions = new List<ModItemDefinition>();
    private static readonly Dictionary<string, ModItemDefinition> DefinitionsByLabel =
        new Dictionary<string, ModItemDefinition>();
    private static readonly Dictionary<string, int> ModIdsByFolder = new Dictionary<string, int>();
    private static readonly Dictionary<int, ModItemDefinition> DefinitionsByModId =
        new Dictionary<int, ModItemDefinition>();
    private static readonly Dictionary<int, Sprite> IconsByModId = new Dictionary<int, Sprite>();

    public static int RegisteredCount => Definitions.Count;
    public static int ResolvedCount => ModIdsByFolder.Count;

    public static void Register(params ModItemDefinition[] items)
    {
        if (items == null) return;

        for (int i = 0; i < items.Length; i++)
            Register(items[i]);
    }

    public static void Register(ModItemDefinition item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        foreach (var existing in Definitions)
        {
            if (string.Equals(existing.Folder, item.Folder, StringComparison.Ordinal) ||
                string.Equals(existing.Label, item.Label, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Duplicate mod item folder '{item.Folder}' or label '{item.Label}'.");
            }
        }

        Definitions.Add(item);
        DefinitionsByLabel[item.Label] = item;
        ClearResolved();
        ModItemPatches.Install();
    }

    public static void ResetCache()
    {
        ClearResolved();
    }

    public static bool TryGetModId(string folder, out int modId, out string reason)
    {
        Refresh();

        if (ModIdsByFolder.TryGetValue(folder, out modId))
        {
            reason = string.Empty;
            return true;
        }

        reason = $"ModLoader did not expose '{folder}'.";
        return false;
    }

    public static GameObject? GetPrefab(int modId)
    {
        if (ModIdsByFolder.Count == 0)
            Refresh();

        if (!DefinitionsByModId.TryGetValue(modId, out var definition))
            return null;

        var prefab = ModLoader.instance?.GetModPrefab(modId);
        if (prefab != null)
            NormalizeObject(prefab, definition, modId);
        return prefab;
    }

    public static void NormalizeExisting(UsableObject usable)
    {
        if (usable == null) return;

        if (ModIdsByFolder.Count == 0)
            Refresh();

        string folder = usable.modFolderName;
        if (string.IsNullOrEmpty(folder)) return;
        if (!ModIdsByFolder.TryGetValue(folder, out int modId)) return;
        if (!DefinitionsByModId.TryGetValue(modId, out var definition)) return;

        var prefab = ModLoader.instance?.GetModPrefab(modId) ?? usable.gameObject;
        NormalizeObject(usable, definition, modId, prefab);
    }

    public static bool IsRegistered(UsableObject usable)
    {
        if (usable == null) return false;
        string folder = usable.modFolderName;
        if (string.IsNullOrEmpty(folder)) return false;

        if (ModIdsByFolder.Count == 0)
            Refresh();

        return ModIdsByFolder.ContainsKey(folder);
    }

    public static void Refresh()
    {
        var cards = Resources.FindObjectsOfTypeAll<ModShopItem>();
        for (int i = 0; i < cards.Length; i++)
        {
            var card = cards[i];
            if (card == null) continue;

            var config = card.config;
            if (config == null) continue;

            var cardObject = card.gameObject;
            if (cardObject == null) continue;
            if (!cardObject.scene.IsValid()) continue;

            if (!DefinitionsByLabel.TryGetValue(config.itemName, out var definition))
                continue;

            ModIdsByFolder[definition.Folder] = card.modID;
            DefinitionsByModId[card.modID] = definition;
            if (card.itemIcon != null)
                IconsByModId[card.modID] = card.itemIcon.sprite;

            if (cardObject.activeSelf)
                cardObject.SetActive(false);
        }
    }

    private static void ClearResolved()
    {
        ModIdsByFolder.Clear();
        DefinitionsByModId.Clear();
        IconsByModId.Clear();
        ModItemVisuals.ResetCache();
    }

    private static void NormalizeObject(GameObject prefab, ModItemDefinition definition, int modId)
    {
        var usable = prefab.GetComponent<UsableObject>();
        if (usable != null)
            NormalizeObject(usable, definition, modId, prefab);
    }

    private static void NormalizeObject(
        UsableObject usable,
        ModItemDefinition definition,
        int modId,
        GameObject prefab)
    {
        usable.objectInHandType = definition.ItemType;
        usable.maxInHand = 1;
        if (usable.prefabID <= 0)
            usable.prefabID = modId;

        foreach (var renderer in usable.GetComponentsInChildren<Renderer>(true))
            ModItemVisuals.MakeOpaque(renderer);

        string label = definition.Label;

        if (usable.item == null)
        {
            try
            {
                var item = ScriptableObject.CreateInstance<Item>();
                if (item != null)
                {
                    item.itemName = label;
                    item.prefab = prefab;
                    item.price = definition.Price;
                    usable.item = item;
                }
            }
            catch (Exception)
            {
            }
        }

        if (usable.shopItemSO == null)
        {
            try
            {
                var shopItem = ScriptableObject.CreateInstance<ShopItemSO>();
                if (shopItem != null)
                {
                    shopItem.itemName = label;
                    shopItem.itemID = modId;
                    shopItem.itemType = definition.ItemType;
                    shopItem.price = definition.Price;
                    if (definition.Icon != null)
                        shopItem.sprite = definition.Icon;
                    else if (IconsByModId.TryGetValue(modId, out var icon))
                        shopItem.sprite = icon;
                    usable.shopItemSO = shopItem;
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
