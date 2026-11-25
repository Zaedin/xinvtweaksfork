using System.Collections.Generic;

namespace XInvTweaksFork;

/// <summary>
/// Stores zero-based backpack storage indices, not the four equipped-bag slots.
/// Unavailable indices stay saved but are never treated as active slots.
/// </summary>
public sealed class SlotLockSelection
{
    public const int BackpackFirstSlot = 4;
    private readonly SortedSet<int> lockedSlots;

    public bool IsEditing { get; private set; }

    public SlotLockSelection(SortedSet<int> lockedSlots)
    {
        this.lockedSlots = lockedSlots;
    }

    public void ToggleEditing() => IsEditing = !IsEditing;

    public void EndEditing() => IsEditing = false;

    public static bool IsStorageSlot(int slotId, int inventoryCount, int first = BackpackFirstSlot)
    {
        return first >= 0 && slotId >= first && slotId < inventoryCount;
    }

    public static bool IsLocked(SortedSet<int> locks, int slotId, int inventoryCount,
        int first = BackpackFirstSlot)
    {
        return IsStorageSlot(slotId, inventoryCount, first) &&
               (locks.Contains(slotId - first) || locks.Contains(slotId - inventoryCount));
    }

    public bool TryToggle(int slotId, int inventoryCount)
    {
        if (!IsEditing || !IsStorageSlot(slotId, inventoryCount)) return false;

        if (IsLocked(lockedSlots, slotId, inventoryCount))
        {
            // Existing configs may address a slot positively, negatively, or both.
            lockedSlots.Remove(slotId - BackpackFirstSlot);
            lockedSlots.Remove(slotId - inventoryCount);
        }
        else
        {
            lockedSlots.Add(slotId - BackpackFirstSlot);
        }

        return true;
    }
}
