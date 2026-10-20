using Unity.Entities;

namespace vrising_server_monitor;

internal static class EntityExtensions
{
    public static bool Has<T>(this Entity entity) where T : struct
        => Core.EntityManager.HasComponent<T>(entity);

    public static T Read<T>(this Entity entity) where T : struct
        => Core.EntityManager.GetComponentData<T>(entity);
}