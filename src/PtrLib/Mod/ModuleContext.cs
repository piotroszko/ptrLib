using PtrLib.Diagnostics;
using PtrLib.Settings;
using PtrLib.Ui;

namespace PtrLib.Mod;

public sealed class ModuleContext
{
    public ModuleContext(
        ModuleLog log,
        ModuleConfig config,
        WindowManager windows,
        ModSettings settings)
    {
        Log = log;
        Config = config;
        Windows = windows;
        Settings = settings;
    }

    public ModuleLog Log { get; }
    public ModuleConfig Config { get; }
    public WindowManager Windows { get; }
    public ModSettings Settings { get; }
    public bool InGameplay { get; set; }
}
