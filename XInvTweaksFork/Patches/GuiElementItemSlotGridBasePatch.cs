using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace XInvTweaksFork.Patches;

internal class GuiElementItemSlotGridBasePatch
{
    private static bool IsOwnBackpack(ICoreClientAPI api, IInventory inventory)
    {
        return ReferenceEquals(inventory,
            api.World.Player.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName));
    }

    public static bool OnMouseDownOnElementPrefix(GuiElementItemSlotGridBase __instance,
        IInventory ___inventory, ICoreClientAPI api, MouseEvent args)
    {
        var system = api.ModLoader.GetModSystem<XInvTweaksForkModSystem>();
        if (system.SlotLocks?.IsEditing != true || !IsOwnBackpack(api, ___inventory)) return true;
        if (args.Handled || !__instance.IsPositionInside(args.X, args.Y) ||
            !__instance.Bounds.ParentBounds.PointInside(args.X, args.Y)) return true;

        for (var i = 0; i < __instance.SlotBounds.Length && i < __instance.renderedSlots.Count; i++)
        {
            if (!__instance.SlotBounds[i].PointInside(args.X, args.Y)) continue;
            var slotId = __instance.renderedSlots.GetKeyAtIndex(i);
            // bag change can leave old grid visible until next composition
            if (slotId < 0 || slotId >= ___inventory.Count)
            {
                args.Handled = true;
                return false;
            }
            if (!SlotLockSelection.IsStorageSlot(slotId, ___inventory.Count)) return true;

            if (args.Button == EnumMouseButton.Left && system.SlotLocks.TryToggle(slotId, ___inventory.Count))
                system.SaveSlotLocks();

            args.Handled = true;
            return false;
        }

        return true;
    }

    // also block keyboard activation and dragging while selecting locks
    public static bool SlotClickPrefix(IInventory ___inventory, ICoreClientAPI api, int slotId)
    {
        if (api.ModLoader.GetModSystem<XInvTweaksForkModSystem>().SlotLocks?.IsEditing != true ||
            !IsOwnBackpack(api, ___inventory)) return true;
        return slotId is >= 0 and < SlotLockSelection.BackpackFirstSlot && slotId < ___inventory.Count;
    }

    public static void RenderInteractiveElementsPostfix(GuiElementItemSlotGridBase __instance,
        IInventory ___inventory, ICoreClientAPI ___api)
    {
        if (!IsOwnBackpack(___api, ___inventory)) return;
        var overlay = ___api.ModLoader.GetModSystem<XInvTweaksForkModSystem>().LockOverlay;
        for (var i = 0; i < __instance.SlotBounds.Length && i < __instance.renderedSlots.Count; i++)
        {
            var slotId = __instance.renderedSlots.GetKeyAtIndex(i);
            if (!SlotLockSelection.IsLocked(XInvTweaksForkModSystem.Config.LockedSlots,
                    slotId, ___inventory.Count)) continue;
            var bounds = __instance.SlotBounds[i];
            if (!bounds.PartiallyInside(__instance.Bounds.ParentBounds)) continue;
            ___api.Render.PushScissor(__instance.InsideClipBounds ?? __instance.Bounds.ParentBounds, true);
            try
            {
                overlay?.Render(bounds);
            }
            finally
            {
                ___api.Render.PopScissor();
            }
        }
    }

    public static bool OnMouseWheelPrefix(GuiElementItemSlotGridBase __instance,
        IInventory ___inventory, ICoreClientAPI api, MouseWheelEventArgs args)
    {
        if (api.ModLoader.GetModSystem<XInvTweaksForkModSystem>().SlotLocks?.IsEditing != true ||
            !IsOwnBackpack(api, ___inventory) ||
            !__instance.IsPositionInside(api.Input.MouseX, api.Input.MouseY)) return true;
        args.SetHandled();
        return false;
    }

    public static void OnMouseWheelPostfix(GuiElementItemSlotGridBase __instance, ElementBounds[] ___SlotBounds,
        Vintagestory.API.Datastructures.OrderedDictionary<int, ItemSlot> ___renderedSlots, IInventory ___inventory,
        Action<object> ___SendPacketHandler, ICoreClientAPI api, MouseWheelEventArgs args)
    {
        if (args.IsHandled || !XInvTweaksForkModSystem.Config.PushPullWheel) return;
        var ctrl = api.Input.KeyboardKeyState[(int)GlKeys.ShiftLeft] ||
                   api.Input.KeyboardKeyState[(int)GlKeys.ShiftRight];
        if (!ctrl || !__instance.KeyboardControlEnabled ||
            !__instance.IsPositionInside(api.Input.MouseX, api.Input.MouseY)) return;
        if (__instance.hoverSlotId < 0 || __instance.hoverSlotId >= ___inventory.Count) return;
        if (___inventory[__instance.hoverSlotId] is { Empty: true }) return;
        OnMouseWheel(api, ___inventory[__instance.hoverSlotId], args.delta, ___SendPacketHandler);
        args.SetHandled();
    }

    private static void OnMouseWheel(ICoreClientAPI api, ItemSlot? source, int wheelDelta,
        Action<object> sendPacketHandler)
    {
        if (source == null) return;
        if (api.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative) return;
        var packet = wheelDelta > 0 ? PushItem(api, source) : PullItem(api, source);
        if(packet == null) return;
        sendPacketHandler?.Invoke(packet);
    }

    private static object? PushItem(ICoreClientAPI api, ItemSlot source)
    {
        var op = new ItemStackMoveOperation(api.World, EnumMouseButton.Wheel, 0, EnumMergePriority.AutoMerge, 1)
        {
            ActingPlayer = api.World.Player
        };
        var target = api.World.Player.InventoryManager.GetBestSuitedSlot(source, false);
        return api.World.Player.InventoryManager.TryTransferTo(source, target, ref op);
    }

    private static object? PullItem(ICoreClientAPI api, ItemSlot target)
    {
        if (target?.Itemstack == null) return null;
        List<IInventory> inventories = api.World.Player.InventoryManager.OpenedInventories;
        ItemSlot? bestSource = null;

        foreach (var inventory in inventories.Where(inventory => inventory != target.Inventory || bestSource == null)
                     .Where(_ => target.Inventory is not InventoryBasePlayer ||
                                         bestSource?.Inventory is not InventoryBasePlayer))
        {
            foreach (var slot in inventory)
            {
                if (slot == target) continue;
                if (slot.Itemstack?.Collectible != target.Itemstack.Collectible) continue;
                bestSource = slot;
                break;
            }

            if (bestSource?.Inventory is InventoryBasePlayer && bestSource?.Inventory != target.Inventory) break;
        }

        if (bestSource == null) return null;
        var op = new ItemStackMoveOperation(api.World, EnumMouseButton.Wheel, 0, EnumMergePriority.AutoMerge, 1)
            {
                ActingPlayer = api.World.Player
            };
        return api.World.Player.InventoryManager.TryTransferTo(bestSource, target, ref op);
    }
}
