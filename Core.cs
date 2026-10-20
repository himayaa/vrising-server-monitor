using System;
using System.Linq;
using BepInEx.Logging;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;

namespace vrising_server_monitor;

internal static class Core
{
    public static ManualLogSource Log { get; set; }

    static World _server;
    public static World Server => _server ??= World.s_AllWorlds.ToArray()
        .FirstOrDefault(w => w.Name == "Server");

    public static EntityManager EntityManager => Server.EntityManager;

    static PrefabCollectionSystem _prefabCollection;

    /// <summary>Full prefab name, e.g. "Item_Ingredient_Wood_Standard", or the GUID as text if unknown.</summary>
    public static string PrefabName(PrefabGUID guid)
    {
        _prefabCollection ??= Server.GetExistingSystemManaged<PrefabCollectionSystem>();
        return _prefabCollection._PrefabLookupMap.TryGetName(guid, out var name)
            ? name
            : guid.ToString();
    }

    /// <summary>Prefab name shortened for announcements; see <see cref="ShortenItemName"/>.</summary>
    public static string ItemDisplayName(PrefabGUID guid) => ShortenItemName(PrefabName(guid));

    /// <summary>
    /// Drops the "Item_" prefix and the category segment after it:
    /// "Item_Ingredient_Wood_Standard" -> "Wood_Standard". If that would leave only a tier
    /// token, the category is kept: "Item_Something_T01" -> "Something_T01". Names that don't
    /// start with "Item_" or have fewer than three segments are returned unchanged.
    /// </summary>
    public static string ShortenItemName(string name)
    {
        if (!name.StartsWith("Item_", StringComparison.Ordinal)) return name;
        var parts = name.Split('_');
        if (parts.Length < 3) return name;
        var skip = parts.Length == 3 && IsTierToken(parts[2]) ? 1 : 2;
        return string.Join("_", parts, skip, parts.Length - skip);
    }

    /// <summary>"T01", "T2", ...</summary>
    static bool IsTierToken(string segment)
    {
        if (segment.Length < 2 || segment[0] != 'T') return false;
        for (var i = 1; i < segment.Length; i++)
            if (!char.IsDigit(segment[i])) return false;
        return true;
    }
}
