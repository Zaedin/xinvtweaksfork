using Vintagestory.API.Client;

namespace XInvTweaksFork.Patches;

internal class GuiDialogBlockEntityInventoryPatch
{
    public static void OnGuiOpenedPostfix(GuiDialogBlockEntityInventory __instance)
    {
        var system = __instance.Inventory?.Api?.ModLoader.GetModSystem<XInvTweaksForkModSystem>();
        system?.OnInventoryOpened(__instance.SingleComposer.Bounds);
    }

    public static void OnGuiClosedPostfix(GuiDialogBlockEntityInventory __instance)
    {
        var system = __instance.Inventory?.Api?.ModLoader.GetModSystem<XInvTweaksForkModSystem>();
        system?.OnInventoryClosed();
    }
}