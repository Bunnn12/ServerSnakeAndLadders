

namespace SnakesAndLadders.Services.Constants
{
    public static class InventoryAppServiceConstants
    {
        public const int MIN_VALID_USER_ID = 1;

        public const byte MIN_ITEM_SLOT = 1;
        public const byte MAX_ITEM_SLOT = 3;

        public const byte MIN_DICE_SLOT = 1;
        public const byte MAX_DICE_SLOT = 2;

        public const string ERROR_USER_ID_POSITIVE = "userId must be positive.";
        public const string ERROR_ITEM_SLOT_RANGE = "slotNumber is not a valid item slot.";
        public const string ERROR_DICE_SLOT_RANGE = "slotNumber is not a valid dice slot.";
        public const string ERROR_OBJECT_ID_POSITIVE = "objectId must be positive.";
        public const string ERROR_DICE_ID_POSITIVE = "diceId must be positive.";

        public const string ERROR_USER_NO_ITEM_INVENTORY = "El usuario no tiene inventario de objetos.";
        public const string ERROR_USER_NO_DICE_INVENTORY = "El usuario no tiene inventario de dados.";
        public const string ERROR_USER_NO_ITEM_OR_QUANTITY =
            "El usuario no posee el objeto especificado o no tiene cantidad disponible.";
        public const string ERROR_USER_NO_DICE_OR_QUANTITY =
            "El usuario no posee el dado especificado o no tiene cantidad disponible.";

        public const string ERROR_DUPLICATED_SELECTION_TEMPLATE =
            "No se puede asignar el mismo identificador más de una vez en los {0}.";

        public const string CONTEXT_SELECTED_ITEMS = "objetos seleccionados";
        public const string CONTEXT_SELECTED_DICE = "dados seleccionados";
    }
}
