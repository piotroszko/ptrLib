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
        int sortingOrder = 20)
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

        var close = UiFactory.CreateButton(template.ButtonTemplate, window, "Button Close");
        UiFactory.SetTopRight(
            close.GetComponent<RectTransform>(),
            new Vector2(34f, 34f),
            new Vector2(-12f, -10f));
        UiFactory.SetButtonLabel(close, "X");

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
        close.onClick.AddListener((UnityAction)(Action)result.Hide);

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

        Camera? targetCamera = GetCanvasCamera(target);
        Camera? parentCamera = GetCanvasCamera(parentTransform!);

        var targetCorners = new Vector3[4];
        target.GetWorldCorners(targetCorners);
        Vector2 targetBottomLeft = RectTransformUtility.WorldToScreenPoint(targetCamera, targetCorners[0]);
        Vector2 targetTopLeft = RectTransformUtility.WorldToScreenPoint(targetCamera, targetCorners[1]);
        Vector2 targetTopRight = RectTransformUtility.WorldToScreenPoint(targetCamera, targetCorners[2]);
        Vector2 targetBottomRight = RectTransformUtility.WorldToScreenPoint(targetCamera, targetCorners[3]);

        var parentCorners = new Vector3[4];
        parent.GetWorldCorners(parentCorners);
        Vector2 parentBottomLeft = RectTransformUtility.WorldToScreenPoint(parentCamera, parentCorners[0]);
        Vector2 parentTopLeft = RectTransformUtility.WorldToScreenPoint(parentCamera, parentCorners[1]);
        Vector2 parentTopRight = RectTransformUtility.WorldToScreenPoint(parentCamera, parentCorners[2]);
        Vector2 parentBottomRight = RectTransformUtility.WorldToScreenPoint(parentCamera, parentCorners[3]);

        var windowCorners = new Vector3[4];
        _window.GetWorldCorners(windowCorners);
        Vector2 windowBottomLeft = RectTransformUtility.WorldToScreenPoint(parentCamera, windowCorners[0]);
        Vector2 windowTopRight = RectTransformUtility.WorldToScreenPoint(parentCamera, windowCorners[2]);

        float targetLeft = Math.Min(Math.Min(targetBottomLeft.x, targetTopLeft.x),
            Math.Min(targetTopRight.x, targetBottomRight.x));
        float targetRight = Math.Max(Math.Max(targetBottomLeft.x, targetTopLeft.x),
            Math.Max(targetTopRight.x, targetBottomRight.x));
        float targetCenterY = (targetTopLeft.y + targetBottomRight.y) * 0.5f;

        float parentLeft = Math.Min(Math.Min(parentBottomLeft.x, parentTopLeft.x),
            Math.Min(parentTopRight.x, parentBottomRight.x));
        float parentRight = Math.Max(Math.Max(parentBottomLeft.x, parentTopLeft.x),
            Math.Max(parentTopRight.x, parentBottomRight.x));
        float parentBottom = Math.Min(Math.Min(parentBottomLeft.y, parentBottomRight.y),
            Math.Min(parentTopLeft.y, parentTopRight.y));
        float parentTop = Math.Max(Math.Max(parentBottomLeft.y, parentBottomRight.y),
            Math.Max(parentTopLeft.y, parentTopRight.y));

        float width = Math.Abs(windowTopRight.x - windowBottomLeft.x);
        float height = Math.Abs(windowTopRight.y - windowBottomLeft.y);
        if (width < 1f) width = _window.sizeDelta.x;
        if (height < 1f) height = _window.sizeDelta.y;

        float centerX;
        if (preferLeft)
            centerX = targetLeft - gap - width * 0.5f;
        else
        {
            float leftSpace = targetLeft - parentLeft;
            float rightSpace = parentRight - targetRight;
            centerX = rightSpace > leftSpace
                ? targetRight + gap + width * 0.5f
                : targetLeft - gap - width * 0.5f;
        }

        float centerY = targetCenterY;
        float minX = parentLeft + width * 0.5f + 16f;
        float maxX = parentRight - width * 0.5f - 16f;
        float minY = parentBottom + height * 0.5f + 16f;
        float maxY = parentTop - height * 0.5f - 16f;
        centerX = Mathf.Clamp(centerX, Math.Min(minX, maxX), Math.Max(minX, maxX));
        centerY = Mathf.Clamp(centerY, Math.Min(minY, maxY), Math.Max(minY, maxY));

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent,
                new Vector2(centerX, centerY),
                parentCamera,
                out Vector2 local))
        {
            _window.anchoredPosition = local;
        }
    }

    private static Camera? GetCanvasCamera(Transform transform)
    {
        var canvas = transform.GetComponentInParent<Canvas>();
        return canvas != null ? canvas.worldCamera : null;
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
        UiFactory.SetButtonLabel(button, text);

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

        ButtonExtended? button = null;
        if (!string.IsNullOrEmpty(actionText) && onClick != null)
        {
            button = UiFactory.CreateButton(_buttonTemplate, rowObject.transform, "Button " + actionText);
            UiFactory.SetButtonLabel(button, actionText);

            var buttonLayout = button.gameObject.AddComponent<LayoutElement>();
            buttonLayout.preferredWidth = 54f;
            buttonLayout.preferredHeight = 26f;
            button.onClick.AddListener((UnityAction)onClick);
        }

        _children.Add(rowObject);
        return new NativeWindowRow(rowObject, titleLabel, detailLabel, button);
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
    private readonly ButtonExtended? _button;

    internal NativeWindowRow(
        GameObject root,
        TextMeshProUGUI title,
        TextMeshProUGUI detail,
        ButtonExtended? button)
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

        UiFactory.SetButtonLabel(_button, text);
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
