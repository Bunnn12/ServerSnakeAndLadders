using System;
using System.Collections.Generic;
using System.ServiceModel;
using log4net;
using ServerSnakesAndLadders.Common;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Enums;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Services.Constants;

namespace SnakesAndLadders.Services.Logic
{
    public sealed class ShopAppService : IShopAppService
    {
        private static readonly ILog _logger =
            LogManager.GetLogger(typeof(ShopAppService));

        private readonly IShopRepository _shopRepository;
        private readonly Func<string, int> _getUserIdFromToken;

        public ShopAppService(
            IShopRepository shopRepository,
            Func<string, int> getUserIdFromToken)
        {
            _shopRepository = shopRepository
                ?? throw new ArgumentNullException(nameof(shopRepository));
            _getUserIdFromToken = getUserIdFromToken
                ?? throw new ArgumentNullException(nameof(getUserIdFromToken));
        }

        public ShopRewardDto PurchaseAvatarChest(
            string token,
            ShopChestRarity rarity)
        {
            int userId = EnsureUser(token);
            int price = GetAvatarPrice(rarity);

            _logger.InfoFormat(
                ShopAppServiceConstants.LOG_PURCHASE_AVATAR_CHEST,
                userId,
                rarity,
                price);

            var request = new AvatarChestPurchaseDto
            {
                UserId = userId,
                Rarity = rarity,
                PriceCoins = price
            };

            OperationResult<ShopRewardDto> result =
                _shopRepository.PurchaseAvatarChest(request);

            return EnsureRewardResult(result);
        }

        public ShopRewardDto PurchaseStickerChest(
            string token,
            ShopChestRarity rarity)
        {
            int userId = EnsureUser(token);
            int price = GetStickerPrice(rarity);

            _logger.InfoFormat(
                ShopAppServiceConstants.LOG_PURCHASE_STICKER_CHEST,
                userId,
                rarity,
                price);

            var request = new StickerChestPurchaseDto
            {
                UserId = userId,
                Rarity = rarity,
                PriceCoins = price
            };

            OperationResult<ShopRewardDto> result =
                _shopRepository.PurchaseStickerChest(request);

            return EnsureRewardResult(result);
        }

        public ShopRewardDto PurchaseDice(
            string token,
            int diceId)
        {
            int userId = EnsureUser(token);

            _logger.InfoFormat(
                ShopAppServiceConstants.LOG_PURCHASE_DICE,
                userId,
                diceId,
                ShopAppServiceConstants.PRICE_DICE_DEFAULT);

            var request = new DicePurchaseDto
            {
                UserId = userId,
                DiceId = diceId,
                PriceCoins = ShopAppServiceConstants.PRICE_DICE_DEFAULT
            };

            OperationResult<ShopRewardDto> result =
                _shopRepository.PurchaseDice(request);

            return EnsureRewardResult(result);
        }

        public ShopRewardDto PurchaseItemChest(string token)
        {
            int userId = EnsureUser(token);

            _logger.InfoFormat(
                ShopAppServiceConstants.LOG_PURCHASE_ITEM_CHEST,
                userId,
                ShopAppServiceConstants.PRICE_ITEM_CHEST);

            var request = new ItemChestPurchaseDto
            {
                UserId = userId,
                PriceCoins = ShopAppServiceConstants.PRICE_ITEM_CHEST
            };

            OperationResult<ShopRewardDto> result =
                _shopRepository.PurchaseItemChest(request);

            return EnsureRewardResult(result);
        }

        public int GetCurrentCoins(string token)
        {
            int userId = EnsureUser(token);

            _logger.InfoFormat(
                ShopAppServiceConstants.LOG_GET_CURRENT_COINS,
                userId);

            OperationResult<int> result =
                _shopRepository.GetCurrentCoins(userId);

            EnsureResultNotNull(result);
            EnsureResultSuccess(result);

            return result.Data;
        }

        public List<StickerDto> GetUserStickers(string token)
        {
            int userId = EnsureUser(token);

            _logger.InfoFormat(
                ShopAppServiceConstants.LOG_GET_USER_STICKERS,
                userId);

            OperationResult<List<StickerDto>> result =
                _shopRepository.GetUserStickers(userId);

            EnsureResultNotNull(result);
            EnsureResultSuccess(result);

            if (result.Data == null)
            {
                throw new FaultException(
                    ShopAppServiceConstants.ERROR_NULL_DATA);
            }

            return result.Data;
        }

        private int EnsureUser(string token)
        {
            int userId = _getUserIdFromToken(token);

            if (userId <= 0)
            {
                throw new FaultException(
                    ShopAppServiceConstants.ERROR_INVALID_SESSION);
            }

            return userId;
        }

        private static ShopRewardDto EnsureRewardResult(
            OperationResult<ShopRewardDto> result)
        {
            EnsureResultNotNull(result);
            EnsureResultSuccess(result);

            if (result.Data == null)
            {
                throw new FaultException(
                    ShopAppServiceConstants.ERROR_NULL_DATA);
            }

            return result.Data;
        }

        private static void EnsureResultNotNull<T>(
            OperationResult<T> result)
        {
            if (result == null)
            {
                throw new FaultException(
                    ShopAppServiceConstants.ERROR_NULL_RESULT);
            }
        }

        private static void EnsureResultSuccess<T>(
            OperationResult<T> result)
        {
            if (result.IsSuccess)
            {
                return;
            }

            string code = string.IsNullOrWhiteSpace(result.ErrorMessage)
                ? ShopAppServiceConstants.ERROR_NULL_RESULT
                : result.ErrorMessage;

            throw new FaultException(code);
        }

        private static int GetAvatarPrice(ShopChestRarity rarity)
        {
            switch (rarity)
            {
                case ShopChestRarity.Common:
                    return ShopAppServiceConstants.PRICE_AVATAR_COMMON;
                case ShopChestRarity.Epic:
                    return ShopAppServiceConstants.PRICE_AVATAR_EPIC;
                case ShopChestRarity.Legendary:
                    return ShopAppServiceConstants.PRICE_AVATAR_LEGENDARY;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(rarity),
                        rarity,
                        "Unsupported rarity.");
            }
        }

        private static int GetStickerPrice(ShopChestRarity rarity)
        {
            switch (rarity)
            {
                case ShopChestRarity.Common:
                    return ShopAppServiceConstants.PRICE_STICKER_COMMON;
                case ShopChestRarity.Epic:
                    return ShopAppServiceConstants.PRICE_STICKER_EPIC;
                case ShopChestRarity.Legendary:
                    return ShopAppServiceConstants.PRICE_STICKER_LEGENDARY;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(rarity),
                        rarity,
                        "Unsupported rarity.");
            }
        }
    }
}
