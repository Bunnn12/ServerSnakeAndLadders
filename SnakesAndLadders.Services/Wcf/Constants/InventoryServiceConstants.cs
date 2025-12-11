namespace SnakesAndLadders.Services.Constants
{
    internal static class InventoryServiceConstants
    {
        internal const string ERROR_MESSAGE_TEMPLATE =
            "Error en InventoryService.{0}.";

        internal const string OP_GET_INVENTORY = "GetInventory";
        internal const string OP_UPDATE_SELECTED_ITEMS = "UpdateSelectedItems";
        internal const string OP_UPDATE_SELECTED_DICE = "UpdateSelectedDice";
        internal const string OP_EQUIP_ITEM_TO_SLOT = "EquipItemToSlot";
        internal const string OP_UNEQUIP_ITEM_FROM_SLOT = "UnequipItemFromSlot";
        internal const string OP_EQUIP_DICE_TO_SLOT = "EquipDiceToSlot";
        internal const string OP_UNEQUIP_DICE_FROM_SLOT = "UnequipDiceFromSlot";
    }
}
