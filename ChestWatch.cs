using System;
using System.Collections.Generic;
using System.Linq;
using ProjectM;
using ProjectM.CastleBuilding;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace vrising_server_monitor;

/// <summary>Total amount of one item type.</summary>
internal readonly record struct ItemTotal(PrefabGUID Item, int Amount);

/// <summary>Shared building blocks for the chest-watch patches.</summary>
internal static class ChestWatch
{
    public static void Info(string message) => Core.Log.LogInfo($"[ChestWatch] {message}");

    /// <summary>Runs <paramref name="handle"/> for every entity in the query, isolating failures per event.</summary>
    public static void ForEachEvent(EntityQuery query, Action<Entity> handle)
    {
        var events = query.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (var evt in events)
            {
                try { handle(evt); }
                catch (Exception e) { Core.Log.LogError($"[ChestWatch] {e}"); }
            }
        }
        finally
        {
            events.Dispose();
        }
    }

    /// <summary>Resolves a NetworkId to its entity, logging an exit line on failure.</summary>
    public static bool TryResolveContainer(NetworkId id, string label, out Entity container)
    {
        if (NetworkIds.TryGetEntity(id, out container)) return true;
        Info($"Exit: {label} NetworkId not found");
        return false;
    }

    /// <summary>
    /// Finds the entity that carries a container's InventoryBuffer. Some containers carry it
    /// directly; others (chests) link to a separate inventory entity through their
    /// InventoryInstanceElement buffer.
    /// </summary>
    public static bool TryGetInventoryEntity(Entity container, out Entity inventory)
    {
        var em = Core.EntityManager;

        if (em.HasBuffer<InventoryBuffer>(container))
        {
            inventory = container;
            return true;
        }

        inventory = Entity.Null;

        if (!em.HasBuffer<InventoryInstanceElement>(container))
            return false;

        var instances = em.GetBuffer<InventoryInstanceElement>(container);
        if (instances.Length == 0)
            return false;

        var external = instances[0].ExternalInventoryEntity.GetEntityOnServer();
        if (external == Entity.Null || !em.HasBuffer<InventoryBuffer>(external))
            return false;

        Info($"Using external inventory entity {external.Index}:{external.Version}");
        inventory = external;
        return true;
    }

    /// <summary>Returns the InventoryBuffer slots of a container, following the external inventory link if needed.</summary>
    public static bool TryGetInventorySlots(Entity container, out DynamicBuffer<InventoryBuffer> slots)
    {
        if (!TryGetInventoryEntity(container, out var inventory))
        {
            slots = default;
            return false;
        }
        slots = Core.EntityManager.GetBuffer<InventoryBuffer>(inventory);
        return true;
    }

    /// <summary>TryGetInventorySlots for a source container, with the exit log and component dump on failure.</summary>
    public static bool TryGetSourceSlots(Entity source, out DynamicBuffer<InventoryBuffer> slots)
    {
        if (TryGetInventorySlots(source, out slots)) return true;
        Info("Exit: source has no InventoryBuffer");
        LogComponentTypes(source);
        return false;
    }

    /// <summary>Reads the item in a slot, logging an exit line if the slot is out of range or empty.</summary>
    public static bool TryGetSlotItem(DynamicBuffer<InventoryBuffer> slots, int slotIndex, out InventoryBuffer item)
    {
        item = default;
        if (slotIndex < 0 || slotIndex >= slots.Length)
        { Info($"Exit: slot {slotIndex} out of range ({slots.Length})"); return false; }
        item = slots[slotIndex];
        if (item.Amount <= 0)
        { Info("Exit: slot is empty"); return false; }
        return true;
    }

    /// <summary>Resolves the user that owns a container: CastleHeartConnection -> heart -> UserOwner.</summary>
    public static bool TryGetOwner(Entity container, out Entity ownerUser)
    {
        ownerUser = Entity.Null;
        if (!container.Has<CastleHeartConnection>())
        { Info("Exit: source has no CastleHeartConnection"); return false; }
        var heart = container.Read<CastleHeartConnection>().CastleHeartEntity.GetEntityOnServer();
        if (heart == Entity.Null || !heart.Has<UserOwner>())
        { Info("Exit: no heart or heart has no UserOwner"); return false; }
        ownerUser = heart.Read<UserOwner>().Owner.GetEntityOnServer();
        return true;
    }

    /// <summary>True (with an exit log) when the actor owns the container; taking from your own storage is never announced.</summary>
    public static bool IsOwnWithdrawal(Entity actorUser, Entity ownerUser)
    {
        if (ownerUser == actorUser)
        { Info("Exit: actor owns the container"); return true; }
        return false;
    }

    public static string UserName(Entity user) => user.Read<User>().CharacterName.ToString();

    /// <summary>Total amount per item type, keyed by the prefab GUID hash.</summary>
    public static Dictionary<int, ItemTotal> Snapshot(DynamicBuffer<InventoryBuffer> slots)
    {
        var totals = new Dictionary<int, ItemTotal>();
        for (var i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            if (slot.Amount <= 0) continue;
            var key = slot.ItemType.GuidHash;
            var amount = totals.TryGetValue(key, out var current) ? current.Amount + slot.Amount : slot.Amount;
            totals[key] = new ItemTotal(slot.ItemType, amount);
        }
        return totals;
    }

    /// <summary>Amounts that went down between two snapshots.</summary>
    public static List<ItemTotal> Decreases(Dictionary<int, ItemTotal> before, Dictionary<int, ItemTotal> after)
    {
        var taken = new List<ItemTotal>();
        foreach (var (key, was) in before)
        {
            var now = after.TryGetValue(key, out var current) ? current.Amount : 0;
            if (was.Amount > now) taken.Add(new ItemTotal(was.Item, was.Amount - now));
        }
        return taken;
    }

    /// <summary>Formats totals as "98x Wood_Standard, 12x Bone".</summary>
    public static string FormatItems(IEnumerable<ItemTotal> items)
        => string.Join(", ", items.Select(i => $"{i.Amount}x {Core.ItemDisplayName(i.Item)}"));

    public static void LogComponentTypes(Entity entity)
    {
        var types = Core.EntityManager.GetComponentTypes(entity, Allocator.Temp);
        try
        {
            var names = new string[types.Length];
            for (var i = 0; i < types.Length; i++)
                names[i] = types[i].ToString();
            Info($"Components on {entity.Index}:{entity.Version}: {string.Join(", ", names)}");
        }
        finally
        {
            types.Dispose();
        }
    }
}
