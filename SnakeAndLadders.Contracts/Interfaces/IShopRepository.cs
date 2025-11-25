using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServerSnakesAndLadders.Common;
using SnakeAndLadders.Contracts.Dtos;

namespace SnakeAndLadders.Contracts.Interfaces
{
    public interface IShopRepository
    {
        OperationResult<ShopRewardDto> PurchaseAvatarChest(AvatarChestPurchaseDto request);
        OperationResult<ShopRewardDto> PurchaseStickerChest(StickerChestPurchaseDto request);
        OperationResult<ShopRewardDto> PurchaseDice(DicePurchaseDto request);
        OperationResult<ShopRewardDto> PurchaseItemChest(ItemChestPurchaseDto request);
        OperationResult<int> GetCurrentCoins(int userId);
    }
}
