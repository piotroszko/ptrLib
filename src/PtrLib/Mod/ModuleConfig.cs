using MelonLoader;

namespace PtrLib.Mod;

public sealed class ModuleConfig
{
    public MelonPreferences_Category Category { get; }
    public MelonPreferences_Entry<bool> Enabled { get; }

    public ModuleConfig(string modName, PtrModule module)
    {
        Category = MelonPreferences.CreateCategory(
            $"{modName}_{module.Id}",
            $"{modName}: {module.DisplayName}");
        Enabled = Category.CreateEntry("Enabled", module.EnabledByDefault, "Enabled");
    }

    public bool IsEnabled => Enabled.Value;
}
