using ServerSnakesAndLadders.Common;

namespace SnakesAndLadders.Data.Constants
{
    internal static class InventoryRepositoryConstants
    {
        public const int COMMAND_TIMEOUT_SECONDS = 30;
        public const int MIN_VALID_USER_ID = 1;
        public const int MIN_OBJECT_ID = 1;
        public const int MIN_DICE_ID = 1;

        public const byte MIN_SLOT_NUMBER = 1;
        public const byte MAX_ITEM_SLOTS = 3;
        public const byte MAX_DICE_SLOTS = 2;

        public const string ERROR_USER_ID_POSITIVE = "userId must be positive.";
        public const string ERROR_OBJECT_ID_POSITIVE = "objectId must be positive.";
        public const string ERROR_DICE_ID_POSITIVE = "diceId must be positive.";
        public const string ERROR_ITEM_CODE_REQUIRED = "itemCode is required.";
        public const string ERROR_DICE_CODE_REQUIRED = "diceCode is required.";
        public const string ERROR_USER_DOES_NOT_OWN_ITEM = "User does not own the specified item.";
        public const string ERROR_USER_HAS_NO_ITEM_QUANTITY = "User has no available quantity for the specified item.";
        public const string ERROR_USER_DOES_NOT_OWN_DICE = "User does not own the specified dice.";
        public const string ERROR_USER_HAS_NO_DICE_QUANTITY = "User has no available quantity for the specified dice.";

        public const string ERROR_ITEM_CONFIG_NOT_FOUND_TEMPLATE =
            "No item configured with code '{0}'.";

        public const string ERROR_DICE_CONFIG_NOT_FOUND_TEMPLATE =
            "No dice configured with code '{0}'.";

        public const string ERROR_DATABASE_GRANTING_ITEM =
            "Database error while granting item to user.";

        public const string ERROR_UNEXPECTED_GRANTING_ITEM =
            "Unexpected error while granting item to user.";

        public const string ERROR_DATABASE_GRANTING_DICE =
            "Database error while granting dice to user.";

        public const string ERROR_UNEXPECTED_GRANTING_DICE =
            "Unexpected error while granting dice to user.";

        public const string LOG_SQL_GET_USER_ITEMS =
            "SQL error while loading user item inventory.";

        public const string LOG_UNEXPECTED_GET_USER_ITEMS =
            "Unexpected error while loading user item inventory.";

        public const string LOG_SQL_GET_USER_DICE =
            "SQL error while loading user dice inventory.";

        public const string LOG_UNEXPECTED_GET_USER_DICE =
            "Unexpected error while loading user dice inventory.";

        public const string LOG_SQL_GRANT_ITEM =
            "SQL error while granting item to user.";

        public const string LOG_UNEXPECTED_GRANT_ITEM =
            "Unexpected error while granting item to user.";

        public const string LOG_SQL_GRANT_DICE =
            "SQL error while granting dice to user.";

        public const string LOG_UNEXPECTED_GRANT_DICE =
            "Unexpected error while granting dice to user.";
    }
}
