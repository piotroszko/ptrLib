using PtrLib.Settings;
namespace PtrLib.Mod;



public abstract class PtrModule
{
    public abstract string Id { get; }
    public abstract string DisplayName { get; }
    public virtual bool EnabledByDefault => true;
    public ModuleContext Context { get; private set; } = null!;

    public void Attach(ModuleContext context) => Context = context;

    public virtual void OnRegisterSettings(ModSettings settings) { }
    public virtual void OnInitialize() { }
    public virtual void OnUpdate() { }
    public virtual void OnGameplayEnter() { }
    public virtual void OnGameplayExit() { }
    public virtual void OnGui() { }
    public virtual void OnShutdown() { }
}
