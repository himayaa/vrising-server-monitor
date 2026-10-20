using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;

namespace vrising_server_monitor.Patches;

/// <summary>Shared "dropped from storage" announcement for the drop patches.</summary>
internal static class DropAnnouncer
{
    /// <summary>Applies the single-move filters to a source container and announces the item in the slot.</summary>
    public static void FromSlot(Entity actorUser, Entity source, int slotIndex)
    {
        // Read the item before the drop happens; afterward the slot is empty.
        if (!ChestWatch.TryGetSourceSlots(source, out var slots)) return;
        if (!ChestWatch.TryGetSlotItem(slots, slotIndex, out var item)) return;

        if (!ChestWatch.TryGetOwner(source, out var ownerUser)) return;
        if (ChestWatch.IsOwnWithdrawal(actorUser, ownerUser)) return;

        var actorName = ChestWatch.UserName(actorUser);
        var ownerName = ChestWatch.UserName(ownerUser);
        var itemName = Core.ItemDisplayName(item.ItemType);

        ChestWatch.Info($"Announcing: {actorName} dropped {item.Amount}x {itemName} from {ownerName}");
        Chat.Broadcast($"{actorName} dropped {item.Amount}x {itemName} from {ownerName}'s storage");
    }
}

/// <summary>
/// Dragging an item out of a container onto the ground raises DropInventoryItemEvent
/// (NetworkId Inventory + SlotIndex), handled by DropInventoryItemSystem.
/// </summary>
[HarmonyPatch(typeof(DropInventoryItemSystem), nameof(DropInventoryItemSystem.OnUpdate))]
public static class DropInventoryItemPatch
{
    public static void Prefix(DropInventoryItemSystem __instance)
        => ChestWatch.ForEachEvent(__instance._Query, HandleDrop);

    static void HandleDrop(Entity evt)
    {
        ChestWatch.Info("Drop event received (DropInventoryItemSystem)");

        var actorUser = evt.Read<FromCharacter>().User;
        var drop = evt.Read<DropInventoryItemEvent>();
        ChestWatch.Info($"SlotIndex={drop.SlotIndex}");

        if (!ChestWatch.TryResolveContainer(drop.Inventory, "Inventory", out var source)) return;
        DropAnnouncer.FromSlot(actorUser, source, drop.SlotIndex);
    }
}

// Not patched: DropItemSystem (DropItemAtSlotEvent) and DropEntireInventoryEvent carry no
// inventory reference and always act on the acting character's own bag, so they can never
// involve someone else's storage.
