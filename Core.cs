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

    public static string PrefabName(PrefabGUID guid)
    {
        _prefabCollection ??= Server.GetExistingSystemManaged<PrefabCollectionSystem>();
        return _prefabCollection._PrefabLookupMap.TryGetName(guid, out var name)
            ? name
            : guid.ToString();
    }
}