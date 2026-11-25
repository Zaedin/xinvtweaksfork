using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Xunit;

namespace XInvTweaksFork.Tests;

[CollectionDefinition("Priority sorting", DisableParallelization = true)]
public class PrioritySortingCollection;

[Collection("Priority sorting")]
public class PriorityFeatureTests : IDisposable
{
    private readonly Dictionary<FieldInfo, object?> saved = new();

    public PriorityFeatureTests()
    {
        Set("Priority", "");
        Set("SortOrder", new List<string> { "priority", "id" });
        Set("Priorities", new Dictionary<string, int>());
    }

    private void Set(string name, object? value)
    {
        var field = typeof(InventoryUtil).GetField(name, BindingFlags.Static | BindingFlags.NonPublic)!;
        if (!saved.ContainsKey(field)) saved.Add(field, field.GetValue(null));
        field.SetValue(null, value);
    }

    private static IComparer<CollectibleObject> Comparer(bool enabled)
    {
        var type = typeof(InventoryUtil).GetNestedType("CollectibleComparer", BindingFlags.NonPublic)!;
        return (IComparer<CollectibleObject>)Activator.CreateInstance(type, new object[] { enabled })!;
    }

    private class NamedItem : Item
    {
        public override string GetHeldItemName(ItemStack stack) => Code.Path;
    }

    private static Item MakeItem(int id, string code, int priority = 0) => new NamedItem
    {
        ItemId = id,
        Code = new AssetLocation("game", code),
        Attributes = new JsonObject(new JObject { ["sortpriority"] = priority })
    };

    [Theory]
    [InlineData(true, -1)]
    [InlineData(false, 1)]
    public void TextPriorityIsOnlyAppliedWhenEnabled(bool enabled, int expectedSign)
    {
        Set("Priority", "target");
        var result = Comparer(enabled).Compare(MakeItem(2, "target"), MakeItem(1, "other"));
        Assert.Equal(expectedSign, Math.Sign(result));
    }

    [Theory]
    [InlineData(true, -1)]
    [InlineData(false, 1)]
    public void ConfiguredPriorityIncludingCachedValuesIsOnlyAppliedWhenEnabled(bool enabled, int expectedSign)
    {
        Set("Priorities", new Dictionary<string, int> { ["target"] = 10 });
        var result = Comparer(enabled).Compare(MakeItem(2, "target", 10), MakeItem(1, "other"));
        Assert.Equal(expectedSign, Math.Sign(result));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void EmptyOrClearedTextPriorityFallsBackToNormalSorting(string? text)
    {
        Set("Priority", text);
        Assert.True(Comparer(true).Compare(MakeItem(1, "first"), MakeItem(2, "second")) < 0);
    }

    [Fact]
    public void DisablingPriorityPreservesOtherConfiguredSortRules()
    {
        Set("Priority", "first");
        Set("SortOrder", new List<string> { "priority", "idinvert" });
        Assert.True(Comparer(false).Compare(MakeItem(1, "first"), MakeItem(2, "second")) > 0);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConfigToggleRoundTripsWithoutDeletingUserRules(bool enabled)
    {
        var config = JsonConvert.DeserializeObject<InvTweaksConfig>(
            $$"""{"EnablePriority":{{enabled.ToString().ToLowerInvariant()}},"Priorities":{"target":10},"SortOrder":["priority","id"]}""");
        Assert.NotNull(config);
        Assert.Equal(enabled, config.EnablePriority);
        var reloaded = JsonConvert.DeserializeObject<InvTweaksConfig>(JsonConvert.SerializeObject(config));
        Assert.NotNull(reloaded);
        Assert.Equal(enabled, reloaded.EnablePriority);
        Assert.Equal(10, reloaded.Priorities["target"]);
        Assert.Equal(new[] { "priority", "id" }, reloaded.SortOrder);
    }

    [Fact]
    public void ExistingConfigsKeepPriorityDisabledByDefault()
    {
        Assert.False(JsonConvert.DeserializeObject<InvTweaksConfig>("{}")!.EnablePriority);
    }

    public void Dispose()
    {
        foreach (var pair in saved) pair.Key.SetValue(null, pair.Value);
    }
}
