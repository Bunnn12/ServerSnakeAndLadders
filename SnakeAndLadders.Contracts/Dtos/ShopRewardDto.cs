using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnakeAndLadders.Contracts.Enums;

namespace SnakeAndLadders.Contracts.Dtos
{
    public sealed class ShopRewardDto
    {
        public ShopRewardType RewardType { get; set; }
        public int RewardId { get; set; }
        public string RewardCode { get; set; } = string.Empty;
        public string RewardName { get; set; } = string.Empty;
        public bool IsNewForUser { get; set; }
        public int CoinsBefore { get; set; }
        public int CoinsAfter { get; set; }
    }
}
