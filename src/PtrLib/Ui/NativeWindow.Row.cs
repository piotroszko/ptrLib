using System;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PtrLib.Ui;

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
}
