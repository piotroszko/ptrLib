using Il2Cpp;
using PtrLib.Mod;
using PtrLib.Ui;

namespace PtrLib.PcApps;

internal sealed class PcAppsModule : PtrModule
{
    public override string Id => "PcApps";
    public override string DisplayName => "PC Apps";

    public override void OnGameplayEnter()
    {
        PcApps.Reset();
    }

    public override void OnUpdate()
    {
        if (!Context.InGameplay) return;

        var shop = UiFactory.FindSceneComponent<ComputerShop>();
        if (shop == null || shop.mainScreen == null) return;

        PcApps.EnsureInstalled(shop, Context.Log);
    }
}
