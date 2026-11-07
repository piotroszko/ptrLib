using System;
using System.Collections.Generic;
using Il2Cpp;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PtrLib.Ui;

/// <summary>
/// A runtime-built window that reuses the game's native window visuals
/// (background, title font and button style).
/// </summary>
public sealed partial class NativeWindow
{
    private readonly GameObject _root;
    private readonly RectTransform _window;
    private readonly RectTransform _content;
    private readonly TextMeshProUGUI _textTemplate;
    private readonly ButtonExtended _buttonTemplate;
    private readonly TextMeshProUGUI _title;
    private readonly RectTransform _rectTemplate;
    private readonly List<GameObject> _children = new List<GameObject>();
    private string _positionKey;
    private bool _hasSavedPosition;
    private GameObject? _viewport;
    private GameObject? _separator;
    private GameObject? _resizeGrip;
    private GameObject? _scrollbar;
    private TextMeshProUGUI? _minimizeLabel;
    private bool _minimized;
    private float _expandedHeight;
    private float _currentWidth;
    private float _currentHeight;

    private NativeWindow(
        GameObject root,
        RectTransform window,
        RectTransform content,
        TextMeshProUGUI title,
        TextMeshProUGUI textTemplate,
        ButtonExtended buttonTemplate,
        RectTransform rectTemplate,
        string positionKey)
    {
        _root = root;
        _window = window;
        _content = content;
        _title = title;
        _textTemplate = textTemplate;
        _buttonTemplate = buttonTemplate;
        _rectTemplate = rectTemplate;
        _positionKey = positionKey;
    }

    public GameObject Root => _root;
    public RectTransform WindowRect => _window;
    public RectTransform ContentRect => _content;
    public bool IsVisible => _root != null && _root.activeSelf;
    public string PositionKey => _positionKey;
    public bool HasSavedPosition => _hasSavedPosition;

    public void SetTitle(string title)
    {
        if (_title != null)
            _title.text = title ?? string.Empty;
    }


}
