using System;
using System.Collections.Generic;
using Il2Cpp;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PtrLib.Ui;

/// <summary>
/// A runtime-built window that reuses the game's native window visuals
/// (background, title font and button style).
/// </summary>
public sealed class NativeWindow
{
    private readonly GameObject _root;
    private readonly RectTransform _window;
    private readonly RectTransform _content;
    private readonly TextMeshProUGUI _textTemplate;
    private readonly ButtonExtended _buttonTemplate;
    private readonly TextMeshProUGUI _title;
    private readonly RectTransform _rectTemplate;
    private readonly List<GameObject> _children = new List<GameObject>();

    private NativeWindow(
        GameObject root,
        RectTransform window,
        RectTransform content,
        TextMeshProUGUI title,
        TextMeshProUGUI textTemplate,
        ButtonExtended buttonTemplate,
        RectTransform rectTemplate)
    {
        _root = root;
        _window = window;
        _content = content;
        _title = title;
        _textTemplate = textTemplate;
        _buttonTemplate = buttonTemplate;
        _rectTemplate = rectTemplate;
    }

    public GameObject Root => _root;
    public RectTransform WindowRect => _window;
    public RectTransform ContentRect => _content;
    public bool IsVisible => _root != null && _root.activeSelf;

    public static NativeWindow? Create(
        string name,
        string title,
        Vector2 size,
        Vector2 anchoredPosition,
        int sortingOrder = 20,
        bool showCloseButton = true)
    {
        var template = NativeWindowTemplate.Find();
        if (template == null)
            return null;

        var root = UnityEngine.Object.Instantiate(template.Root, template.Root.transform.parent);
        root.name = name;
        root.SetActive(false);

        var canvas = root.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;
        }

        var raycaster = root.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
            raycaster = root.AddComponent<GraphicRaycaster>();
        raycaster.enabled = true;

        var canvasGroup = root.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.enabled = true;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        // The clone carries native configuration scripts; they must not run
        // against this stripped-down window.
        var behaviours = root.GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            var behaviour = behaviours[i];
            if (behaviour == null) continue;
            if (behaviour is CanvasScaler || behaviour is GraphicRaycaster) continue;
            behaviour.enabled = false;
        }

        var bcg = UiFactory.FindChild(root.transform, "bcg");
        var window = bcg != null ? bcg.GetComponent<RectTransform>() : null;
        if (window == null)
        {
            window = CreateFallbackWindow(template.RectTemplate, root.transform);
        }

        Transform windowRoot = window;
        while (windowRoot.parent != null && windowRoot.parent != root.transform)
            windowRoot = windowRoot.parent;

        for (int i = root.transform.childCount - 1; i >= 0; i--)
        {
            var child = root.transform.GetChild(i);
            if (child != windowRoot)
                UnityEngine.Object.Destroy(child.gameObject);
        }

        for (int i = window.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(window.GetChild(i).gameObject);

        window.anchorMin = new Vector2(0.5f, 0.5f);
        window.anchorMax = new Vector2(0.5f, 0.5f);
        window.pivot = new Vector2(0.5f, 0.5f);
        window.sizeDelta = size;
        window.anchoredPosition = anchoredPosition;

        var titleText = UiFactory.CreateText(
            window,
            template.TextTemplate,
            title,
            24f,
            TextAlignmentOptions.Left,
            Vector2.zero,
            Vector2.zero);
        UiFactory.SetTopLeft(
            titleText.rectTransform,
            new Vector2(size.x - 96f, 38f),
            new Vector2(18f, -12f));

        Button? closeButton = null;
        if (showCloseButton)
        {
            var close = UiFactory.CreateButton(template.ButtonTemplate, window, "Button Close");
            UiFactory.SetTopRight(
                close.GetComponent<RectTransform>(),
                new Vector2(34f, 34f),
                new Vector2(-12f, -10f));
            UiFactory.SetButtonLabel(close, "X");
            closeButton = close.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = close.targetGraphic;
            closeButton.transition = Selectable.Transition.None;
        }

        var separator = CreateSeparator(template.RectTemplate, window, size.x, -54f);

        var content = CreateScrollContent(template.RectTemplate, window, size);
        var result = new NativeWindow(
            root,
            window,
            content,
            titleText,
            template.TextTemplate,
            template.ButtonTemplate,
            template.RectTemplate);
        if (closeButton != null)
            closeButton.onClick.AddListener((UnityAction)(Action)result.Hide);

        result._children.Add(separator);
        result.Show(false);
        return result;
    }

    public void SetTitle(string title)
    {
        if (_title != null)
            _title.text = title ?? string.Empty;
    }

    public void Show(bool show = true)
    {
        if (_root == null) return;
        _root.SetActive(show);
    }

    public void Hide() => Show(false);

    public void Close()
    {
        if (_root != null)
            UnityEngine.Object.Destroy(_root);
    }

    public void PlaceNextTo(RectTransform target, float gap = 24f, bool preferLeft = false)
    {
        if (target == null) return;

        var parentTransform = _window.parent;
        var parent = parentTransform != null ? parentTransform.GetComponent<RectTransform>() : null;
        if (parent == null) return;

        float parentScaleX = Math.Abs(parent.lossyScale.x);
        float parentScaleY = Math.Abs(parent.lossyScale.y);
        float targetScaleX = Math.Abs(target.lossyScale.x);
        float targetScaleY = Math.Abs(target.lossyScale.y);
        if (parentScaleX < 0.0001f) parentScaleX = 1f;
        if (parentScaleY < 0.0001f) parentScaleY = 1f;
        if (targetScaleX < 0.0001f) targetScaleX = 1f;
        if (targetScaleY < 0.0001f) targetScaleY = 1f;

        Vector3 targetWorldCenter = target.TransformPoint(target.rect.center);
        Vector3 targetLocalCenter = parent.InverseTransformPoint(targetWorldCenter);

        float targetWidth = target.rect.width >= 1f ? target.rect.width : target.sizeDelta.x;
        float targetHeight = target.rect.height >= 1f ? target.rect.height : target.sizeDelta.y;
        float targetWidthLocal = targetWidth * targetScaleX / parentScaleX;
        float targetHeightLocal = targetHeight * targetScaleY / parentScaleY;

        float width = _window.sizeDelta.x;
        float height = _window.sizeDelta.y;

        float centerX = preferLeft
            ? targetLocalCenter.x - targetWidthLocal * 0.5f - gap - width * 0.5f
            : targetLocalCenter.x + targetWidthLocal * 0.5f + gap + width * 0.5f;
        float centerY = targetLocalCenter.y;

        Rect parentRect = parent.rect;
        float minX = parentRect.xMin + width * 0.5f + 16f;
        float maxX = parentRect.xMax - width * 0.5f - 16f;
        float minY = parentRect.yMin + height * 0.5f + 16f;
        float maxY = parentRect.yMax - height * 0.5f - 16f;

        centerX = Mathf.Clamp(centerX, Math.Min(minX, maxX), Math.Max(minX, maxX));
        centerY = Mathf.Clamp(centerY, Math.Min(minY, maxY), Math.Max(minY, maxY));

        _window.anchoredPosition = new Vector2(
            centerX - parentRect.center.x,
            centerY - parentRect.center.y);
    }

    public void Clear()
    {
        for (int i = _content.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(_content.GetChild(i).gameObject);

        _children.Clear();
    }

    public TextMeshProUGUI AddLabel(string text, float fontSize = 17f)
    {
        var label = UiFactory.CreateText(
            _content,
            _textTemplate,
            text,
            fontSize,
            TextAlignmentOptions.Left,
            Vector2.zero,
            Vector2.zero);

        var layout = label.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = fontSize + 8f;

        _children.Add(label.gameObject);
        return label;
    }

    public ButtonExtended AddButton(string text, Action onClick, float width = 110f, float height = 28f)
    {
        var button = UiFactory.CreateButton(_buttonTemplate, _content, "Button " + text);
        bool hasLabel = UiFactory.SetButtonLabel(button, text);
        if (!hasLabel)
        {
            var label = UiFactory.CreateText(
                button.transform,
                _textTemplate,
                text,
                14f,
                TextAlignmentOptions.Center,
                new Vector2(2f, 2f),
                new Vector2(-2f, -2f));
            label.raycastTarget = false;
        }

        var element = button.gameObject.AddComponent<LayoutElement>();
        element.preferredWidth = width;
        element.preferredHeight = height;

        button.onClick.AddListener((UnityAction)onClick);
        _children.Add(button.gameObject);
        return button;
    }

    public NativeWindowRow AddRow(string title, string detail, string actionText, Action? onClick)
    {
        var row = CreateRect(_content, "Row");
        var rowObject = row.gameObject;
        var rowHit = rowObject.AddComponent<Image>();
        rowHit.color = new Color(0f, 0f, 0f, 0f);
        rowHit.raycastTarget = true;
        var rowButton = rowObject.AddComponent<Button>();
        rowButton.targetGraphic = rowHit;
        rowButton.transition = Selectable.Transition.None;
        rowButton.interactable = true;
        if (onClick != null)
            rowButton.onClick.AddListener((UnityAction)onClick);

        var layout = rowObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.MiddleLeft;

        var element = rowObject.AddComponent<LayoutElement>();
        element.preferredHeight = 54f;

        var titleLabel = UiFactory.CreateText(
            rowObject.transform,
            _textTemplate,
            title,
            17f,
            TextAlignmentOptions.Left,
            Vector2.zero,
            Vector2.zero);
        titleLabel.enableWordWrapping = false;
        titleLabel.overflowMode = TextOverflowModes.Ellipsis;
        SetLabelLayout(titleLabel, 140f, 0f, 50f);

        var detailLabel = UiFactory.CreateText(
            rowObject.transform,
            _textTemplate,
            detail,
            14f,
            TextAlignmentOptions.Left,
            Vector2.zero,
            Vector2.zero);
        detailLabel.overflowMode = TextOverflowModes.Ellipsis;
        SetLabelLayout(detailLabel, 0f, 1f, 50f);

        Button? actionButton = null;
        if (!string.IsNullOrEmpty(actionText) && onClick != null)
            actionButton = CreateTextButton(rowObject.transform, actionText, onClick, 64f, 28f);

        _children.Add(rowObject);
        return new NativeWindowRow(rowObject, titleLabel, detailLabel, actionButton);
    }

    private Button CreateTextButton(Transform parent, string text, Action onClick, float width, float height)
    {
        var rect = CreateRect(_rectTemplate, parent, "Button " + text);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.08f, 0.35f, 0.55f, 1f);
        image.raycastTarget = true;

        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        button.interactable = true;
        button.onClick.AddListener((UnityAction)onClick);

        var label = UiFactory.CreateText(
            rect,
            _textTemplate,
            text,
            13f,
            TextAlignmentOptions.Center,
            new Vector2(2f, 1f),
            new Vector2(-2f, -1f));
        label.raycastTarget = false;

        var layout = rect.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = height;
        return button;
    }

    private static void SetLabelLayout(
        TextMeshProUGUI label,
        float preferredWidth,
        float flexibleWidth,
        float height)
    {
        var element = label.gameObject.AddComponent<LayoutElement>();
        element.preferredWidth = preferredWidth;
        element.flexibleWidth = flexibleWidth;
        element.preferredHeight = height;
    }

    private RectTransform CreateRect(Transform parent, string name)
        => CreateRect(_rectTemplate, parent, name);

    private static RectTransform CreateRect(RectTransform rectTemplate, Transform parent, string name)
    {
        var clone = UnityEngine.Object.Instantiate(rectTemplate.gameObject, parent);
        clone.name = name;

        for (int i = clone.transform.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(clone.transform.GetChild(i).gameObject);

        var components = clone.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            var component = components[i];
            if (component == null || component is Transform) continue;
            if (component is Graphic || component is CanvasRenderer)
                UnityEngine.Object.Destroy(component);
        }

        var rect = clone.GetComponent<RectTransform>();
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        return rect;
    }

    private static RectTransform CreateScrollContent(RectTransform rectTemplate, RectTransform window, Vector2 size)
    {
        var viewportRect = CreateRect(rectTemplate, window, "Scroll Viewport");
        var viewport = viewportRect.gameObject;
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(12f, 12f);
        viewportRect.offsetMax = new Vector2(-12f, -58f);

        var image = viewport.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.01f);
        image.raycastTarget = true;

        viewport.AddComponent<RectMask2D>();

        var scroll = viewport.AddComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        var contentRect = CreateRect(rectTemplate, viewport.transform, "Content");
        var content = contentRect.gameObject;
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;

        var layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 4f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = contentRect;
        return contentRect;
    }

    private static GameObject CreateSeparator(RectTransform rectTemplate, RectTransform window, float width, float y)
    {
        var rect = CreateRect(rectTemplate, window, "Separator");
        var separator = rect.gameObject;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(-24f, 1f);

        var image = separator.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.15f);
        image.raycastTarget = false;

        return separator;
    }

    private static RectTransform CreateFallbackWindow(RectTransform rectTemplate, Transform root)
    {
        var rect = CreateRect(rectTemplate, root, "Window");
        var window = rect.gameObject;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        var image = window.AddComponent<Image>();
        image.color = new Color(0.02f, 0.04f, 0.06f, 0.98f);

        return rect;
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

public sealed class NativeWindowRow
{
    private readonly GameObject _root;
    private readonly TextMeshProUGUI _title;
    private readonly TextMeshProUGUI _detail;
    private readonly Button? _button;

    internal NativeWindowRow(
        GameObject root,
        TextMeshProUGUI title,
        TextMeshProUGUI detail,
        Button? button)
    {
        _root = root;
        _title = title;
        _detail = detail;
        _button = button;
    }

    public GameObject Root => _root;

    public void Set(string title, string detail)
    {
        if (_title != null) _title.text = title ?? string.Empty;
        if (_detail != null) _detail.text = detail ?? string.Empty;
    }

    public void SetAction(string text, Action? onClick)
    {
        if (_button == null) return;

        _button.gameObject.SetActive(onClick != null);
        if (onClick == null) return;

        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener((UnityAction)onClick);
    }

    public void SetActionEnabled(bool enabled)
    {
        if (_button != null)
            _button.interactable = enabled;
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
