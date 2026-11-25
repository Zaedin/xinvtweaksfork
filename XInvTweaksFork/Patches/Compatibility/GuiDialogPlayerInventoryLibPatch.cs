using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace XInvTweaksFork.Patches.Compatibility;

internal class GuiDialogPlayerInventoryLibPatch
{
    public static void OnGuiOpenedPostfix(object  __instance)
    {
        var type = __instance.GetType();
        var api = type.GetField("_api", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(__instance);
        var composer = type.GetField("_composer", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(__instance);

        if (api == null || composer == null) return;
        if (api is not ICoreAPI coreApi) return;
        if (composer.GetType().GetProperty("Bounds")?.GetValue(composer) is not ElementBounds bounds) return;

        coreApi.ModLoader.GetModSystem<XInvTweaksForkModSystem>()?.OnInventoryOpened(bounds);
    }

    public static void OnGuiClosedPostfix(object  __instance)
    {
        var type = __instance.GetType();
        var api = type.GetField("_api", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(__instance);

        if (api is not ICoreAPI coreApi) return;

        coreApi.ModLoader.GetModSystem<XInvTweaksForkModSystem>()?.OnInventoryClosed();
    }
}