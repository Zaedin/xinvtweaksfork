using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace XInvTweaksFork.Tests;

public class ConfigLibSupportTests
{
    private static JObject Definition() => JObject.Parse(File.ReadAllText(Path.Combine(
        AppContext.BaseDirectory, "assets", "xinvtweaksfork", "config", "configlib-patches.json")));

    [Fact]
    public void DefinitionExposesEveryScalarWithMatchingTypeAndDefault()
    {
        var definition = Definition();
        Assert.Equal("xinvtweaksfork.json", definition.Value<string>("file"));
        Assert.Empty((JObject)definition["patches"]!);
        var settings = definition["settings"]!.Children<JObject>()
            .Where(s => s.Value<string>("type") != "separator").ToArray();
        var fields = typeof(InvTweaksConfig).GetFields()
            .Where(f => f.FieldType == typeof(bool) || f.FieldType == typeof(int)).ToArray();
        Assert.Equal(fields.Length, settings.Length);
        Assert.Equal(settings.Length, settings.Select(s => s.Value<string>("code")).Distinct().Count());
        var defaults = new InvTweaksConfig();
        foreach (var field in fields)
        {
            var setting = Assert.Single(settings, s => s.Value<string>("code") == field.Name);
            Assert.Equal(field.FieldType == typeof(bool) ? "boolean" : "integer", setting.Value<string>("type"));
            Assert.Equal(field.GetValue(defaults), setting["default"]!.ToObject(field.FieldType));
            Assert.True(setting.Value<bool>("clientSide"));
            if (field.FieldType == typeof(int)) Assert.Equal(0, setting["range"]!.Value<int>("min"));
        }
    }

    [Fact]
    public void DefinitionLabelsAndHintsHaveEnglishAndSpanishTranslations()
    {
        foreach (var locale in new[] { "en", "es-es" })
        {
            var lang = JObject.Parse(File.ReadAllText(Path.Combine(
                AppContext.BaseDirectory, "assets", "xinvtweaksfork", "lang", locale + ".json")));
            foreach (var setting in Definition()["settings"]!.Children<JObject>())
                foreach (var property in new[] { "ingui", "comment", "title", "text" })
                {
                    var key = setting.Value<string>(property);
                    if (key == null) continue;
                    Assert.StartsWith("xinvtweaksfork:", key);
                    Assert.False(string.IsNullOrWhiteSpace(lang.Value<string>(key.Split(':')[1])));
                }
        }
    }

    [Fact]
    public void SavingSlotLocksPreservesPendingMenuSettingsWithoutApplyingThemLive()
    {
        var current = new InvTweaksConfig();
        current.LockedSlots.Add(19);
        current.SortOrder.Add("name");
        var saved = JObject.Parse("""{"ExtendChestUi":false,"Delay":321,"LockedSlots":[0]}""");
        var merged = ModConfigPersistence.MergeSavedScalars(current, saved);
        Assert.False(merged.Value<bool>("ExtendChestUi"));
        Assert.Equal(321, merged.Value<int>("Delay"));
        Assert.Equal(new[] { 19 }, merged["LockedSlots"]!.ToObject<int[]>());
        Assert.Equal(new[] { "name" }, merged["SortOrder"]!.ToObject<string[]>());
        Assert.True(current.ExtendChestUi);
        Assert.Equal(200, current.Delay);
        Assert.Equal(new[] { 0 }, saved["LockedSlots"]!.ToObject<int[]>());
    }

    [Fact]
    public void MissingOrWrongTypeDiskValuesDoNotReplaceValidRuntimeDefaults()
    {
        var current = new InvTweaksConfig();
        var merged = ModConfigPersistence.MergeSavedScalars(current,
            JObject.Parse("""{"ExtendChestUi":"false","Delay":null,"NotASetting":false}"""));
        Assert.True(merged.Value<bool>("ExtendChestUi"));
        Assert.Equal(200, merged.Value<int>("Delay"));
        Assert.Null(merged["NotASetting"]);
    }
}
