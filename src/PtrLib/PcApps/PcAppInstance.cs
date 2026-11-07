using Il2Cpp;
using UnityEngine;

namespace PtrLib.PcApps;

internal sealed class PcAppInstance
{
    internal PcAppInstance(PcAppDefinition definition, ComputerShop shop, GameObject panel)
    {
        Definition = definition;
        Shop = shop;
        Panel = panel;
    }

    internal PcAppDefinition Definition { get; }
    internal ComputerShop Shop { get; }
    internal GameObject Panel { get; }

    internal void Open()
    {
        if (Panel == null) return;
        if (Shop.mainScreen != null)
        {
            Shop.ButtonReturnMainScreen();
            Shop.mainScreen.SetActive(false);
        }

        Panel.SetActive(true);
        Definition.Opened?.Invoke();
    }

    internal void Close()
    {
        if (Panel == null) return;
        Panel.SetActive(false);
        if (Shop.mainScreen != null)
            Shop.mainScreen.SetActive(true);
        Definition.Closed?.Invoke();
    }
}
