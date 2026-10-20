using System;
using HarmonyLib;
using ProjectM;
using ProjectM.CastleBuilding;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;

namespace vrising_server_monitor.Patches;

[HarmonyPatch(typeof(MoveItemBetweenInventoriesSystem), nameof(MoveItemBetweenInventoriesSystem.OnUpdate))]
public static class MoveItemPatch
{
    // Set to true to also announce taking from your own storage (useful for solo testing).
    const bool AnnounceOwnWithdrawals = false;

    public static void Prefix(MoveItemBetweenInventoriesSystem __instance)
    {
        var events = __instance._MoveItemBetweenInventoriesEventQuery.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (var evt in events)
            {
                try { HandleMove(evt); }
                catch (Exception e) { Core.Log.LogError($"[ChestWatch] {e}"); }
            }
        }
        finally
        {
            events.Dispose();
        }
    }

    /// <summary>
    /// Finds the InventoryBuffer slots for a container. Some containers carry the buffer
    /// directly; others (chests) link to a separate inventory entity through their
    /// InventoryInstanceElement buffer. Returns false if neither route yields slots.
    /// </summary>
    static bool TryGetInventorySlots(Entity container, out DynamicBuffer<InventoryBuffer> slots)
    {
        var em = Core.EntityManager;

        if (em.HasBuffer<InventoryBuffer>(container))
        {
            slots = em.GetBuffer<InventoryBuffer>(container);
            return true;
        }

        slots = default;

        if (!em.HasBuffer<InventoryInstanceElement>(container))
            return false;

        var instances = em.GetBuffer<InventoryInstanceElement>(container);
        if (instances.Length == 0)
            return false;

        var inventory = instances[0].ExternalInventoryEntity.GetEntityOnServer();
        if (inventory == Entity.Null || !em.HasBuffer<InventoryBuffer>(inventory))
            return false;

        Core.Log.LogInfo($"[ChestWatch] Using external inventory entity {inventory.Index}:{inventory.Version}");
        slots = em.GetBuffer<InventoryBuffer>(inventory);
        return true;
    }

    static void LogComponentTypes(Entity entity)
    {
        var types = Core.EntityManager.GetComponentTypes(entity, Allocator.Temp);
        try
        {
            var names = new string[types.Length];
            for (var i = 0; i < types.Length; i++)
                names[i] = types[i].ToString();
            Core.Log.LogInfo($"[ChestWatch] Components on {entity.Index}:{entity.Version}: {string.Join(", ", names)}");
        }
        finally
        {
            types.Dispose();
        }
    }

    static void HandleMove(Entity evt)
    {
        Core.Log.LogInfo("[ChestWatch] Move event received");

        var actorUser = evt.Read<FromCharacter>().User;
        var move = evt.Read<MoveItemBetweenInventoriesEvent>();
        Core.Log.LogInfo($"[ChestWatch] FromSlot={move.FromSlot} ToSlot={move.ToSlot} Method={move.TransferMethod}");

        if (!NetworkIds.TryGetEntity(move.FromInventory, out var source))
        { Core.Log.LogInfo("[ChestWatch] Exit: FromInventory NetworkId not found"); return; }
        if (!NetworkIds.TryGetEntity(move.ToInventory, out var target))
        { Core.Log.LogInfo("[ChestWatch] Exit: ToInventory NetworkId not found"); return; }
        if (source == target)
        { Core.Log.LogInfo("[ChestWatch] Exit: same inventory"); return; }

        if (!TryGetInventorySlots(source, out var slots))
        {
            Core.Log.LogInfo("[ChestWatch] Exit: source has no InventoryBuffer");
            LogComponentTypes(source);
            return;
        }
        if (move.FromSlot < 0 || move.FromSlot >= slots.Length)
        { Core.Log.LogInfo($"[ChestWatch] Exit: slot {move.FromSlot} out of range ({slots.Length})"); return; }
        var slot = slots[move.FromSlot];
        if (slot.Amount <= 0)
        { Core.Log.LogInfo("[ChestWatch] Exit: slot is empty"); return; }

        // Ownership is checked on the container itself, not on the inventory entity.
        if (!source.Has<CastleHeartConnection>())
        { Core.Log.LogInfo("[ChestWatch] Exit: source has no CastleHeartConnection"); return; }
        var heart = source.Read<CastleHeartConnection>().CastleHeartEntity.GetEntityOnServer();
        if (heart == Entity.Null || !heart.Has<UserOwner>())
        { Core.Log.LogInfo("[ChestWatch] Exit: no heart or heart has no UserOwner"); return; }
        var ownerUser = heart.Read<UserOwner>().Owner.GetEntityOnServer();

        if (ownerUser == actorUser && !AnnounceOwnWithdrawals)
        { Core.Log.LogInfo("[ChestWatch] Exit: actor owns the container"); return; }

        var actorName = actorUser.Read<User>().CharacterName.ToString();
        var ownerName = ownerUser.Read<User>().CharacterName.ToString();
        var itemName = Core.PrefabName(slot.ItemType);

        Core.Log.LogInfo($"[ChestWatch] Announcing: {actorName} took {slot.Amount}x {itemName} from {ownerName}");
        Chat.Broadcast($"{actorName} took {slot.Amount}x {itemName} from {ownerName}'s storage");
    }
}
