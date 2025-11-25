using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnakeAndLadders.Contracts.Enums;

namespace SnakeAndLadders.Contracts.Dtos
{
    public sealed class StickerChestPurchaseDto
    {
        public int UserId { get; set; }
        public ShopChestRarity Rarity { get; set; }
        public int PriceCoins { get; set; }
    }
}
