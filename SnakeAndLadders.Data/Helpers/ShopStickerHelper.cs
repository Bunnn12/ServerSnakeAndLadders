using SnakesAndLadders.Data.Constants;

namespace SnakesAndLadders.Data.Helpers
{
    internal static class ShopStickerHelper
    {
        public static bool IsActiveSticker(Sticker sticker)
        {
            if (sticker == null)
            {
                return false;
            }

            byte[] status = sticker.Estado;

            return status != null
                   && status.Length >= ShopRepositoryConstants.STATUS_MIN_LENGTH
                   && status[ShopRepositoryConstants.STATUS_ACTIVE_INDEX] == ShopRepositoryConstants.STATUS_ACTIVE;
        }
    }
}
