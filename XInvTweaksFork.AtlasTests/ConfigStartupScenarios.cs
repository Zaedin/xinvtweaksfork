using System.Threading.Tasks;
using Atlas.XUnit;
using Newtonsoft.Json.Linq;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace XInvTweaksFork.AtlasTests;

[AtlasDataFiles("fixtures/ModConfig", TargetPath = "ModConfig")]
public class ConfigStartupScenarios : AtlasScenarioBase
{
    [AtlasScenario]
    public async Task DisabledUiSurvivesRealModStartupAndItsConfigWrites()
    {
        Assert.NotNull(World.Api.ModLoader.GetModSystem<XInvTweaksForkModSystem>());
        await World.Ticks(1);

        // The staged mod has executed StartPre and SaveGameLoaded, both of which write config.
        Assert.False(XInvTweaksForkModSystem.Config.ExtendChestUi);
        Assert.False(XInvTweaksForkModSystem.Config.PushPullWheel);
        Assert.Equal(321, XInvTweaksForkModSystem.Config.Delay);

        // Inspect JSON independently of the config deserializer to catch disk overwrites too.
        var persisted = World.Api.LoadModConfig<JObject>("xinvtweaksfork.json");
        Assert.NotNull(persisted);
        Assert.False(persisted.Value<bool>("ExtendChestUi"));
        Assert.False(persisted.Value<bool>("PushPullWheel"));
        Assert.Equal(321, persisted.Value<int>("Delay"));
        Assert.Equal(new[] { 0, 19 }, persisted["LockedSlots"]!.ToObject<int[]>());

        var reloaded = World.Api.LoadModConfig<InvTweaksConfig>("xinvtweaksfork.json");
        Assert.NotNull(reloaded);
        Assert.False(reloaded.ExtendChestUi);
        Assert.False(reloaded.PushPullWheel);
        Assert.Equal(321, reloaded.Delay);
    }
}
