using System;
using Il2Cpp;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PtrLib.Ui;

public sealed partial class NativeWindow
{
    public void ToggleMinimized() => SetMinimized(!_minimized);

    public void SetMinimized(bool minimized)
    {
        if (_window == null || _minimized == minimized)
            return;

        if (minimized)
        {
            _expandedHeight = _currentHeight;
            _currentHeight = 54f;
            _window.sizeDelta = new Vector2(_currentWidth, _currentHeight);
            _viewport?.SetActive(false);
            _separator?.SetActive(false);
            _resizeGrip?.SetActive(false);
            _scrollbar?.SetActive(false);
            _minimized = true;
        }
        else
        {
            _currentHeight = Mathf.Max(120f, _expandedHeight);
            _window.sizeDelta = new Vector2(_currentWidth, _currentHeight);
            _viewport?.SetActive(true);
            _separator?.SetActive(true);
            _resizeGrip?.SetActive(true);
            _scrollbar?.SetActive(true);
            _minimized = false;
        }

        _window.anchoredPosition = ClampToParent(_window.anchoredPosition, _window);

        SavePosition();

        if (_minimizeLabel != null)
            _minimizeLabel.text = _minimized ? "+" : "-";
    }

    private void ResizeWindow(BaseEventData data)
    {
        if (_window == null || _minimized)
            return;

        var pointer = data.TryCast<PointerEventData>();
        var parent = _window.parent as RectTransform;
        if (pointer == null || parent == null)
            return;

        var canvas = _window.GetComponentInParent<Canvas>();
        float scale = canvas != null && canvas.scaleFactor > 0.0001f
            ? canvas.scaleFactor
            : 1f;
        Vector2 delta = pointer.delta / scale;
        float maxWidth = Mathf.Max(320f, parent.rect.width - 16f);
        float maxHeight = Mathf.Max(140f, parent.rect.height - 16f);
        float width = Mathf.Clamp(_currentWidth + delta.x, 320f, maxWidth);
        float height = Mathf.Clamp(_currentHeight - delta.y, 140f, maxHeight);
        _currentWidth = width;
        _currentHeight = height;
        _window.sizeDelta = new Vector2(width, height);
        _window.anchoredPosition = ClampToParent(_window.anchoredPosition, _window);
        _expandedHeight = height;
        StorePosition();
    }

    public void SetPositionKey(string key)
    {
        if (string.IsNullOrEmpty(key) || string.Equals(key, _positionKey, StringComparison.Ordinal))
            return;

        SetMinimized(false);
        SavePosition();
        _positionKey = key;
        _hasSavedPosition = TryLoadSavedPosition();
    }

    internal void SavePosition()
    {
        StorePosition();
        PlayerPrefs.Save();
    }

    private void StorePosition()
    {
        if (string.IsNullOrEmpty(_positionKey) || _window == null)
            return;

        string prefix = PositionPrefix(_positionKey);
        PlayerPrefs.SetFloat(prefix + ".x", _window.anchoredPosition.x);
        PlayerPrefs.SetFloat(prefix + ".y", _window.anchoredPosition.y);
        PlayerPrefs.SetFloat(prefix + ".w", _currentWidth);
        PlayerPrefs.SetFloat(prefix + ".h", _minimized ? _expandedHeight : _currentHeight);
    }

    private bool TryLoadSavedPosition()
    {
        if (string.IsNullOrEmpty(_positionKey) || _window == null)
            return false;

        string prefix = PositionPrefix(_positionKey);
        bool hasPosition = PlayerPrefs.HasKey(prefix + ".x") && PlayerPrefs.HasKey(prefix + ".y");
        bool centerPending = PlayerPrefs.GetInt(prefix + ".center", 0) != 0;
        if (!hasPosition && !centerPending)
            return false;

        if (PlayerPrefs.HasKey(prefix + ".w") && PlayerPrefs.HasKey(prefix + ".h"))
        {
            float width = PlayerPrefs.GetFloat(prefix + ".w");
            float height = PlayerPrefs.GetFloat(prefix + ".h");
            if (width >= 100f && height >= 80f)
            {
                var parent = _window.parent as RectTransform;
                if (parent != null)
                {
                    width = Mathf.Clamp(width, 320f, Mathf.Max(320f, parent.rect.width - 16f));
                    height = Mathf.Clamp(height, 140f, Mathf.Max(140f, parent.rect.height - 16f));
                }

                _window.sizeDelta = new Vector2(width, height);
                _currentWidth = width;
                _currentHeight = height;
                _expandedHeight = height;
            }
        }

        if (centerPending)
        {
            _window.anchoredPosition = Vector2.zero;
            PlayerPrefs.DeleteKey(prefix + ".center");
            _hasSavedPosition = true;
            StorePosition();
            PlayerPrefs.Save();
            return true;
        }

        _window.anchoredPosition = ClampToParent(
            new Vector2(
                PlayerPrefs.GetFloat(prefix + ".x"),
                PlayerPrefs.GetFloat(prefix + ".y")),
            _window);
        return true;
    }

    public void CenterInParent()
    {
        if (_window == null)
            return;

        _window.anchoredPosition = Vector2.zero;
        _hasSavedPosition = true;
        SavePosition();
    }

    public static void ClearSavedPosition(string key)
    {
        if (string.IsNullOrEmpty(key))
            return;

        string prefix = PositionPrefix(key);
        PlayerPrefs.DeleteKey(prefix + ".x");
        PlayerPrefs.DeleteKey(prefix + ".y");
        PlayerPrefs.DeleteKey(prefix + ".w");
        PlayerPrefs.DeleteKey(prefix + ".h");
        PlayerPrefs.DeleteKey(prefix + ".center");
        PlayerPrefs.Save();
    }

    public static void RequestCenter(string key)
    {
        if (string.IsNullOrEmpty(key))
            return;

        PlayerPrefs.SetInt(PositionPrefix(key) + ".center", 1);
        PlayerPrefs.Save();
    }

    private static string PositionPrefix(string key) => "PtrPack.Window." + key;

    internal static Vector2 ClampToParent(Vector2 position, RectTransform window)
    {
        var parent = window.parent as RectTransform;
        if (parent == null)
            return position;

        float width = window.rect.width >= 1f ? window.rect.width : window.sizeDelta.x;
        float height = window.rect.height >= 1f ? window.rect.height : window.sizeDelta.y;
        Rect rect = parent.rect;
        float minX = rect.xMin + width * 0.5f + 8f;
        float maxX = rect.xMax - width * 0.5f - 8f;
        float minY = rect.yMin + height * 0.5f + 8f;
        float maxY = rect.yMax - height * 0.5f - 8f;

        return new Vector2(
            Mathf.Clamp(position.x, Mathf.Min(minX, maxX), Mathf.Max(minX, maxX)),
            Mathf.Clamp(position.y, Mathf.Min(minY, maxY), Mathf.Max(minY, maxY)));
    }

    public void Show(bool show = true)
    {
        if (_root == null) return;
        _root.SetActive(show);
    }

    public void Hide()
    {
        SavePosition();
        Show(false);
    }

    public void Close()
    {
        SavePosition();
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
}
