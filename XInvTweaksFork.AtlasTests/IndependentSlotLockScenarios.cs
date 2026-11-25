using System.Threading.Tasks;
using Atlas.XUnit;
using Xunit;

namespace XInvTweaksFork.AtlasTests;

[AtlasDataFiles("fixtures/IndependentSlotLocks", TargetPath = "ModConfig")]
public class IndependentSlotLockScenarios : AtlasScenarioBase
{
    [AtlasScenario]
    public async Task EditingAndPersistingLocksDoesNotRequireExtendedUiOrWheelTransfers()
    {
        Assert.NotNull(World.Api.ModLoader.GetModSystem<XInvTweaksForkModSystem>());
        var config = XInvTweaksForkModSystem.Config;
        Assert.False(config.ExtendChestUi);
        Assert.False(config.PushPullWheel);
        var selection = new SlotLockSelection(config.LockedSlots);
        selection.ToggleEditing();
        Assert.True(selection.TryToggle(4, 24)); // Unlock first slot.
        Assert.True(selection.TryToggle(5, 24)); // Lock second slot.
        selection.EndEditing();

        ModConfigPersistence.Store(World.Api, config, "xinvtweaksfork.json", preserveSavedScalars: false);
        await World.Ticks(1);
        var saved = World.Api.LoadModConfig<InvTweaksConfig>("xinvtweaksfork.json")!;
        Assert.False(saved.ExtendChestUi);
        Assert.False(saved.PushPullWheel);
        Assert.Equal(new[] { 1, 19 }, saved.LockedSlots);
        Assert.False(selection.IsEditing);
        Assert.True(SlotLockSelection.IsLocked(saved.LockedSlots, 23, 24));
        Assert.False(SlotLockSelection.IsLocked(saved.LockedSlots, 23, 19));
        Assert.True(SlotLockSelection.IsLocked(saved.LockedSlots, 23, 24));
    }
}
