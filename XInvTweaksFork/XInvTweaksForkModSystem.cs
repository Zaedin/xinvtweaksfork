using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;
using XInvTweaksFork.Patches;
using XInvTweaksFork.Patches.Compatibility;

namespace XInvTweaksFork;

public class XInvTweaksForkModSystem : ModSystem
{
    private Harmony? _harmony;
    private ICoreAPI _api;
    private ICoreClientAPI _capi;
    private ChestSortDialog _chestSortDialog;
    internal SlotLockSelection? SlotLocks { get; private set; }
    internal SlotLockOverlay? LockOverlay { get; private set; }

    private const string ConfigPath = "xinvtweaksfork.json";
    
    public static InvTweaksConfig Config { get; private set; }

    public override double ExecuteOrder()
    {
        return 1.0;
    }

    public override void StartPre(ICoreAPI coreApi)
    {
        _api = coreApi;
        LoadConfig();
    }

    public override void StartServerSide(ICoreServerAPI coreServerApi)
    {
        coreServerApi.Event.SaveGameLoaded += OnWorldLoaded;
    }

    public override void StartClientSide(ICoreClientAPI coreClientApi)
    {
        _capi = coreClientApi;
        _api = coreClientApi;
        SlotLocks = new SlotLockSelection(Config.LockedSlots);
        LockOverlay = new SlotLockOverlay(_capi);

        PatchClient();
        RegisterKeys();

        if (!Config.ExtendChestUi) return;
        _chestSortDialog = new ChestSortDialog(_capi, SlotLocks);

        GuiDialog? invDialog = _capi.Gui.LoadedGuis.OfType<GuiDialogInventory>().FirstOrDefault();

        if (invDialog == null) return;
        invDialog.OnOpened += () => _chestSortDialog.OnInventoryOpened(invDialog.Composers["maininventory"].Bounds);
        invDialog.OnClosed += () => { _chestSortDialog.OnInventoryClosed(); };
    }

    public override void Dispose()
    {
        SlotLocks?.EndEditing();
        _chestSortDialog?.Dispose();
        LockOverlay?.Dispose();
        _harmony?.UnpatchAll("XInvTweakPatch");
        base.Dispose();
    }

    internal void SaveSlotLocks()
    {
        SaveConfig();
    }

    private void SaveConfig()
    {
        ModConfigPersistence.Store(_api, Config, ConfigPath, _api.ModLoader.IsModEnabled("configlib"));
    }

    private void LoadConfig()
    {
        try
        {
            Config = _api.LoadModConfig<InvTweaksConfig>(ConfigPath);
        }
        catch (Exception)
        {
            Config = new InvTweaksConfig();
        }

        Config ??= new InvTweaksConfig();
        Config.SortBlacklist.Add("cart");

        if (_api.Side == EnumAppSide.Client)
        {
            if (Config.SortOrder.Count == 0)
            {
                Config.SortOrder.Add("priority");
                Config.SortOrder.Add("lightinvert");
                Config.SortOrder.Add("tool");
                Config.SortOrder.Add("tooltier");
                Config.SortOrder.Add("block");
                Config.SortOrder.Add("storageflags");
                Config.SortOrder.Add("id");
                Config.SortOrder.Add("name");
                Config.SortOrder.Add("durability");
                Config.SortOrder.Add("attackpower");
                Config.SortOrder.Add("satiety");
                Config.SortOrder.Add("health");
                Config.SortOrder.Add("intoxication");
                Config.SortOrder.Add("stacksize");
                Config.SortOrder.Add("density");
                Config.SortOrder.Add("state");
            }

            if (Config.StackOrder.Count == 0)
            {
                Config.StackOrder.Add("stacksize");
                Config.StackOrder.Add("durability");
                Config.StackOrder.Add("transition");
            }

            InventoryUtil.SortOrder = Config.SortOrder;
            InventoryUtil.StackOrder = Config.StackOrder;
            InventoryUtil.Priorities = Config.Priorities;
            InventoryUtil.StorageFlagsOrder = Config.StorageFlagsOrder;
        }

        SaveConfig();
    }

    private void OnWorldLoaded()
    {
        var toAdd = new Dictionary<string, int>();
        var toRemove = new List<string>();

        foreach (var collectible in _api.World.Collectibles)
        {
            var storageProps = collectible.GetBehavior<CollectibleBehaviorGroundStorable>()?.StorageProps;
            if (storageProps == null) continue;
            var pair = Config.BulkQuanties.GetEnumerator();

            var found = false;
            while (pair.MoveNext() && !found)
                if (collectible.WildCardMatch(new AssetLocation(pair.Current.Key)))
                {
                    var maxTransfer = Math.Min(collectible.MaxStackSize, storageProps.StackingCapacity);
                    var value = Math.Max(Math.Min(pair.Current.Value, maxTransfer), storageProps.BulkTransferQuantity);
                    storageProps.BulkTransferQuantity = value;
                    found = true;

                    if (pair.Current.Value == value) continue;
                    if (toAdd.TryAdd(pair.Current.Key, value)) toRemove.Add(pair.Current.Key);
                }

            if (found) continue;

            if (collectible.MaxStackSize <= storageProps.BulkTransferQuantity) continue;
            if (storageProps.StackingCapacity <= storageProps.BulkTransferQuantity) continue;

            var firstCodePart = collectible.FirstCodePart();
            var dashCount = collectible.Code.Path.Count(x => x == '-');
            for (var ii = 0; ii < dashCount; ++ii) firstCodePart += "-*";

            toAdd.TryAdd(firstCodePart, storageProps.BulkTransferQuantity);
        }

        foreach (var key in toRemove) Config.BulkQuanties.Remove(key);

        if (toAdd.Count > 0)
        {
            var iterator = toAdd.GetEnumerator();
            while (iterator.MoveNext())
                if (!Config.BulkQuanties.ContainsKey(iterator.Current.Key))
                    Config.BulkQuanties.Add(iterator.Current.Key, iterator.Current.Value);
        }

        SaveConfig();
    }

    private void PatchClient()
    {
        _harmony = new Harmony("XInvTweakPatch");
        _harmony.PatchAll(Assembly.GetExecutingAssembly());

        if (Config.Tools)
            ManualPatch.PatchMethod(_harmony, typeof(CollectibleObject), typeof(CollectibleObjectPatch), "DamageItem");

        //if (Config.groundStorage) ManualPatch.PatchMethod(harmony, typeof(CollectibleBehaviorGroundStorable), typeof(CollectibleObjectPatch), "Interact");
        if (Config.GroundStorage)
            ManualPatch.PatchMethod(_harmony, typeof(BlockEntityGroundStorage), typeof(BlockEntityGroundStoragePatch),
                "OnPlayerInteractStart");

        if (Config.Blocks)
            ManualPatch.PatchMethod(_harmony, typeof(Block), typeof(CollectibleObjectPatch), "TryPlaceBlock");

        if (Config.Stairs)
            ManualPatch.PatchMethod(_harmony, typeof(BlockStairs), typeof(CollectibleObjectPatch), "TryPlaceBlock");

        //if (Config.piles) ManualPatch.PatchMethod(harmony, typeof(BlockEntityItemPile), typeof(CollectibleObjectPatch), "OnPlayerInteract");
        if (Config.Piles)
            ManualPatch.PatchMethod(_harmony, typeof(BlockEntityItemPile), typeof(BlockEntityItemPilePatch),
                "OnPlayerInteract");

        if (Config.ExtendChestUi)
            ManualPatch.PatchMethod(_harmony, typeof(GuiDialogBlockEntityInventory),
                typeof(GuiDialogBlockEntityInventoryPatch), "OnGuiOpened");

        if (Config.ExtendChestUi)
            ManualPatch.PatchMethod(_harmony, typeof(GuiDialogBlockEntityInventory),
                typeof(GuiDialogBlockEntityInventoryPatch), "OnGuiClosed");

        PatchSlotLocks(_harmony);

        if (Config.CrateSwitch)
            ManualPatch.PatchMethod(_harmony, typeof(BlockEntityCrate), typeof(BlockEntityCratePatch),
                "OnBlockInteractStart");

        if (Config.Seeds)
        {
            ManualPatch.PatchMethod(_harmony, typeof(ItemPlantableSeed), typeof(CollectibleObjectPatch),
                "OnHeldInteractStart");

            var assembly = _capi.ModLoader.GetModSystem("XSkills.XSkills")?.GetType().Assembly;
            var type = assembly?.GetType("XSkills.XSkillsItemPlantableSeed");
            if (type != null)
                ManualPatch.PatchMethod(_harmony, type, typeof(CollectibleObjectPatch), "OnHeldInteractStart");
        }

        if (Config.StrgClick)
            ManualPatch.PatchMethod(_harmony, typeof(InventoryBase), typeof(InventoryBasePatch), "ActivateSlot");

        //if (Config.survivalPick) ManualPatch.PatchMethod(harmony, typeof(SystemMouseInWorldInteractions), typeof(SystemMouseInWorldInteractionsPatch), "HandleMouseInteractionsBlockSelected");
        
        foreach (var system in _capi.ModLoader.Systems)
        {
            var type = system.GetType();
            _api.Logger.Notification(type.FullName);
        }
        var modSystem = _capi.ModLoader.GetModSystem("PlayerInventoryLib.PlayerInventoryLibSystem");
        if (modSystem != null)
        {
            PatchPlayerInventoryLib(modSystem);
        }

        _capi.Event.LevelFinalize += OnWorldLoaded;
    }

    // Slot locks are independent of the optional sorting dialog and wheel-transfer feature.
    internal static void PatchSlotLocks(Harmony harmony)
    {
        foreach (var method in new[]
                 {
                     "OnMouseDownOnElement", "SlotClick", "RenderInteractiveElements", "OnMouseWheel", "OnGuiClosed"
                 })
            ManualPatch.PatchMethod(harmony, typeof(GuiElementItemSlotGridBase),
                typeof(GuiElementItemSlotGridBasePatch), method);
    }
    
    private void PatchPlayerInventoryLib(ModSystem modSystem)
    {
        if (_harmony is null) return;
        var assembly = modSystem.GetType().Assembly;

        var dialogType = assembly.GetType("PlayerInventoryLib.GUI.GuiDialogSurvivalInventory");
        if (dialogType == null)
        {
            _api.Logger.Warning("[XInvTweaksFork] PlayerInventoryLib detected, but GuiDialogSurvivalInventory was not found.");
            return;
        }

        ManualPatch.PatchMethod(_harmony, dialogType, typeof(GuiDialogPlayerInventoryLibPatch), "OnGuiOpened");
        ManualPatch.PatchMethod(_harmony, dialogType, typeof(GuiDialogPlayerInventoryLibPatch), "OnGuiClosed");

        _api.Logger.Notification("[XInvTweaksFork] Patched PlayerInventoryLib.");
    }

    private void RegisterKeys()
    {
        _capi.Input.RegisterHotKey("pushinventory", Lang.Get("xinvtweaksfork:pushinventory"), GlKeys.Z,
            HotkeyType.InventoryHotkeys);
        _capi.Input.RegisterHotKey("sortinventories", Lang.Get("xinvtweaksfork:sortinventories"), GlKeys.Z,
            HotkeyType.InventoryHotkeys, false, false, true);
        _capi.Input.RegisterHotKey("pullinventory", Lang.Get("xinvtweaksfork:pullinventory"), GlKeys.Z,
            HotkeyType.InventoryHotkeys, false, true);
        _capi.Input.RegisterHotKey("sortbackpack", Lang.Get("xinvtweaksfork:sortbackpack"), GlKeys.Z,
            HotkeyType.InventoryHotkeys, true);
        _capi.Input.RegisterHotKey("fillbackpack", Lang.Get("xinvtweaksfork:fillbackpack"), GlKeys.Z,
            HotkeyType.InventoryHotkeys, true, true);
        _capi.Input.RegisterHotKey("clearhandslot", Lang.Get("xinvtweaksfork:clearhandslot"), GlKeys.V,
            HotkeyType.InventoryHotkeys);

        _capi.Input.SetHotKeyHandler("pushinventory", _ => InventoryUtil.PushInventory(_capi));
        _capi.Input.SetHotKeyHandler("sortinventories", _ => InventoryUtil.SortInventories(_capi));
        _capi.Input.SetHotKeyHandler("pullinventory", _ => InventoryUtil.PullInventory(_capi));
        _capi.Input.SetHotKeyHandler("sortbackpack", _ => InventoryUtil.SortBackpack(_capi));
        _capi.Input.SetHotKeyHandler("fillbackpack", _ => InventoryUtil.FillBackpack(_capi));
        _capi.Input.SetHotKeyHandler("clearhandslot", _ => InventoryUtil.ClearHandSlot(_capi));
        SlotLockHotkey.Register(_capi.Input, SlotLocks!, Lang.Get("xinvtweaksfork:togglelockselection"),
            editing => _capi.ShowChatMessage(Lang.Get(editing
                ? "xinvtweaksfork:lockselection-enabled"
                : "xinvtweaksfork:lockselection-disabled")));
    }

    public void OnInventoryOpened(ElementBounds parent)
    {
        _chestSortDialog?.OnInventoryOpened(parent);
    }

    public void OnInventoryClosed()
    {
        _chestSortDialog?.OnInventoryClosed();
    }
}
