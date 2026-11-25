using System.Threading.Tasks;
using Atlas.XUnit;
using Newtonsoft.Json.Linq;
using Xunit;

namespace XInvTweaksFork.AtlasTests;

[AtlasDataFiles("fixtures/PriorityDisabled", TargetPath = "ModConfig")]
public class PriorityConfigScenarios : AtlasScenarioBase
{
    [AtlasScenario]
    public async Task DisabledPrioritySurvivesStartupWithoutDeletingItsRules()
    {
        Assert.NotNull(World.Api.ModLoader.GetModSystem<XInvTweaksForkModSystem>());
        await World.Ticks(1);
        Assert.False(XInvTweaksForkModSystem.Config.EnablePriority);
        Assert.True(XInvTweaksForkModSystem.Config.ExtendChestUi);
        Assert.Equal(10, XInvTweaksForkModSystem.Config.Priorities["target"]);
        Assert.Equal(new[] { "priority", "id" }, XInvTweaksForkModSystem.Config.SortOrder);

        var persisted = World.Api.LoadModConfig<JObject>("xinvtweaksfork.json");
        Assert.NotNull(persisted);
        Assert.False(persisted.Value<bool>("EnablePriority"));
        Assert.Equal(10, persisted["Priorities"]!["target"]!.Value<int>());
        Assert.False(World.Api.LoadModConfig<InvTweaksConfig>("xinvtweaksfork.json")!.EnablePriority);
    }
}

[AtlasDataFiles("fixtures/ModConfig", TargetPath = "ModConfig")]
public class PriorityDefaultConfigScenarios : AtlasScenarioBase
{
    [AtlasScenario]
    public async Task ExistingConfigWithoutTheNewOptionKeepsPriorityDisabled()
    {
        Assert.NotNull(World.Api.ModLoader.GetModSystem<XInvTweaksForkModSystem>());
        await World.Ticks(1);
        Assert.False(XInvTweaksForkModSystem.Config.EnablePriority);
        var persisted = World.Api.LoadModConfig<JObject>("xinvtweaksfork.json");
        Assert.NotNull(persisted);
        Assert.False(persisted.Value<bool>("EnablePriority"));
    }
}
