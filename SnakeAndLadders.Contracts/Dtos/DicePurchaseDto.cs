using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLadders.Contracts.Dtos
{
    public sealed class DicePurchaseDto
    {
        public int UserId { get; set; }
        public int DiceId { get; set; }
        public int PriceCoins { get; set; }
    }
}
