using System.Collections.Generic;

namespace PtrLib.Ui;

public sealed class WindowManager
{
    private readonly List<PtrWindow> _windows = new List<PtrWindow>();
    private int _nextId = 1000;

    public PtrWindow Show(string title, string body, float width, float height)
    {
        var window = new PtrWindow(_nextId++, title, body, width, height);
        _windows.Add(window);
        return window;
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
    }

    public void DrawAll()
    {
        for (int i = _windows.Count - 1; i >= 0; i--)
            _windows[i].Draw();
    }
}
