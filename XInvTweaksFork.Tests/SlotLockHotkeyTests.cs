using System;
using System.Collections.Generic;
using System.Reflection;
using Vintagestory.API.Client;
using Xunit;

namespace XInvTweaksFork.Tests;

public class SlotLockHotkeyTests
{
    public class InputCapture : DispatchProxy
    {
        public object?[] Registration = [];
        public Delegate Handler = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod!.Name == "RegisterHotKey")
            {
                Registration = args!;
                return null;
            }
            if (targetMethod.Name == "SetHotKeyHandler")
            {
                Assert.Equal(SlotLockHotkey.Code, args![0]);
                Handler = (Delegate)args[1]!;
                return null;
            }
            throw new InvalidOperationException(targetMethod.Name);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void HotkeyTogglesSelectionWithoutDisablingLocksOrRequiringExtendedUi(bool extendUi)
    {
        var config = new InvTweaksConfig { ExtendChestUi = extendUi, PushPullWheel = false };
        config.LockedSlots.Add(19);
        var selection = new SlotLockSelection(config.LockedSlots);
        var input = DispatchProxy.Create<IInputAPI, InputCapture>();
        var capture = (InputCapture)(object)input;
        var notifications = new List<bool>();
        SlotLockHotkey.Register(input, selection, "Toggle slot-lock selection", notifications.Add);

        Assert.Equal(SlotLockHotkey.Code, capture.Registration[0]);
        Assert.Equal(GlKeys.L, capture.Registration[2]);
        Assert.Equal(HotkeyType.InventoryHotkeys, capture.Registration[3]);
        Assert.Equal(true, capture.Registration[4]); // Alt
        Assert.Equal(true, capture.Registration[5]); // Ctrl
        Assert.Equal(false, capture.Registration[6]); // Shift

        Assert.Equal(true, capture.Handler.DynamicInvoke(new KeyCombination()));
        Assert.True(selection.IsEditing);
        Assert.Equal(true, capture.Handler.DynamicInvoke(new KeyCombination()));
        Assert.False(selection.IsEditing);
        Assert.Equal(new[] { true, false }, notifications);
        Assert.True(SlotLockSelection.IsLocked(config.LockedSlots, 23, 24));
        Assert.Equal(extendUi, config.ExtendChestUi);
    }

    [Fact]
    public void ButtonHotkeyAndCloseShareOneStateNotification()
    {
        var selection = new SlotLockSelection(new SortedSet<int> { 0 });
        var observed = new List<bool>();
        selection.EditingChanged += () => observed.Add(selection.IsEditing);
        selection.ToggleEditing();
        selection.EndEditing();
        selection.EndEditing();
        selection.ToggleEditing();
        selection.ToggleEditing();
        Assert.Equal(new[] { true, false, true, false }, observed);
    }
}
