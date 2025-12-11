using SnakeAndLadders.Contracts.Enums;
using SnakesAndLadders.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Data.Helpers
{
    internal static class ShopSelectionHelper
    {
        internal static ShopChestRarity GetStickerPackRarity(string packCode)
        {
            if (string.IsNullOrWhiteSpace(packCode))
            {
                return ShopChestRarity.Common;
            }

            string normalizedCode = packCode.ToUpperInvariant();

            if (string.Equals(normalizedCode, ShopRulesRepository.STICKER_PACK_REVO, StringComparison.Ordinal))
            {
                return ShopChestRarity.Legendary;
            }

            if (string.Equals(normalizedCode, ShopRulesRepository.STICKER_PACK_OCHARAN, StringComparison.Ordinal))
            {
                return ShopChestRarity.Legendary;
            }

            if (string.Equals(normalizedCode, ShopRulesRepository.STICKER_PACK_SAUL, StringComparison.Ordinal))
            {
                return ShopChestRarity.Epic;
            }

            if (string.Equals(normalizedCode, ShopRulesRepository.STICKER_PACK_LIZ, StringComparison.Ordinal))
            {
                return ShopChestRarity.Epic;
            }

            return ShopChestRarity.Common;
        }

        internal static string GetRandomItemCode()
        {
            int totalWeight =
                ShopRulesRepository.ITEM_WEIGHT_ROCKET +
                ShopRulesRepository.ITEM_WEIGHT_ANCHOR +
                ShopRulesRepository.ITEM_WEIGHT_SWAP +
                ShopRulesRepository.ITEM_WEIGHT_FREEZE +
                ShopRulesRepository.ITEM_WEIGHT_SHIELD;

            int roll = ShopRandomHelper.NextExclusive(totalWeight);

            if (roll < ShopRulesRepository.ITEM_WEIGHT_ROCKET)
            {
                return ShopRulesRepository.ITEM_CODE_ROCKET;
            }

            roll -= ShopRulesRepository.ITEM_WEIGHT_ROCKET;

            if (roll < ShopRulesRepository.ITEM_WEIGHT_ANCHOR)
            {
                return ShopRulesRepository.ITEM_CODE_ANCHOR;
            }

            roll -= ShopRulesRepository.ITEM_WEIGHT_ANCHOR;

            if (roll < ShopRulesRepository.ITEM_WEIGHT_SWAP)
            {
                return ShopRulesRepository.ITEM_CODE_SWAP;
            }

            roll -= ShopRulesRepository.ITEM_WEIGHT_SWAP;

            if (roll < ShopRulesRepository.ITEM_WEIGHT_FREEZE)
            {
                return ShopRulesRepository.ITEM_CODE_FREEZE;
            }

            return ShopRulesRepository.ITEM_CODE_SHIELD;
        }

        internal static Avatar GetRandomAvatar(IList<Avatar> avatars)
        {
            if (avatars == null || avatars.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(avatars));
            }

            int totalWeight = avatars.Sum(GetAvatarWeight);

            if (totalWeight <= 0)
            {
                int indexFallback = ShopRandomHelper.NextExclusive(avatars.Count);
                return avatars[indexFallback];
            }

            int roll = ShopRandomHelper.NextExclusive(totalWeight);

            foreach (Avatar avatar in avatars)
            {
                int weight = GetAvatarWeight(avatar);

                if (weight <= 0)
                {
                    continue;
                }

                if (roll < weight)
                {
                    return avatar;
                }

                roll -= weight;
            }

            return avatars[avatars.Count - 1];
        }

        internal static int GetAvatarWeight(Avatar avatar)
        {
            if (avatar == null)
            {
                return ShopRulesRepository.AVATAR_WEIGHT_DEFAULT;
            }

            string code = avatar.CodigoAvatar;

            if (string.Equals(code, ShopRulesRepository.AVATAR_CODE_MARIA, StringComparison.OrdinalIgnoreCase))
            {
                return ShopRulesRepository.AVATAR_WEIGHT_MARIA;
            }

            if (string.Equals(code, ShopRulesRepository.AVATAR_CODE_LIZ, StringComparison.OrdinalIgnoreCase))
            {
                return ShopRulesRepository.AVATAR_WEIGHT_LIZ;
            }

            if (string.Equals(code, ShopRulesRepository.AVATAR_CODE_REVO, StringComparison.OrdinalIgnoreCase))
            {
                return ShopRulesRepository.AVATAR_WEIGHT_REVO;
            }

            if (string.Equals(code, ShopRulesRepository.AVATAR_CODE_OCHARAN, StringComparison.OrdinalIgnoreCase))
            {
                return ShopRulesRepository.AVATAR_WEIGHT_OCHARAN;
            }

            if (string.Equals(code, ShopRulesRepository.AVATAR_CODE_SAUL, StringComparison.OrdinalIgnoreCase))
            {
                return ShopRulesRepository.AVATAR_WEIGHT_SAUL;
            }

            if (string.Equals(code, ShopRulesRepository.AVATAR_CODE_JAIME, StringComparison.OrdinalIgnoreCase))
            {
                return ShopRulesRepository.AVATAR_WEIGHT_JAIME;
            }

            if (string.Equals(code, ShopRulesRepository.AVATAR_CODE_WILLY, StringComparison.OrdinalIgnoreCase))
            {
                return ShopRulesRepository.AVATAR_WEIGHT_WILLY;
            }

            return ShopRulesRepository.AVATAR_WEIGHT_DEFAULT;
        }

        internal static PaqueteStickers GetRandomStickerPack(IList<PaqueteStickers> packs)
        {
            if (packs == null || packs.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(packs));
            }

            int totalWeight = packs.Sum(GetStickerPackWeight);

            if (totalWeight <= 0)
            {
                int indexFallback = ShopRandomHelper.NextExclusive(packs.Count);
                return packs[indexFallback];
            }

            int roll = ShopRandomHelper.NextExclusive(totalWeight);

            foreach (PaqueteStickers pack in packs)
            {
                int weight = GetStickerPackWeight(pack);

                if (weight <= 0)
                {
                    continue;
                }

                if (roll < weight)
                {
                    return pack;
                }

                roll -= weight;
            }

            return packs[packs.Count - 1];
        }

        internal static int GetStickerPackWeight(PaqueteStickers pack)
        {
            if (pack == null)
            {
                return ShopRulesRepository.STICKER_PACK_WEIGHT_DEFAULT;
            }

            string code = pack.CodigoPaqueteStickers;

            if (string.Equals(code, ShopRulesRepository.STICKER_PACK_REVO, StringComparison.OrdinalIgnoreCase))
            {
                return ShopRulesRepository.STICKER_PACK_WEIGHT_REVO;
            }

            if (string.Equals(code, ShopRulesRepository.STICKER_PACK_OCHARAN, StringComparison.OrdinalIgnoreCase))
            {
                return ShopRulesRepository.STICKER_PACK_WEIGHT_OCHARAN;
            }

            if (string.Equals(code, ShopRulesRepository.STICKER_PACK_LIZ, StringComparison.OrdinalIgnoreCase))
            {
                return ShopRulesRepository.STICKER_PACK_WEIGHT_LIZ;
            }

            if (string.Equals(code, ShopRulesRepository.STICKER_PACK_SAUL, StringComparison.OrdinalIgnoreCase))
            {
                return ShopRulesRepository.STICKER_PACK_WEIGHT_SAUL;
            }

            if (string.Equals(code, ShopRulesRepository.STICKER_PACK_JAIME, StringComparison.OrdinalIgnoreCase))
            {
                return ShopRulesRepository.STICKER_PACK_WEIGHT_JAIME;
            }

            if (string.Equals(code, ShopRulesRepository.STICKER_PACK_WILLY, StringComparison.OrdinalIgnoreCase))
            {
                return ShopRulesRepository.STICKER_PACK_WEIGHT_WILLY;
            }

            return ShopRulesRepository.STICKER_PACK_WEIGHT_DEFAULT;
        }
    }
}
