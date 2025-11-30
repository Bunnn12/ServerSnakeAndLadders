using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Data.Repositories
{
    public static class ShopRulesRepository
    {
        public const int MIN_USER_ID = 1;

        public const string ERROR_INVALID_USER_ID = "SHOP_INVALID_USER_ID";
        public const string ERROR_USER_NOT_FOUND = "SHOP_USER_NOT_FOUND";
        public const string ERROR_INSUFFICIENT_COINS = "SHOP_INSUFFICIENT_COINS";
        public const string ERROR_NO_AVATARS_FOR_RARITY = "SHOP_NO_AVATARS_FOR_RARITY";
        public const string ERROR_NO_STICKER_PACKS = "SHOP_NO_STICKER_PACKS";
        public const string ERROR_INVALID_DICE_ID = "SHOP_INVALID_DICE_ID";
        public const string ERROR_DICE_NOT_FOUND = "SHOP_DICE_NOT_FOUND";
        public const string ERROR_ITEM_NOT_FOUND = "SHOP_ITEM_NOT_FOUND";
        public const string ERROR_DB = "SHOP_DB_ERROR";
        public const string ERROR_PERSISTENCE = "SHOP_PERSISTENCE_ERROR";
        public const string ERROR_UNEXPECTED = "SHOP_UNEXPECTED_ERROR";
        public const string ERROR_FATAL = "SHOP_FATAL_ERROR";

        public const string ITEM_CODE_ROCKET = "IT_ROCKET";
        public const string ITEM_CODE_ANCHOR = "IT_ANCHOR";
        public const string ITEM_CODE_SWAP = "IT_SWAP";
        public const string ITEM_CODE_FREEZE = "IT_FREEZE";
        public const string ITEM_CODE_SHIELD = "IT_SHIELD";

        public const int AVATAR_WEIGHT_DEFAULT = 10;

        public const string AVATAR_CODE_MARIA = "009";
        public const string AVATAR_CODE_LIZ = "004";
        public const string AVATAR_CODE_REVO = "001";
        public const string AVATAR_CODE_OCHARAN = "003";
        public const string AVATAR_CODE_SAUL = "005";
        public const string AVATAR_CODE_JAIME = "010";
        public const string AVATAR_CODE_WILLY = "011";

        public const int AVATAR_WEIGHT_MARIA = 40;
        public const int AVATAR_WEIGHT_LIZ = 20;
        public const int AVATAR_WEIGHT_REVO = 5;
        public const int AVATAR_WEIGHT_OCHARAN = 10;
        public const int AVATAR_WEIGHT_SAUL = 15;
        public const int AVATAR_WEIGHT_JAIME = 30;
        public const int AVATAR_WEIGHT_WILLY = 30;

        public const int ITEM_WEIGHT_ROCKET = 30;
        public const int ITEM_WEIGHT_ANCHOR = 30;
        public const int ITEM_WEIGHT_SWAP = 20;
        public const int ITEM_WEIGHT_FREEZE = 10;
        public const int ITEM_WEIGHT_SHIELD = 10;

        public const int STICKER_PACK_WEIGHT_DEFAULT = 10;

        public const string STICKER_PACK_REVO = "STP01";
        public const string STICKER_PACK_OCHARAN = "STP02";
        public const string STICKER_PACK_LIZ = "STP03";
        public const string STICKER_PACK_SAUL = "STP04";
        public const string STICKER_PACK_JAIME = "STP05";
        public const string STICKER_PACK_WILLY = "STP06";

        public const int STICKER_PACK_WEIGHT_REVO = 8;
        public const int STICKER_PACK_WEIGHT_OCHARAN = 5;
        public const int STICKER_PACK_WEIGHT_LIZ = 25;
        public const int STICKER_PACK_WEIGHT_SAUL = 20;
        public const int STICKER_PACK_WEIGHT_JAIME = 30;
        public const int STICKER_PACK_WEIGHT_WILLY = 27;
    }
}
