using System;
using Il2Cpp;
using UnityEngine;

namespace PtrLib.Items;

public sealed class ModItemDefinition
{
    public ModItemDefinition(
        string folder,
        string label,
        int price,
        PlayerManager.ObjectInHand itemType,
        Sprite? icon = null)
    {
        if (string.IsNullOrWhiteSpace(folder))
            throw new ArgumentException("Item folder must not be empty.", nameof(folder));
        if (string.IsNullOrWhiteSpace(label))
            throw new ArgumentException("Item label must not be empty.", nameof(label));

        Folder = folder;
        Label = label;
        Price = price;
        ItemType = itemType;
        Icon = icon;
    }

    public string Folder { get; }
    public string Label { get; }
    public int Price { get; }
    public PlayerManager.ObjectInHand ItemType { get; }
    public Sprite? Icon { get; }
}
