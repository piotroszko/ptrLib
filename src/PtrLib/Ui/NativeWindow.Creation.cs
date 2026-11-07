using System;
using Il2Cpp;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PtrLib.Ui;

public sealed partial class NativeWindow
{
    public static NativeWindow? Create(
        string name,
        string title,
        Vector2 size,
        Vector2 anchoredPosition,
        int sortingOrder = 20,
        bool showCloseButton = true,
        string? positionKey = null)
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
        var titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.offsetMin = new Vector2(18f, -50f);
        titleRect.offsetMax = new Vector2(showCloseButton ? -94f : -54f, -12f);

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
        var scrollbarObject = window.Find("Scrollbar");
        var result = new NativeWindow(
            root,
            window,
            content,
            titleText,
            template.TextTemplate,
            template.ButtonTemplate,
            template.RectTemplate,
            string.IsNullOrEmpty(positionKey) ? name : positionKey);
        result._scrollbar = scrollbarObject != null ? scrollbarObject.gameObject : null;
        if (closeButton != null)
            closeButton.onClick.AddListener((UnityAction)(Action)result.Hide);

        try
        {
            var minimizeRect = CreateRect(template.RectTemplate, window, "Button Minimize");
            minimizeRect.anchorMin = new Vector2(1f, 1f);
            minimizeRect.anchorMax = new Vector2(1f, 1f);
            minimizeRect.pivot = new Vector2(1f, 1f);
            minimizeRect.sizeDelta = new Vector2(34f, 34f);
            minimizeRect.anchoredPosition = new Vector2(showCloseButton ? -52f : -12f, -10f);

            var minimizeImage = minimizeRect.gameObject.AddComponent<Image>();
            minimizeImage.color = new Color(1f, 1f, 1f, 0.1f);
            minimizeImage.raycastTarget = true;

            var minimizeButton = minimizeRect.gameObject.AddComponent<Button>();
            minimizeButton.targetGraphic = minimizeImage;
            minimizeButton.transition = Selectable.Transition.None;
            minimizeButton.onClick.AddListener((UnityAction)(Action)result.ToggleMinimized);

            result._minimizeLabel = UiFactory.CreateText(
                minimizeRect,
                template.TextTemplate,
                "-",
                19f,
                TextAlignmentOptions.Center,
                Vector2.zero,
                Vector2.zero);
            result._minimizeLabel.raycastTarget = false;
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogWarning($"[PtrLib] Minimize button setup failed: {exception}");
        }

        try
        {
            var dragRect = CreateRect(template.RectTemplate, window, "Drag Bar");
            dragRect.anchorMin = new Vector2(0f, 1f);
            dragRect.anchorMax = new Vector2(1f, 1f);
            dragRect.pivot = new Vector2(0.5f, 1f);
            dragRect.sizeDelta = new Vector2(showCloseButton ? -120f : -80f, 46f);
            dragRect.anchoredPosition = Vector2.zero;
            var dragImage = dragRect.gameObject.AddComponent<Image>();
            dragImage.color = new Color(0f, 0f, 0f, 0f);
            dragImage.raycastTarget = true;
            var trigger = dragRect.gameObject.AddComponent<EventTrigger>();
            if (trigger.triggers == null)
                trigger.triggers = new Il2CppSystem.Collections.Generic.List<EventTrigger.Entry>();
            var dragEntry = NewTriggerEntry(EventTriggerType.Drag);
            dragEntry.callback.AddListener((UnityAction<BaseEventData>)(data =>
            {
                DragWindow(window, data);
                result.StorePosition();
            }));
            trigger.triggers.Add(dragEntry);
            var endEntry = NewTriggerEntry(EventTriggerType.EndDrag);
            endEntry.callback.AddListener((UnityAction<BaseEventData>)(_ => result.SavePosition()));
            trigger.triggers.Add(endEntry);
            result._children.Add(dragRect.gameObject);
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogWarning($"[PtrLib] Drag bar setup failed: {exception}");
        }
        try
        {
            var gripRect = CreateRect(template.RectTemplate, window, "Resize Grip");
            gripRect.anchorMin = new Vector2(1f, 0f);
            gripRect.anchorMax = new Vector2(1f, 0f);
            gripRect.pivot = new Vector2(1f, 0f);
            gripRect.sizeDelta = new Vector2(14f, 14f);
            gripRect.anchoredPosition = Vector2.zero;
            var gripImage = gripRect.gameObject.AddComponent<Image>();
            gripImage.color = new Color(1f, 1f, 1f, 0.18f);
            gripImage.raycastTarget = true;
            var gripTrigger = gripRect.gameObject.AddComponent<EventTrigger>();
            if (gripTrigger.triggers == null)
                gripTrigger.triggers = new Il2CppSystem.Collections.Generic.List<EventTrigger.Entry>();
            var gripDrag = NewTriggerEntry(EventTriggerType.Drag);
            gripDrag.callback.AddListener((UnityAction<BaseEventData>)(data =>
                result.ResizeWindow(data)));
            gripTrigger.triggers.Add(gripDrag);
            var gripEnd = NewTriggerEntry(EventTriggerType.EndDrag);
            gripEnd.callback.AddListener((UnityAction<BaseEventData>)(_ => result.SavePosition()));
            gripTrigger.triggers.Add(gripEnd);
            gripRect.SetAsLastSibling();
            result._resizeGrip = gripRect.gameObject;
            result._children.Add(gripRect.gameObject);
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogWarning($"[PtrLib] Resize grip setup failed: {exception}");
        }

        result._viewport = content.parent != null ? content.parent.gameObject : null;
        result._separator = separator;
        result._currentWidth = size.x;
        result._currentHeight = size.y;
        result._expandedHeight = size.y;
        result.TryLoadSavedPosition();
        result._children.Add(separator);
        result.Show(false);
        return result;
    }

    private static RectTransform CreateScrollContent(RectTransform rectTemplate, RectTransform window, Vector2 size)
    {
        var viewportRect = CreateRect(rectTemplate, window, "Scroll Viewport");
        var viewport = viewportRect.gameObject;
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(12f, 12f);
        viewportRect.offsetMax = new Vector2(-24f, -58f);

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

        var scrollbarRect = CreateRect(rectTemplate, window, "Scrollbar");
        scrollbarRect.anchorMin = new Vector2(1f, 0f);
        scrollbarRect.anchorMax = new Vector2(1f, 1f);
        scrollbarRect.pivot = new Vector2(1f, 0.5f);
        scrollbarRect.offsetMin = new Vector2(-14f, 12f);
        scrollbarRect.offsetMax = new Vector2(-8f, -58f);

        var scrollbarTrack = scrollbarRect.gameObject.AddComponent<Image>();
        scrollbarTrack.color = new Color(1f, 1f, 1f, 0.08f);
        scrollbarTrack.raycastTarget = true;

        var handleRect = CreateRect(rectTemplate, scrollbarRect, "Handle");
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.one;
        handleRect.offsetMin = Vector2.zero;
        handleRect.offsetMax = Vector2.zero;

        var handleImage = handleRect.gameObject.AddComponent<Image>();
        handleImage.color = new Color(1f, 1f, 1f, 0.55f);
        handleImage.raycastTarget = true;

        var scrollbar = scrollbarRect.gameObject.AddComponent<Scrollbar>();
        scrollbar.handleRect = handleRect;
        scrollbar.targetGraphic = handleImage;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.transition = Selectable.Transition.None;
        scrollbar.size = 1f;

        scroll.verticalScrollbar = scrollbar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

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
    private static EventTrigger.Entry NewTriggerEntry(EventTriggerType type)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        if (entry.callback == null)
            entry.callback = new EventTrigger.TriggerEvent();
        return entry;
    }

    private static void DragWindow(RectTransform window, BaseEventData data)
    {
        var pointer = data.TryCast<PointerEventData>();
        if (pointer == null || window == null)
            return;

        var canvas = window.GetComponentInParent<Canvas>();
        float scale = canvas != null && canvas.scaleFactor > 0.0001f
            ? canvas.scaleFactor
            : 1f;
        window.anchoredPosition = ClampToParent(
            window.anchoredPosition + pointer.delta / scale,
            window);
    }
}
