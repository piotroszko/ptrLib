using System.Collections.Generic;

using UnityEngine;

namespace PtrLib.Ui;

public sealed class WindowManager
{
    private readonly List<PtrWindow> _windows = new List<PtrWindow>();
    private readonly List<NativeWindow> _nativeWindows = new List<NativeWindow>();
    private int _nextId = 1000;

    public PtrWindow Show(string title, string body, float width, float height)
    {
        var window = new PtrWindow(_nextId++, title, body, width, height);
        _windows.Add(window);
        return window;
    }

    public NativeWindow? ShowNative(
        string name,
        string title,
        Vector2 size,
        Vector2 anchoredPosition,
        int sortingOrder = 20)
    {
        var window = NativeWindow.Create(name, title, size, anchoredPosition, sortingOrder);
        if (window != null)
            _nativeWindows.Add(window);
        return window;
    }

    public void Close(NativeWindow window)
    {
        if (window == null) return;
        window.Close();
        _nativeWindows.Remove(window);
    }

    public void Close(PtrWindow window)
    {
        window.Visible = false;
        _windows.Remove(window);
    }

    public void CloseAll()
    {
        foreach (var window in _windows)
            window.Visible = false;
        _windows.Clear();

        for (int i = _nativeWindows.Count - 1; i >= 0; i--)
            _nativeWindows[i].Close();
        _nativeWindows.Clear();
    }

    public void DrawAll()
    {
        for (int i = _windows.Count - 1; i >= 0; i--)
            _windows[i].Draw();
    }
}
