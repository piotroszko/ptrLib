using PtrLib.Mod;

namespace PtrLib.Items;

internal sealed class ModItemsModule : PtrModule
{
    public override string Id => "ModItems";
    public override string DisplayName => "Mod Items";

    public override void OnGameplayEnter()
    {
        ModItemRegistry.ResetCache();
    }
}
