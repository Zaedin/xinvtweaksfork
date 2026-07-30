using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Common;

namespace XInvTweaksFork.Patches;

internal class InventoryBasePatch
{
    /// <summary>
    /// Number of bag-equipment slots at the start of the player backpack inventory.
    /// Must stay in sync with <see cref="InventoryUtil.SortBackpack"/>.
    /// </summary>
    private const int BackpackBagSlotCount = 4;

    private static bool ActivateSlotPrefix(InventoryBase __instance, ref object __result, int slotId,
        ref ItemStackMoveOperation op)
    {
        if (op.ShiftDown || !op.CtrlDown) return true;
        if (__instance is InventoryPlayerCreative) return true;

        var capi = __instance.Api as ICoreClientAPI;
        if (capi == null) return true;
        if (slotId < 0 || slotId >= __instance.Count) return true;

        var slot = __instance[slotId];
        if (slot?.Itemstack == null) return true;
        if (slot.Itemstack.Collectible.IsLiquid()) return true;

        var isBackpack = __instance.ClassName == GlobalConstants.backpackInvClassName;

        // Ctrl+click on bag-equipment slots can unequip a bag and rebuild backpack content
        // slots mid-loop, which crashes with "Supplied slot is not part of this inventory".
        if (isBackpack && slotId < BackpackBagSlotCount) return true;

        var collectible = slot.Itemstack.Collectible;
        __result = null;

        var startIndex = isBackpack ? BackpackBagSlotCount : 0;
        var endIndex = Math.Min(slotId, __instance.Count - 1);
        var toMove = new List<ItemSlot>();

        for (var i = startIndex; i <= endIndex; i++)
        {
            var candidate = __instance[i];
            if (candidate?.Itemstack == null) continue;
            if (candidate.Itemstack.Collectible != collectible) continue;
            toMove.Add(candidate);
        }

        foreach (var moveSlot in toMove)
        {
            if (!IsLiveSlot(__instance, moveSlot)) continue;
            if (moveSlot.Itemstack == null) continue;
            if (moveSlot.Itemstack.Collectible != collectible) continue;

            try
            {
                var slotOp = new ItemStackMoveOperation(op.World, op.MouseButton, EnumModifierKey.SHIFT,
                    EnumMergePriority.AutoMerge)
                {
                    ActingPlayer = op.ActingPlayer,
                    RequestedQuantity = moveSlot.Itemstack.StackSize
                };

                object[] packets = slotOp.ActingPlayer.InventoryManager.TryTransferAway(moveSlot, ref slotOp, false);
                if (packets == null) continue;
                foreach (var packet in packets) capi.Network.SendPacketClient(packet);
            }
            catch (ArgumentException)
            {
                // Inventory was rebuilt while transferring; abort the rest of the bulk move.
                break;
            }
        }

        return false;
    }

    private static bool IsLiveSlot(InventoryBase inventory, ItemSlot slot)
    {
        if (slot == null || slot.Inventory != inventory) return false;

        for (var i = 0; i < inventory.Count; i++)
        {
            if (ReferenceEquals(inventory[i], slot)) return true;
        }

        return false;
    }
}
