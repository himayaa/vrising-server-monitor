using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;

namespace vrising_server_monitor.Patches;

/// <summary>Single item moved between inventories (drag, shift-click, split).</summary>
[HarmonyPatch(typeof(MoveItemBetweenInventoriesSystem), nameof(MoveItemBetweenInventoriesSystem.OnUpdate))]
public static class MoveItemPatch
{
    public static void Prefix(MoveItemBetweenInventoriesSystem __instance)
        => ChestWatch.ForEachEvent(__instance._MoveItemBetweenInventoriesEventQuery, HandleMove);

    static void HandleMove(Entity evt)
    {
        ChestWatch.Info("Move event received");

        var actorUser = evt.Read<FromCharacter>().User;
        var move = evt.Read<MoveItemBetweenInventoriesEvent>();
        ChestWatch.Info($"FromSlot={move.FromSlot} ToSlot={move.ToSlot} Method={move.TransferMethod}");

        if (!ChestWatch.TryResolveContainer(move.FromInventory, "FromInventory", out var source)) return;
        if (!ChestWatch.TryResolveContainer(move.ToInventory, "ToInventory", out var target)) return;
        if (source == target)
        { ChestWatch.Info("Exit: same inventory"); return; }

        // Read the item before the move happens; afterward the slot is empty.
        if (!ChestWatch.TryGetSourceSlots(source, out var slots)) return;
        if (!ChestWatch.TryGetSlotItem(slots, move.FromSlot, out var slot)) return;

        // Ownership is checked on the container itself, not on the inventory entity.
        if (!ChestWatch.TryGetOwner(source, out var ownerUser)) return;
        if (ChestWatch.IsOwnWithdrawal(actorUser, ownerUser)) return;

        var actorName = ChestWatch.UserName(actorUser);
        var ownerName = ChestWatch.UserName(ownerUser);
        var itemName = Core.ItemDisplayName(slot.ItemType);

        ChestWatch.Info($"Announcing: {actorName} took {slot.Amount}x {itemName} from {ownerName}");
        Chat.Broadcast($"{actorName} took {slot.Amount}x {itemName} from {ownerName}'s storage");
    }
}
