using System;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;

namespace XInvTweaksFork;

/// <summary>Preserves restart-pending ConfigLib edits when this mod saves slot locks or world defaults.</summary>
public static class ModConfigPersistence
{
    public static JObject MergeSavedScalars(InvTweaksConfig current, JObject saved)
    {
        var result = JObject.FromObject(current);
        foreach (var field in typeof(InvTweaksConfig).GetFields())
        {
            if (field.FieldType != typeof(bool) && field.FieldType != typeof(int)) continue;
            var value = saved[field.Name];
            var expected = field.FieldType == typeof(bool) ? JTokenType.Boolean : JTokenType.Integer;
            if (value?.Type == expected) result[field.Name] = value.DeepClone();
        }

        return result;
    }

    public static void Store(ICoreAPI api, InvTweaksConfig current, string path, bool preserveSavedScalars)
    {
        if (!preserveSavedScalars)
        {
            api.StoreModConfig(current, path);
            return;
        }

        JObject? saved;
        try
        {
            saved = api.LoadModConfig<JObject>(path);
        }
        catch (Exception exception)
        {
            // Never replace a malformed or temporarily unreadable file with stale in-memory settings.
            api.Logger.Error("[XInvTweaksFork] Could not preserve ConfigLib settings; config was not saved: {0}",
                exception.Message);
            return;
        }

        api.StoreModConfig(saved == null ? JObject.FromObject(current) : MergeSavedScalars(current, saved), path);
    }
}
