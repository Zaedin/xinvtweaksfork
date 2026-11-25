using System.Collections.Generic;
using Vintagestory.API.Common;

namespace XInvTweaksFork;

public class InvTweaksConfig
{
    // Scalar fields must be writable so JSON config loading can override their defaults.
    public bool Blocks = true;

    public readonly Dictionary<string, int> BulkQuanties = new();
    public bool CrateSwitch = true;
    public int Delay = 200;
    public bool ExtendChestUi = true;
    public bool GroundStorage = true;
    public readonly SortedSet<int> LockedSlots = [];
    public bool Piles = true;

    public readonly Dictionary<string, int> Priorities = new();

    public bool PushPullWheel = true;
    public bool Seeds = true;
    public readonly SortedSet<string> SortBlacklist = [];

    public readonly List<string> SortOrder = [];
    public readonly List<string> StackOrder = [];
    public bool Stairs = true;
    public readonly List<EnumItemStorageFlags> StorageFlagsOrder = [];
    public bool StrgClick = true;
    public bool Tools = true;
    public int ToolSwitchDurability = 0;
}
