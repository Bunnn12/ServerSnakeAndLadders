using System.ServiceModel;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Enums;

namespace SnakeAndLadders.Contracts.Services
{
    [ServiceContract]
    public interface IShopService
    {
        [OperationContract]
        ShopRewardDto PurchaseAvatarChest(string token, ShopChestRarity rarity);

        [OperationContract]
        ShopRewardDto PurchaseStickerChest(string token, ShopChestRarity rarity);

        [OperationContract]
        ShopRewardDto PurchaseDice(string token, int diceId);

        [OperationContract]
        ShopRewardDto PurchaseItemChest(string token);

        [OperationContract]
        int GetCurrentCoins(string token);
    }
}
