using System;
using Il2Cpp;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PtrLib.Ui;

public sealed partial class NativeWindow
{
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

        label.enableWordWrapping = true;

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

    public NativeWindowRow AddRow(
        string title,
        string detail,
        string actionText,
        Action? onClick,
        Sprite? icon = null)
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

        if (icon != null)
        {
            var iconRect = CreateRect(rowObject.transform, "Icon");
            var iconImage = iconRect.gameObject.AddComponent<Image>();
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            var iconLayout = iconRect.gameObject.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = 42f;
            iconLayout.preferredHeight = 42f;
        }

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

}
