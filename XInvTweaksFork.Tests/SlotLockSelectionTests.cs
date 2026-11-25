using System.Collections.Generic;
using Newtonsoft.Json;
using Xunit;

namespace XInvTweaksFork.Tests;

public class SlotLockSelectionTests
{
    [Fact]
    public void SelectionIsOptInAndCanBeEndedWithoutDiscardingLocks()
    {
        var locks = new SortedSet<int>();
        var selection = new SlotLockSelection(locks);
        Assert.False(selection.IsEditing);
        Assert.False(selection.TryToggle(4, 24));
        Assert.Empty(locks);

        selection.ToggleEditing();
        Assert.True(selection.IsEditing);
        Assert.True(selection.TryToggle(4, 24));
        Assert.Equal(new[] { 0 }, locks);
        selection.EndEditing();
        selection.EndEditing();
        Assert.False(selection.IsEditing);
        Assert.False(selection.TryToggle(4, 24));
        Assert.Equal(new[] { 0 }, locks);
    }

    [Fact]
    public void ClickingStorageSlotTwiceLocksThenUnlocksIt()
    {
        var locks = new SortedSet<int> { 2 };
        var selection = new SlotLockSelection(locks);
        selection.ToggleEditing();

        Assert.True(selection.TryToggle(23, 24));
        Assert.Equal(new[] { 2, 19 }, locks);
        Assert.True(SlotLockSelection.IsLocked(locks, 23, 24));
        Assert.True(selection.TryToggle(23, 24));
        Assert.Equal(new[] { 2 }, locks);
        Assert.False(SlotLockSelection.IsLocked(locks, 23, 24));
    }

    [Theory]
    [InlineData(-1, 24)]
    [InlineData(0, 24)]
    [InlineData(3, 24)]
    [InlineData(24, 24)]
    [InlineData(25, 24)]
    [InlineData(4, 4)]
    [InlineData(4, 0)]
    [InlineData(int.MaxValue, 24)]
    [InlineData(int.MinValue, 24)]
    public void InvalidAndEquippedBagSlotsCannotBeLocked(int slotId, int count)
    {
        var locks = new SortedSet<int> { 0, 19 };
        var selection = new SlotLockSelection(locks);
        selection.ToggleEditing();
        Assert.False(selection.TryToggle(slotId, count));
        Assert.False(SlotLockSelection.IsLocked(locks, slotId, count));
        Assert.Equal(new[] { 0, 19 }, locks);
    }

    [Fact]
    public void TwentiethStorageSlotIsSuspendedAtFifteenAndRestoredWithCapacity()
    {
        var locks = new SortedSet<int>();
        var selection = new SlotLockSelection(locks);
        selection.ToggleEditing();
        Assert.True(selection.TryToggle(23, 24));

        // Four equipped-bag slots plus fifteen storage slots.
        Assert.False(SlotLockSelection.IsLocked(locks, 23, 19));
        Assert.False(selection.TryToggle(23, 19));
        for (var slotId = 0; slotId < 19; slotId++)
            Assert.False(SlotLockSelection.IsLocked(locks, slotId, 19));
        Assert.Equal(new[] { 19 }, locks);

        // Restoring capacity reactivates the same storage index, not the last available slot.
        Assert.True(SlotLockSelection.IsLocked(locks, 23, 24));
        Assert.True(SlotLockSelection.IsLocked(locks, 23, 32));
        Assert.False(SlotLockSelection.IsLocked(locks, 22, 32));
        Assert.False(SlotLockSelection.IsLocked(locks, 31, 32));
    }

    [Fact]
    public void RemovingAllBackpacksSuspendsEveryLockWithoutDeletingPreferences()
    {
        var locks = new SortedSet<int> { 0, 4, 19, -1 };
        for (var slotId = 0; slotId < 32; slotId++)
            Assert.False(SlotLockSelection.IsLocked(locks, slotId, 4));
        Assert.Equal(new[] { -1, 0, 4, 19 }, locks);
        Assert.True(SlotLockSelection.IsLocked(locks, 4, 24));
        Assert.True(SlotLockSelection.IsLocked(locks, 8, 24));
        Assert.True(SlotLockSelection.IsLocked(locks, 23, 24));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ClickingLegacyReverseLockUnlocksBothAliases(bool hasPositiveAlias)
    {
        var locks = new SortedSet<int> { -1, 5 };
        if (hasPositiveAlias) locks.Add(19);
        var selection = new SlotLockSelection(locks);
        selection.ToggleEditing();
        Assert.True(SlotLockSelection.IsLocked(locks, 23, 24));
        Assert.True(selection.TryToggle(23, 24));
        Assert.False(SlotLockSelection.IsLocked(locks, 23, 24));
        Assert.Equal(new[] { 5 }, locks);
    }

    [Fact]
    public void LegacyReverseIndicesRemainRelativeToTheCurrentInventoryEnd()
    {
        var locks = new SortedSet<int> { -1, -2, -100 };
        Assert.True(SlotLockSelection.IsLocked(locks, 23, 24));
        Assert.True(SlotLockSelection.IsLocked(locks, 22, 24));
        Assert.True(SlotLockSelection.IsLocked(locks, 18, 19));
        Assert.True(SlotLockSelection.IsLocked(locks, 17, 19));
        Assert.False(SlotLockSelection.IsLocked(locks, 16, 19));
    }

    [Fact]
    public void TogglingEditingOffPreventsFurtherChanges()
    {
        var locks = new SortedSet<int>();
        var selection = new SlotLockSelection(locks);
        selection.ToggleEditing();
        Assert.True(selection.TryToggle(4, 24));
        selection.ToggleEditing();
        Assert.False(selection.IsEditing);
        Assert.False(selection.TryToggle(5, 24));
        Assert.Equal(new[] { 0 }, locks);
    }

    [Fact]
    public void ConfigRoundTripRetainsUnavailableLocksAndUnrelatedSettings()
    {
        var config = new InvTweaksConfig();
        config.SortOrder.Add("name");
        config.Priorities.Add("stone-*", 7);
        config.LockedSlots.Add(-2);
        var selection = new SlotLockSelection(config.LockedSlots);
        selection.ToggleEditing();
        Assert.True(selection.TryToggle(23, 24));

        var json = JsonConvert.SerializeObject(config);
        Assert.Contains("\"LockedSlots\":[-2,19]", json);
        var restored = JsonConvert.DeserializeObject<InvTweaksConfig>(json);
        Assert.NotNull(restored);
        Assert.Equal(new[] { -2, 19 }, restored.LockedSlots);
        Assert.Equal(new[] { "name" }, restored.SortOrder);
        Assert.Equal(7, restored.Priorities["stone-*"]);
        Assert.False(SlotLockSelection.IsLocked(restored.LockedSlots, 23, 19));
        Assert.True(SlotLockSelection.IsLocked(restored.LockedSlots, 23, 24));

        var nextSession = new SlotLockSelection(restored.LockedSlots);
        Assert.False(nextSession.IsEditing);
    }

    [Fact]
    public void LoadingLegacyConfigStillSupportsBothIndexStyles()
    {
        var config = JsonConvert.DeserializeObject<InvTweaksConfig>(
            """{"LockedSlots":[0,19,-1],"SortOrder":["name"]}""");
        Assert.NotNull(config);
        Assert.True(SlotLockSelection.IsLocked(config.LockedSlots, 4, 24));
        Assert.True(SlotLockSelection.IsLocked(config.LockedSlots, 23, 24));
        Assert.False(SlotLockSelection.IsLocked(config.LockedSlots, 5, 24));
    }

    [Fact]
    public void AvailabilityUsesAnExclusiveUpperBoundAndHonorsTheSortStartOffset()
    {
        var locks = new SortedSet<int> { 0, 19 };
        Assert.True(SlotLockSelection.IsLocked(locks, 4, 24, 4));
        Assert.True(SlotLockSelection.IsLocked(locks, 23, 24, 4));
        Assert.False(SlotLockSelection.IsLocked(locks, 24, 24, 4));
        Assert.True(SlotLockSelection.IsLocked(locks, 0, 20, 0));
        Assert.True(SlotLockSelection.IsLocked(locks, 19, 20, 0));
        Assert.False(SlotLockSelection.IsLocked(locks, 20, 20, 0));
    }
}
