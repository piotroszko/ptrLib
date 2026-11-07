using System;
using MelonLoader;

namespace PtrLib.Diagnostics;

public sealed class ModuleLog
{
    private readonly MelonLogger.Instance _logger;
    private readonly string _tag;

    public ModuleLog(MelonLogger.Instance logger, string tag)
    {
        _logger = logger;
        _tag = tag;
    }

    public void Msg(string message)
        => _logger.Msg($"[{_tag}] {message}");

    public void Warning(string message)
        => _logger.Warning($"[{_tag}] {message}");

    public void Error(string message, Exception? exception = null)
        => _logger.Error(exception == null
            ? $"[{_tag}] {message}"
            : $"[{_tag}] {message}{Environment.NewLine}{exception}");
}
