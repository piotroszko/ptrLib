using MelonLoader;

[assembly: MelonInfo(typeof(PtrLib.PtrLibMod), PtrLib.LibInfo.Name, PtrLib.LibInfo.Version, PtrLib.LibInfo.Author)]
[assembly: MelonGame("Waseku", "Data Center")]

namespace PtrLib;

internal static class LibInfo
{
    internal const string Name = "PtrLib";
    internal const string Version = "0.1.0";
    internal const string Author = "piotr";
}

public sealed class PtrLibMod : MelonMod
{
    public override void OnInitializeMelon()
    {
        LoggerInstance.Msg($"{LibInfo.Name} {LibInfo.Version} loaded.");
    }
}
