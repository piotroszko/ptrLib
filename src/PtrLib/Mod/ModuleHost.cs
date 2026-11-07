using System;
using System.Collections.Generic;
using MelonLoader;
using PtrLib.Diagnostics;
using PtrLib.Game;
using PtrLib.Settings;
using PtrLib.Ui;

namespace PtrLib.Mod;

public sealed class ModuleHost
{
    private readonly MelonLogger.Instance _logger;
    private readonly WindowManager _windows;
    private readonly ModSettings _settings = new ModSettings();
    private readonly List<PtrModule> _modules = new List<PtrModule>();
    private readonly HashSet<PtrModule> _faulted = new HashSet<PtrModule>();
    private bool _gameplayActive;

    private readonly string _modName;

    public ModuleHost(string modName, MelonLogger.Instance logger, WindowManager windows)
    {
        _modName = modName;
        _logger = logger;
        _windows = windows;
    }

    public void Register(PtrModule module)
    {
        if (module == null)
            throw new ArgumentNullException(nameof(module));

        foreach (var existing in _modules)
        {
            if (string.Equals(existing.Id, module.Id, StringComparison.Ordinal))
                throw new InvalidOperationException($"Duplicate module id '{module.Id}'.");
        }

        var context = new ModuleContext(
            new ModuleLog(_logger, module.Id),
            new ModuleConfig(_modName, module),
            _windows,
            _settings);

        module.Attach(context);
        _modules.Add(module);
    }

    public void Initialize()
    {
        _logger.Msg($"[{_modName}] {_modules.Count} module(s) registered.");

        foreach (var module in _modules)
        {
            if (!module.Context.Config.IsEnabled)
            {
                module.Context.Log.Msg("Disabled by config.");
                continue;
            }

            Invoke(module, nameof(PtrModule.OnRegisterSettings), () => module.OnRegisterSettings(_settings));
        }

        _logger.Msg($"[{_modName}] {_settings.Sections.Count} settings section(s) registered.");
        ModuleSettingsRegistry.Register(_modName, _settings);

        foreach (var module in _modules)
        {
            if (!module.Context.Config.IsEnabled) continue;
            Invoke(module, nameof(PtrModule.OnInitialize), module.OnInitialize);
        }
    }

    public void OnUpdate()
    {

        foreach (var module in _modules)
        {
            if (!module.Context.Config.IsEnabled) continue;
            Invoke(module, nameof(PtrModule.OnUpdate), module.OnUpdate);
        }
    }

    public void OnSceneInitialized(string sceneName)
    {
        if (!string.Equals(sceneName, SceneNames.Gameplay, StringComparison.Ordinal))
            return;

        if (_gameplayActive) return;

        _gameplayActive = true;
        foreach (var module in _modules)
            module.Context.InGameplay = true;

        _logger.Msg($"[{_modName}] Gameplay scene '{sceneName}' entered.");
        foreach (var module in _modules)
        {
            if (!module.Context.Config.IsEnabled) continue;
            Invoke(module, nameof(PtrModule.OnGameplayEnter), module.OnGameplayEnter);
        }
    }

    public void OnSceneUnloaded(string sceneName)
    {
        if (!string.Equals(sceneName, SceneNames.Gameplay, StringComparison.Ordinal))
            return;

        ExitGameplay(sceneName);
    }

    private void ExitGameplay(string sceneName)
    {
        if (!_gameplayActive) return;

        _gameplayActive = false;
        foreach (var module in _modules)
            module.Context.InGameplay = false;

        _logger.Msg($"[{_modName}] Left gameplay scene ('{sceneName}').");
        foreach (var module in _modules)
        {
            if (!module.Context.Config.IsEnabled) continue;
            Invoke(module, nameof(PtrModule.OnGameplayExit), module.OnGameplayExit);
        }
    }

    public void DrawGui()
    {
        foreach (var module in _modules)
        {
            if (!module.Context.Config.IsEnabled) continue;
            Invoke(module, nameof(PtrModule.OnGui), module.OnGui);
        }

        _windows.DrawAll();
    }

    public void Shutdown()
    {

        foreach (var module in _modules)
        {
            if (!module.Context.Config.IsEnabled) continue;
            Invoke(module, nameof(PtrModule.OnShutdown), module.OnShutdown);
        }

        _windows.CloseAll();
    }

    private void Invoke(PtrModule module, string phase, Action action)
    {
        if (_faulted.Contains(module)) return;

        try
        {
            action();
        }
        catch (Exception exception)
        {
            _faulted.Add(module);
            module.Context.Log.Error($"{phase} failed; module disabled for this session.", exception);
        }
    }
}
