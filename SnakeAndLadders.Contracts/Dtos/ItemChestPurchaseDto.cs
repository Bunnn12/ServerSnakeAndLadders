using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLadders.Contracts.Dtos
{
    public sealed class ItemChestPurchaseDto
    {
        public int UserId { get; set; }
        public int PriceCoins { get; set; }
    }
}
