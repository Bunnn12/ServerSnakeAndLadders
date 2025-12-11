namespace SnakesAndLadders.Services.Constants
{
    internal static class ShopAppServiceConstants
    {
        internal const string ERROR_INVALID_SESSION = "SHOP_INVALID_SESSION";
        internal const string ERROR_NULL_RESULT = "SHOP_NULL_RESULT";
        internal const string ERROR_NULL_DATA = "SHOP_NULL_DATA";

        internal const int PRICE_AVATAR_COMMON = 200;
        internal const int PRICE_AVATAR_EPIC = 400;
        internal const int PRICE_AVATAR_LEGENDARY = 800;

        internal const int PRICE_STICKER_COMMON = 150;
        internal const int PRICE_STICKER_EPIC = 300;
        internal const int PRICE_STICKER_LEGENDARY = 600;

        internal const int PRICE_DICE_DEFAULT = 300;

        internal const int PRICE_ITEM_CHEST = 500;

        // Log templates
        internal const string LOG_PURCHASE_AVATAR_CHEST =
            "PurchaseAvatarChest. UserId={0}, Rarity={1}, Price={2}";

        internal const string LOG_PURCHASE_STICKER_CHEST =
            "PurchaseStickerChest. UserId={0}, Rarity={1}, Price={2}";

        internal const string LOG_PURCHASE_DICE =
            "PurchaseDice. UserId={0}, DiceId={1}, Price={2}";

        internal const string LOG_PURCHASE_ITEM_CHEST =
            "PurchaseItemChest. UserId={0}, Price={1}";

        internal const string LOG_GET_CURRENT_COINS =
            "GetCurrentCoins. UserId={0}";

        internal const string LOG_GET_USER_STICKERS =
            "GetUserStickers. UserId={0}";
    }
}
