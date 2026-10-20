using System;
using System.Collections.Generic;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;

namespace vrising_server_monitor.Patches;

/// <summary>A qualifying take-all request captured before the system moves the items.</summary>
internal sealed class TakeAllSnapshot
{
    public Entity Actor;
    public Entity Owner;
    public Entity Inventory;
    public Dictionary<int, ItemTotal> Before;
}

/// <summary>Shared Prefix/Postfix logic for both take-all systems.</summary>
internal static class TakeAll
{
    /// <summary>Clears leftovers from a run whose Postfix never ran (e.g. the original threw).</summary>
    public static void Begin(List<TakeAllSnapshot> pending, string system)
    {
        if (pending.Count == 0) return;
        ChestWatch.Info($"Discarding {pending.Count} stale snapshot(s) ({system})");
        pending.Clear();
    }

    /// <summary>Applies the single-move filters and, if the event qualifies, stores a snapshot of the source.</summary>
    public static void Record(List<TakeAllSnapshot> pending, string system, Entity evt, NetworkId fromInventory, NetworkId toInventory)
    {
        var actorUser = evt.Read<FromCharacter>().User;

        if (!ChestWatch.TryResolveContainer(fromInventory, "FromInventory", out var source)) return;
        if (!ChestWatch.TryResolveContainer(toInventory, "ToInventory", out var target)) return;
        if (source == target)
        { ChestWatch.Info("Exit: same inventory"); return; }

        if (!ChestWatch.TryGetInventoryEntity(source, out var inventory))
        {
            ChestWatch.Info("Exit: source has no InventoryBuffer");
            ChestWatch.LogComponentTypes(source);
            return;
        }

        if (!ChestWatch.TryGetOwner(source, out var ownerUser)) return;
        if (ChestWatch.IsOwnWithdrawal(actorUser, ownerUser)) return;

        var before = ChestWatch.Snapshot(Core.EntityManager.GetBuffer<InventoryBuffer>(inventory));
        if (before.Count == 0)
        { ChestWatch.Info("Exit: source is empty"); return; }

        pending.Add(new TakeAllSnapshot { Actor = actorUser, Owner = ownerUser, Inventory = inventory, Before = before });
        ChestWatch.Info($"Snapshot stored: {before.Count} item type(s) in source ({system})");
    }

    /// <summary>Compares each snapshot with the source's current contents and announces what was taken.</summary>
    public static void Flush(List<TakeAllSnapshot> pending)
    {
        try
        {
            foreach (var snapshot in pending)
            {
                try { Announce(snapshot); }
                catch (Exception e) { Core.Log.LogError($"[ChestWatch] {e}"); }
            }
        }
        finally
        {
            pending.Clear();
        }
    }

    static void Announce(TakeAllSnapshot snapshot)
    {
        var em = Core.EntityManager;
        if (!em.Exists(snapshot.Inventory) || !em.HasBuffer<InventoryBuffer>(snapshot.Inventory))
        { ChestWatch.Info("Exit: source inventory no longer readable after take-all"); return; }

        var after = ChestWatch.Snapshot(em.GetBuffer<InventoryBuffer>(snapshot.Inventory));
        var taken = ChestWatch.Decreases(snapshot.Before, after);
        if (taken.Count == 0)
        { ChestWatch.Info("Exit: nothing was taken"); return; }

        var actorName = ChestWatch.UserName(snapshot.Actor);
        var ownerName = ChestWatch.UserName(snapshot.Owner);
        var items = ChestWatch.FormatItems(taken);

        ChestWatch.Info($"Announcing: {actorName} took {items} from {ownerName}");
        Chat.Broadcast($"{actorName} took {items} from {ownerName}'s storage");
    }
}

/// <summary>"Take all" via the original system (MoveAllItemsBetweenInventoriesEvent).</summary>
[HarmonyPatch(typeof(MoveAllItemsBetweenInventoriesSystem), nameof(MoveAllItemsBetweenInventoriesSystem.OnUpdate))]
public static class MoveAllItemsPatch
{
    const string SystemName = "MoveAllItemsBetweenInventoriesSystem";
    static readonly List<TakeAllSnapshot> Pending = new();

    public static void Prefix(MoveAllItemsBetweenInventoriesSystem __instance)
    {
        TakeAll.Begin(Pending, SystemName);
        ChestWatch.ForEachEvent(__instance._EventQuery, evt =>
        {
            ChestWatch.Info($"Take-all event received ({SystemName})");
            var move = evt.Read<MoveAllItemsBetweenInventoriesEvent>();
            TakeAll.Record(Pending, SystemName, evt, move.FromInventory, move.ToInventory);
        });
    }

    public static void Postfix() => TakeAll.Flush(Pending);
}

/// <summary>"Take all" via the V2 system (MoveAllItemsBetweenInventoriesEventV2, adds FilteredItem).</summary>
[HarmonyPatch(typeof(MoveAllItemsBetweenInventoriesV2System), nameof(MoveAllItemsBetweenInventoriesV2System.OnUpdate))]
public static class MoveAllItemsV2Patch
{
    const string SystemName = "MoveAllItemsBetweenInventoriesV2System";
    static readonly List<TakeAllSnapshot> Pending = new();

    public static void Prefix(MoveAllItemsBetweenInventoriesV2System __instance)
    {
        TakeAll.Begin(Pending, SystemName);
        ChestWatch.ForEachEvent(__instance._EventQuery, evt =>
        {
            ChestWatch.Info($"Take-all event received ({SystemName})");
            var move = evt.Read<MoveAllItemsBetweenInventoriesEventV2>();
            ChestWatch.Info($"FilteredItem={Core.PrefabName(move.FilteredItem)}");
            TakeAll.Record(Pending, SystemName, evt, move.FromInventory, move.ToInventory);
        });
    }

    public static void Postfix() => TakeAll.Flush(Pending);
}
