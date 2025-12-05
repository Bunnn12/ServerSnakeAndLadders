using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Enums;
using SnakeAndLadders.Contracts.Interfaces;
using SnakeAndLadders.Contracts.Services;

namespace SnakesAndLadders.Services.Wcf
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public sealed class ShopService : IShopService
    {
        private readonly IShopAppService shopAppService;

        public ShopService(IShopAppService shopAppService)
        {
            this.shopAppService = shopAppService ?? throw new ArgumentNullException(nameof(shopAppService));
        }

        public ShopRewardDto PurchaseAvatarChest(string token, ShopChestRarity rarity)
        {
            return shopAppService.PurchaseAvatarChest(token, rarity);
        }

        public ShopRewardDto PurchaseStickerChest(string token, ShopChestRarity rarity)
        {
            return shopAppService.PurchaseStickerChest(token, rarity);
        }

        public ShopRewardDto PurchaseDice(string token, int diceId)
        {
            return shopAppService.PurchaseDice(token, diceId);
        }

        public ShopRewardDto PurchaseItemChest(string token)
        {
            return shopAppService.PurchaseItemChest(token);
        }
        public int GetCurrentCoins(string token)
        {
            return shopAppService.GetCurrentCoins(token);
        }
        public List<StickerDto> GetUserStickers(string token)
        {
            return shopAppService.GetUserStickers(token);
        }
    }
}
