using System;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Client;
using Xunit;

namespace XInvTweaksFork.Tests;

public class IndependentSlotLockPatchTests
{
    [Theory]
    [InlineData("RenderInteractiveElements")]
    [InlineData("OnMouseDownOnElement")]
    [InlineData("SlotClick")]
    [InlineData("OnMouseWheel")]
    [InlineData("OnGuiClosed")]
    public void LockBindingsInstallWithoutAnyUiOrWheelConfigPrerequisite(string method)
    {
        var harmony = new Harmony("XInvTweaksFork.IndependentLocks." + Guid.NewGuid());
        try
        {
            // The production installer requires only Harmony, not the dialog or any config flag.
            typeof(XInvTweaksForkModSystem).GetMethod("PatchSlotLocks",
                BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, new object[] { harmony });
            var info = Harmony.GetPatchInfo(typeof(GuiElementItemSlotGridBase).GetMethod(method)!);
            Assert.NotNull(info);
            Assert.Contains(harmony.Id, info.Owners);
            if (method == "RenderInteractiveElements" || method == "OnGuiClosed")
                Assert.Contains(info.Postfixes, p => p.owner == harmony.Id);
            else
                Assert.Contains(info.Prefixes, p => p.owner == harmony.Id);
        }
        finally
        {
            harmony.UnpatchAll(harmony.Id);
        }
    }
}
