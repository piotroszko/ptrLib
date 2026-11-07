using System;
using Il2Cpp;
using Il2CppTMPro;
using PtrLib.Diagnostics;
using PtrLib.Ui;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PtrLib.PcApps;

internal static class PcAppInstaller
{
    internal static PcAppInstance? Install(
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
}
