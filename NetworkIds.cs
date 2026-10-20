using ProjectM.Network;
using Unity.Entities;

namespace vrising_server_monitor;

internal static class NetworkIds
{
    static EntityQuery? _singletonQuery;

    static EntityQuery CreateSingletonQuery()
    {
        // NetworkIdSystem is an unmanaged ISystem, so its Singleton component lives on the
        // system's own entity. Plain queries skip system entities unless IncludeSystems is set.
        var desc = new EntityQueryDesc
        {
            All = new ComponentType[] { ComponentType.ReadOnly<NetworkIdSystem.Singleton>() },
            Options = EntityQueryOptions.IncludeSystems,
        };
        return Core.EntityManager.CreateEntityQuery(new EntityQueryDesc[] { desc });
    }

    public static bool TryGetEntity(NetworkId id, out Entity entity)
    {
        _singletonQuery ??= CreateSingletonQuery();

        if (!_singletonQuery.Value.TryGetSingleton<NetworkIdSystem.Singleton>(out var singleton))
        {
            Core.Log.LogWarning("[ChestWatch] NetworkIdSystem.Singleton not found");
            entity = Entity.Null;
            return false;
        }

        if (!singleton._NetworkIdLookupMap.TryGetValue(id, out entity))
        {
            Core.Log.LogWarning("[ChestWatch] NetworkId missing from lookup map");
            entity = Entity.Null;
            return false;
        }

        return true;
    }
}
