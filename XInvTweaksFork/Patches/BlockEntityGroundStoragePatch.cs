using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace XInvTweaksFork.Patches;

internal class BlockEntityGroundStoragePatch
{
    public static void OnPlayerInteractStartPrefix(BlockEntityGroundStorage __instance, IPlayer player)
    {
        if (__instance.Api.Side == EnumAppSide.Server) return;

        if (player.InventoryManager.ActiveHotbarSlot.Itemstack?.Item is ItemDryGrass &&
            __instance.Inventory[0]?.Itemstack?.Item?.CombustibleProps?.SmeltingType == EnumSmeltType.Fire) return;
        if (player.InventoryManager.ActiveHotbarSlot.Itemstack?.Collectible
                .HasBehavior<CollectibleBehaviorGroundStorable>() ?? false) return;
        InventoryUtil.FindPushableCollectible(__instance.Inventory, player);
    }
}