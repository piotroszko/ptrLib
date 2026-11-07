using System;
using Il2Cpp;
using Il2CppTMPro;
using PtrLib.Diagnostics;
using UnityEngine;
using UnityEngine.UI;

namespace PtrLib.PcApps;

public sealed class PcAppDefinition
{
    public PcAppDefinition(
        string id,
        string label,
        Action<PcAppContext>? build = null,
        Action? opened = null,
        Action? closed = null,
        string? iconName = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("App id must not be empty.", nameof(id));

        Id = id;
        Label = label;
        Build = build;
        Opened = opened;
        Closed = closed;
        IconName = string.IsNullOrWhiteSpace(iconName) ? $"Icon {id} bcg" : iconName;
    }

    public string Id { get; }
    public string Label { get; }
    public string IconName { get; }
    public Action<PcAppContext>? Build { get; }
    public Action? Opened { get; }
    public Action? Closed { get; }
}

public sealed class PcAppContext
{
    internal PcAppContext(
        ComputerShop shop,
        GameObject mainScreen,
        GameObject panel,
        TextMeshProUGUI textTemplate,
        ButtonExtended buttonTemplate,
        ModuleLog log)
    {
        Shop = shop;
        MainScreen = mainScreen;
        Panel = panel;
        TextTemplate = textTemplate;
        ButtonTemplate = buttonTemplate;
        Log = log;
    }

    public ComputerShop Shop { get; }
    public GameObject MainScreen { get; }
    public GameObject Panel { get; }
    public TextMeshProUGUI TextTemplate { get; }
    public ButtonExtended ButtonTemplate { get; }
    public ModuleLog Log { get; }
}
