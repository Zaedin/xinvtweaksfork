using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Client;
using Xunit;

namespace XInvTweaksFork.Tests;

public class ClientPatchWiringTests
{
    [Fact]
    public void SlotSelectionPatchesInstallAgainstTheConfiguredGameApi()
    {
        var assembly = typeof(SlotLockSelection).Assembly;
        var patchType = assembly.GetType("XInvTweaksFork.Patches.GuiElementItemSlotGridBasePatch", true)!;
        var patchMethod = assembly.GetType("XInvTweaksFork.ManualPatch", true)!
            .GetMethod("PatchMethod", BindingFlags.Static | BindingFlags.NonPublic)!;
        var harmony = new Harmony("XInvTweaksFork.Tests." + Guid.NewGuid());
        var methodNames = new[]
        {
            "OnMouseDownOnElement", "SlotClick", "RenderInteractiveElements", "OnMouseWheel"
        };

        try
        {
            foreach (var name in methodNames)
            {
                patchMethod.Invoke(null, [
                    harmony, typeof(GuiElementItemSlotGridBase), patchType, name
                ]);
                var target = typeof(GuiElementItemSlotGridBase).GetMethod(name)!;
                var info = Harmony.GetPatchInfo(target);
                Assert.NotNull(info);
                var installed = info.Prefixes.Concat(info.Postfixes).Where(p => p.owner == harmony.Id).ToArray();
                Assert.Equal(name == "OnMouseWheel" ? 2 : 1, installed.Length);
                Assert.All(installed, patch => Assert.Equal(patchType, patch.PatchMethod.DeclaringType));
            }
        }
        finally
        {
            harmony.UnpatchAll(harmony.Id);
        }
    }
}
