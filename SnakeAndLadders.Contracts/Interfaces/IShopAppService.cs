using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Enums;

namespace SnakeAndLadders.Contracts.Interfaces
{
    public interface IShopAppService
    {
        ShopRewardDto PurchaseAvatarChest(string token, ShopChestRarity rarity);
        ShopRewardDto PurchaseStickerChest(string token, ShopChestRarity rarity);
        ShopRewardDto PurchaseDice(string token, int diceId);
        ShopRewardDto PurchaseItemChest(string token);
        int GetCurrentCoins(string token);
        List<StickerDto> GetUserStickers(string token);
    }
}
