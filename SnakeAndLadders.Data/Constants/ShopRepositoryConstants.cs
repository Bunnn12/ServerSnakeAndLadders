using System;

namespace SnakesAndLadders.Data.Constants
{
    internal static class ShopRepositoryConstants
    {
        public const byte STATUS_ACTIVE = 0x01;
        public const int STATUS_MIN_LENGTH = 1;
        public const int STATUS_ACTIVE_INDEX = 0;

        public const int MIN_VALID_DICE_ID = 1;
        public const int RANDOM_MIN_VALUE = 0;

        public const string LOG_WARN_NO_AVATAR_CANDIDATES_FOR_RARITY =
            "No avatar candidates found for rarity {0}.";

        public const string LOG_WARN_NO_STICKER_PACK_CANDIDATES_FOR_RARITY =
            "No sticker pack candidates found for rarity {0}.";

        public const string LOG_WARN_ITEM_NOT_FOUND_FOR_CODE =
            "Item not found for code {0}.";

        public const string LOG_SQL_ERROR_PURCHASE_AVATAR_CHEST =
            "SQL error while purchasing avatar chest.";

        public const string LOG_EF_ERROR_PURCHASE_AVATAR_CHEST =
            "EF error while purchasing avatar chest.";

        public const string LOG_UNEXPECTED_ERROR_PURCHASE_AVATAR_CHEST =
            "Unexpected error while purchasing avatar chest.";

        public const string LOG_FATAL_ERROR_CREATE_CONTEXT_AVATAR_CHEST =
            "Fatal error creating EF context for avatar chest.";

        public const string LOG_SQL_ERROR_PURCHASE_STICKER_CHEST =
            "SQL error while purchasing sticker chest.";

        public const string LOG_EF_ERROR_PURCHASE_STICKER_CHEST =
            "EF error while purchasing sticker chest.";

        public const string LOG_UNEXPECTED_ERROR_PURCHASE_STICKER_CHEST =
            "Unexpected error while purchasing sticker chest.";

        public const string LOG_FATAL_ERROR_CREATE_CONTEXT_STICKER_CHEST =
            "Fatal error creating EF context for sticker chest.";

        public const string LOG_SQL_ERROR_PURCHASE_DICE =
            "SQL error while purchasing dice.";

        public const string LOG_EF_ERROR_PURCHASE_DICE =
            "EF error while purchasing dice.";

        public const string LOG_UNEXPECTED_ERROR_PURCHASE_DICE =
            "Unexpected error while purchasing dice.";

        public const string LOG_FATAL_ERROR_CREATE_CONTEXT_DICE =
            "Fatal error creating EF context for dice purchase.";

        public const string LOG_SQL_ERROR_PURCHASE_ITEM_CHEST =
            "SQL error while purchasing item chest.";

        public const string LOG_EF_ERROR_PURCHASE_ITEM_CHEST =
            "EF error while purchasing item chest.";

        public const string LOG_UNEXPECTED_ERROR_PURCHASE_ITEM_CHEST =
            "Unexpected error while purchasing item chest.";

        public const string LOG_FATAL_ERROR_CREATE_CONTEXT_ITEM_CHEST =
            "Fatal error creating EF context for item chest.";

        public const string LOG_SQL_ERROR_GET_CURRENT_COINS =
            "SQL error while getting current coins.";

        public const string LOG_EF_ERROR_GET_CURRENT_COINS =
            "EF error while getting current coins.";

        public const string LOG_UNEXPECTED_ERROR_GET_CURRENT_COINS =
            "Unexpected error while getting current coins.";

        public const string LOG_SQL_ERROR_GET_STICKERS_FOR_USER =
            "SQL error while getting stickers for user.";

        public const string LOG_EF_ERROR_GET_STICKERS_FOR_USER =
            "EF error while getting stickers for user.";

        public const string LOG_UNEXPECTED_ERROR_GET_STICKERS_FOR_USER =
            "Unexpected error while getting stickers for user.";
    }
}
