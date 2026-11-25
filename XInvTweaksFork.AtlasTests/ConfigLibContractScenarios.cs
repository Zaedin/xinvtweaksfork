using System.Linq;
using System.Threading.Tasks;
using Atlas.XUnit;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Xunit;

namespace XInvTweaksFork.AtlasTests;

public class ConfigLibContractScenarios : AtlasScenarioBase
{
    [AtlasScenario]
    public async Task OptionalMenuDefinitionIsStagedAndMapsToTheRealModConfig()
    {
        Assert.NotNull(World.Api.ModLoader.GetModSystem<XInvTweaksForkModSystem>());
        var definition = World.Api.Assets.Get(new AssetLocation(
            "xinvtweaksfork:config/configlib-patches.json")).ToObject<JObject>();
        Assert.Equal("xinvtweaksfork.json", definition.Value<string>("file"));
        var fields = JObject.FromObject(XInvTweaksForkModSystem.Config);
        foreach (var setting in definition["settings"]!.Children<JObject>()
                     .Where(s => s.Value<string>("type") != "separator"))
        {
            Assert.NotNull(fields[setting.Value<string>("code")!]);
            Assert.True(setting.Value<bool>("clientSide"));
        }
        // The optional-dependency path must also boot without ConfigLib installed.
        Assert.False(World.Api.ModLoader.IsModEnabled("configlib"));
        await World.Ticks(1);
    }

    [AtlasScenario(FreshWorld = true)]
    public async Task RestartPendingMenuEditsSurviveARealConfigWriteForSlotLocks()
    {
        var current = XInvTweaksForkModSystem.Config;
        var file = World.Api.LoadModConfig<JObject>("xinvtweaksfork.json")!;
        file["ExtendChestUi"] = false;
        file["Delay"] = 321;
        World.Api.StoreModConfig(file, "xinvtweaksfork.json");
        current.LockedSlots.Add(19);

        // Exercise the production ConfigLib-aware write path against the embedded server's disk.
        // This tests the persistence contract, not ConfigLib's client UI or its own save implementation.
        ModConfigPersistence.Store(World.Api, current, "xinvtweaksfork.json", preserveSavedScalars: true);
        await World.Ticks(1);
        var persisted = World.Api.LoadModConfig<JObject>("xinvtweaksfork.json")!;
        Assert.False(persisted.Value<bool>("ExtendChestUi"));
        Assert.Equal(321, persisted.Value<int>("Delay"));
        Assert.Contains(19, persisted["LockedSlots"]!.ToObject<int[]>()!);
        Assert.True(current.ExtendChestUi);
        Assert.Equal(200, current.Delay);
        var reloaded = World.Api.LoadModConfig<InvTweaksConfig>("xinvtweaksfork.json")!;
        Assert.False(reloaded.ExtendChestUi);
        Assert.Equal(321, reloaded.Delay);
    }
}
