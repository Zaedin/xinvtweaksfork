using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace XInvTweaksFork.Tests;

public class ConfigScalarPersistenceTests
{
    [Theory]
    [InlineData("Blocks")]
    [InlineData("CrateSwitch")]
    [InlineData("ExtendChestUi")]
    [InlineData("GroundStorage")]
    [InlineData("Piles")]
    [InlineData("PushPullWheel")]
    [InlineData("Seeds")]
    [InlineData("Stairs")]
    [InlineData("StrgClick")]
    [InlineData("Tools")]
    public void DisabledSettingSurvivesLoadingAndSaving(string name)
    {
        var config = JsonConvert.DeserializeObject<InvTweaksConfig>($$"""{"{{name}}":false}""");
        Assert.NotNull(config);
        Assert.Equal(false, typeof(InvTweaksConfig).GetField(name)!.GetValue(config));
        var saved = JObject.Parse(JsonConvert.SerializeObject(config));
        Assert.False(saved.Value<bool>(name));
        var reloaded = JsonConvert.DeserializeObject<InvTweaksConfig>(saved.ToString());
        Assert.NotNull(reloaded);
        Assert.Equal(false, typeof(InvTweaksConfig).GetField(name)!.GetValue(reloaded));
    }

    [Theory]
    [InlineData("Delay", 321)]
    [InlineData("ToolSwitchDurability", 17)]
    public void NumericSettingSurvivesLoadingAndSaving(string name, int value)
    {
        var config = JsonConvert.DeserializeObject<InvTweaksConfig>($$"""{"{{name}}":{{value}}}""");
        Assert.NotNull(config);
        Assert.Equal(value, typeof(InvTweaksConfig).GetField(name)!.GetValue(config));
        Assert.Equal(value, JObject.Parse(JsonConvert.SerializeObject(config)).Value<int>(name));
    }

    [Fact]
    public void MissingSettingsKeepTheirExistingDefaults()
    {
        var config = JsonConvert.DeserializeObject<InvTweaksConfig>("{}");
        Assert.NotNull(config);
        Assert.True(config.ExtendChestUi);
        Assert.True(config.PushPullWheel);
        Assert.Equal(200, config.Delay);
        Assert.Equal(0, config.ToolSwitchDurability);
    }

    [Fact]
    public void DisposingWithoutAClientDialogIsSafe()
    {
        // Server-side instances and clients with ExtendChestUi=false never create a dialog.
        new XInvTweaksForkModSystem().Dispose();
    }
}
