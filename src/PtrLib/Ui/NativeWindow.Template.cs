using Il2Cpp;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PtrLib.Ui;

internal sealed class NativeWindowTemplate
{
    private NativeWindowTemplate(
        GameObject root,
        TextMeshProUGUI textTemplate,
        ButtonExtended buttonTemplate,
        RectTransform rectTemplate)
    {
        Root = root;
        TextTemplate = textTemplate;
        ButtonTemplate = buttonTemplate;
        RectTemplate = rectTemplate;
    }

    internal GameObject Root { get; }
    internal TextMeshProUGUI TextTemplate { get; }
    internal ButtonExtended ButtonTemplate { get; }
    internal RectTransform RectTemplate { get; }

    internal static NativeWindowTemplate? Find()
    {
        var router = UiFactory.FindSceneComponent<RouterConfiguration>();
        if (router != null && TryCreate(router.transform, out var template))
            return template;

        var setIp = UiFactory.FindSceneComponent<SetIP>();
        if (setIp != null && setIp.canvas != null &&
            TryCreate(setIp.canvas.transform, out template))
            return template;

        var switchConfig = UiFactory.FindSceneComponent<NetworkSwitchConfiguration>();
        if (switchConfig != null && TryCreate(switchConfig.transform, out template))
            return template;

        return null;
    }

    private static bool TryCreate(Transform source, out NativeWindowTemplate? template)
    {
        template = null;

        var canvas = source.GetComponentInParent<Canvas>(true);
        var root = canvas != null ? canvas.transform : source;

        if (UiFactory.FindChild(root, "bcg") == null)
            return false;

        var text = root.GetComponentInChildren<TextMeshProUGUI>(true);
        var button = root.GetComponentInChildren<ButtonExtended>(true);
        var rect = FindRectTemplate(root);
        if (text == null || button == null || rect == null)
            return false;

        template = new NativeWindowTemplate(root.gameObject, text, button, rect);
        return true;
    }

    private static RectTransform? FindRectTemplate(Transform root)
    {
        var rects = root.GetComponentsInChildren<RectTransform>(true);
        for (int i = 0; i < rects.Length; i++)
        {
            var rect = rects[i];
            if (rect == null || rect.transform == root) continue;
            if (rect.GetComponent<Graphic>() != null) continue;
            if (rect.GetComponent<CanvasRenderer>() != null) continue;
            if (rect.GetComponents<MonoBehaviour>().Length != 0) continue;
            return rect;
        }

        var bcg = UiFactory.FindChild(root, "bcg");
        return bcg != null ? bcg.GetComponent<RectTransform>() : null;
    }
}
