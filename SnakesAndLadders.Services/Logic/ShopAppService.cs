using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using log4net;
using ServerSnakesAndLadders.Common;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Enums;
using SnakeAndLadders.Contracts.Interfaces;

namespace SnakesAndLadders.Services.Logic
{
    public sealed class ShopAppService : IShopAppService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ShopAppService));

        private const string ERROR_INVALID_SESSION = "SHOP_INVALID_SESSION";
        private const string ERROR_NULL_RESULT = "SHOP_NULL_RESULT";
        private const string ERROR_NULL_DATA = "SHOP_NULL_DATA";

        private const int PRICE_AVATAR_COMMON = 200;
        private const int PRICE_AVATAR_EPIC = 400;
        private const int PRICE_AVATAR_LEGENDARY = 800;

        private const int PRICE_STICKER_COMMON = 150;
        private const int PRICE_STICKER_EPIC = 300;
        private const int PRICE_STICKER_LEGENDARY = 600;

        private const int PRICE_DICE_DEFAULT = 300;

        private const int PRICE_ITEM_CHEST = 500;

        private readonly IShopRepository shopRepository;
        private readonly Func<string, int> getUserIdFromToken;

        public ShopAppService(IShopRepository shopRepository, Func<string, int> getUserIdFromToken)
        {
            this.shopRepository = shopRepository ?? throw new ArgumentNullException(nameof(shopRepository));
            this.getUserIdFromToken = getUserIdFromToken ?? throw new ArgumentNullException(nameof(getUserIdFromToken));
        }

        public ShopRewardDto PurchaseAvatarChest(string token, ShopChestRarity rarity)
        {
            int userId = EnsureUser(token);
            int price = GetAvatarPrice(rarity);

            Logger.InfoFormat(
                "PurchaseAvatarChest. UserId={0}, Rarity={1}, Price={2}",
                userId,
                rarity,
                price);

            var request = new AvatarChestPurchaseDto
            {
                UserId = userId,
                Rarity = rarity,
                PriceCoins = price
            };

            OperationResult<ShopRewardDto> result = shopRepository.PurchaseAvatarChest(request);
            return EnsureSuccess(result);
        }

        public ShopRewardDto PurchaseStickerChest(string token, ShopChestRarity rarity)
        {
            int userId = EnsureUser(token);
            int price = GetStickerPrice(rarity);

            Logger.InfoFormat(
                "PurchaseStickerChest. UserId={0}, Rarity={1}, Price={2}",
                userId,
                rarity,
                price);

            var request = new StickerChestPurchaseDto
            {
                UserId = userId,
                Rarity = rarity,
                PriceCoins = price
            };

            OperationResult<ShopRewardDto> result = shopRepository.PurchaseStickerChest(request);
            return EnsureSuccess(result);
        }

        public ShopRewardDto PurchaseDice(string token, int diceId)
        {
            int userId = EnsureUser(token);

            Logger.InfoFormat(
                "PurchaseDice. UserId={0}, DiceId={1}, Price={2}",
                userId,
                diceId,
                PRICE_DICE_DEFAULT);

            var request = new DicePurchaseDto
            {
                UserId = userId,
                DiceId = diceId,
                PriceCoins = PRICE_DICE_DEFAULT
            };

            OperationResult<ShopRewardDto> result = shopRepository.PurchaseDice(request);
            return EnsureSuccess(result);
        }

        public ShopRewardDto PurchaseItemChest(string token)
        {
            int userId = EnsureUser(token);

            Logger.InfoFormat(
                "PurchaseItemChest. UserId={0}, Price={1}",
                userId,
                PRICE_ITEM_CHEST);

            var request = new ItemChestPurchaseDto
            {
                UserId = userId,
                PriceCoins = PRICE_ITEM_CHEST
            };

            OperationResult<ShopRewardDto> result = shopRepository.PurchaseItemChest(request);
            return EnsureSuccess(result);
        }

        private int EnsureUser(string token)
        {
            int userId = getUserIdFromToken(token);

            if (userId <= 0)
            {
                throw new FaultException(ERROR_INVALID_SESSION);
            }

            return userId;
        }

        private static ShopRewardDto EnsureSuccess(OperationResult<ShopRewardDto> result)
        {
            if (result == null)
            {
                throw new FaultException(ERROR_NULL_RESULT);
            }

            if (!result.IsSuccess)
            {
                string code = string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? ERROR_NULL_RESULT
                    : result.ErrorMessage;

                throw new FaultException(code);
            }

            if (result.Data == null)
            {
                throw new FaultException(ERROR_NULL_DATA);
            }

            return result.Data;
        }

        private static int GetAvatarPrice(ShopChestRarity rarity)
        {
            switch (rarity)
            {
                case ShopChestRarity.Common:
                    return PRICE_AVATAR_COMMON;
                case ShopChestRarity.Epic:
                    return PRICE_AVATAR_EPIC;
                case ShopChestRarity.Legendary:
                    return PRICE_AVATAR_LEGENDARY;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rarity), rarity, "Unsupported rarity.");
            }
        }

        private static int GetStickerPrice(ShopChestRarity rarity)
        {
            switch (rarity)
            {
                case ShopChestRarity.Common:
                    return PRICE_STICKER_COMMON;
                case ShopChestRarity.Epic:
                    return PRICE_STICKER_EPIC;
                case ShopChestRarity.Legendary:
                    return PRICE_STICKER_LEGENDARY;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rarity), rarity, "Unsupported rarity.");
            }
        }

        public int GetCurrentCoins(string token)
        {
            int userId = EnsureUser(token);

            Logger.InfoFormat("GetCurrentCoins. UserId={0}", userId);

            OperationResult<int> result = shopRepository.GetCurrentCoins(userId);

            if (result == null)
            {
                throw new FaultException(ERROR_NULL_RESULT);
            }

            if (!result.IsSuccess)
            {
                string code = string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? ERROR_NULL_RESULT
                    : result.ErrorMessage;

                throw new FaultException(code);
            }

            return result.Data;
        }
        public List<StickerDto> GetUserStickers(string token)
        {
            int userId = EnsureUser(token);

            Logger.InfoFormat("GetUserStickers. UserId={0}", userId);

            OperationResult<List<StickerDto>> result = shopRepository.GetUserStickers(userId);

            if (result == null)
            {
                throw new FaultException(ERROR_NULL_RESULT);
            }

            if (!result.IsSuccess)
            {
                string code = string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? ERROR_NULL_RESULT
                    : result.ErrorMessage;

                throw new FaultException(code);
            }

            if (result.Data == null)
            {
                throw new FaultException(ERROR_NULL_DATA);
            }

            return result.Data;
        }


    }
}
