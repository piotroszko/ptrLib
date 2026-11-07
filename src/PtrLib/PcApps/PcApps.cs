using System;
using System.Collections.Generic;
using Il2Cpp;
using Il2CppTMPro;
using PtrLib.Diagnostics;
using PtrLib.Ui;
using UnityEngine;
using UnityEngine.Events;
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

public static class PcApps
{
    private static readonly List<PcAppDefinition> Definitions = new List<PcAppDefinition>();
    private static readonly Dictionary<int, List<PcAppInstance>> Installed =
        new Dictionary<int, List<PcAppInstance>>();

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
        if (shop == null || shop.mainScreen == null) return;

        int shopId = shop.GetInstanceID();
        if (Installed.TryGetValue(shopId, out var existing))
        {
            for (int i = 0; i < existing.Count; i++)
            {
                var instance = existing[i];
                if (instance.Panel != null && instance.Panel.activeSelf &&
                    instance.Shop.mainScreen != null && instance.Shop.mainScreen.activeSelf)
                {
                    instance.Close();
                }
            }

            return;
        }

        var main = shop.mainScreen;
        var grid = main.transform.Find("Button Grid");
        if (grid == null)
        {
            log.Warning("Computer desktop grid was not found.");
            return;
        }

        var instances = new List<PcAppInstance>();
        foreach (var definition in Definitions)
        {
            var instance = Install(shop, main, grid, definition, log);
            if (instance != null)
                instances.Add(instance);
        }

        if (instances.Count > 0)
            Installed[shopId] = instances;
    }

    private static PcAppInstance? Install(
        ComputerShop shop,
        GameObject main,
        Transform grid,
        PcAppDefinition definition,
        ModuleLog log)
    {
        if (grid.Find(definition.IconName) != null)
            return null;

        var templateIcon = grid.Find("Icon Shop bcg");
        if (templateIcon == null)
        {
            if (grid.childCount == 0) return null;
            templateIcon = grid.GetChild(0);
        }

        var textTemplate = templateIcon.GetComponentInChildren<TextMeshProUGUI>(true);
        if (textTemplate == null)
        {
            log.Warning($"Computer icon text template was not found for '{definition.Label}'.");
            return null;
        }

        var icon = UnityEngine.Object.Instantiate(templateIcon.gameObject, grid);
        icon.name = definition.IconName;

        var label = icon.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            var localised = label.GetComponent<LocalisedText>();
            if (localised != null) localised.enabled = false;
            label.text = definition.Label;
        }

        var button = icon.GetComponentInChildren<ButtonExtended>(true);
        if (button == null)
        {
            UnityEngine.Object.Destroy(icon);
            log.Warning($"Computer icon button was not found for '{definition.Label}'.");
            return null;
        }

        var parent = main.transform.parent;
        if (parent == null)
        {
            UnityEngine.Object.Destroy(icon);
            log.Warning($"Computer desktop root was not found for '{definition.Label}'.");
            return null;
        }

        var panel = UiFactory.CreatePanel(
            parent,
            main,
            "PcApp_" + definition.Id,
            new Color(0.02f, 0.04f, 0.06f, 0.98f));
        UiFactory.Stretch(
            panel.GetComponent<RectTransform>(),
            new Vector2(10f, 10f),
            new Vector2(-10f, -10f));

        var returnTemplate = shop.networkMapScreen != null
            ? shop.networkMapScreen.transform.Find("Button Return")
            : main.transform.Find("Button Return");
        var buttonTemplate = returnTemplate != null
            ? returnTemplate.GetComponent<ButtonExtended>()
            : button;

        var context = new PcAppContext(shop, main, panel, textTemplate, buttonTemplate, log);
        definition.Build?.Invoke(context);

        var back = UiFactory.CreateButton(buttonTemplate, panel.transform, "Button Back");
        UiFactory.SetTopRight(
            back.GetComponent<RectTransform>(),
            new Vector2(140f, 40f),
            new Vector2(-20f, -20f));
        UiFactory.SetButtonLabel(back, "Back");
        back.gameObject.SetActive(true);

        var instance = new PcAppInstance(definition, shop, panel);
        back.onClick.AddListener((UnityAction)(Action)instance.Close);
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener((UnityAction)(Action)instance.Open);

        panel.SetActive(false);
        FixScreenDepth(shop);
        log.Msg($"Installed PC app '{definition.Label}'.");
        return instance;
    }

    private static void FixScreenDepth(ComputerShop shop)
    {
        var root = shop.canvasComputerShop != null
            ? shop.canvasComputerShop
            : shop.mainScreen;
        if (root == null) return;

        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            var material = renderer.material;
            if (material == null) continue;
            if (material.HasProperty("_ZTest"))
                material.SetFloat("_ZTest", 4f);
            if (material.HasProperty("_ZTestTransparent"))
                material.SetFloat("_ZTestTransparent", 4f);
            if (material.HasProperty("_ZTestGBuffer"))
                material.SetFloat("_ZTestGBuffer", 4f);
        }

        foreach (var canvasRenderer in root.GetComponentsInChildren<CanvasRenderer>(true))
        {
            try
            {
                var material = canvasRenderer.GetMaterial();
                if (material == null) continue;
                if (material.HasProperty("_ZTest"))
                    material.SetFloat("_ZTest", 4f);
            }
            catch (Exception)
            {
            }
        }
    }

    private sealed class PcAppInstance
    {
        internal PcAppInstance(PcAppDefinition definition, ComputerShop shop, GameObject panel)
        {
            Definition = definition;
            Shop = shop;
            Panel = panel;
        }

        internal PcAppDefinition Definition { get; }
        internal ComputerShop Shop { get; }
        internal GameObject Panel { get; }

        internal void Open()
        {
            if (Panel == null) return;
            if (Shop.mainScreen != null)
            {
                Shop.ButtonReturnMainScreen();
                Shop.mainScreen.SetActive(false);
            }

            Panel.SetActive(true);
            Definition.Opened?.Invoke();
        }

        internal void Close()
        {
            if (Panel == null) return;
            Panel.SetActive(false);
            if (Shop.mainScreen != null)
                Shop.mainScreen.SetActive(true);
            Definition.Closed?.Invoke();
        }
    }
}
