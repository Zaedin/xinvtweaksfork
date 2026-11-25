using System;
using Vintagestory.API.Client;

namespace XInvTweaksFork;

public static class SlotLockHotkey
{
    public const string Code = "togglelockselection";

    public static void Register(IInputAPI input, SlotLockSelection selection, string displayName,
        Action<bool>? notify = null)
    {
        input.RegisterHotKey(Code, displayName, GlKeys.L, HotkeyType.InventoryHotkeys,
            altPressed: true, ctrlPressed: true);
        input.SetHotKeyHandler(Code, _ =>
        {
            selection.ToggleEditing();
            notify?.Invoke(selection.IsEditing);
            return true;
        });
    }
}
