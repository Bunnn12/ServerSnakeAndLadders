using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLadders.Contracts.Dtos
{
    public class PlayerRankingItemDto
    {
        public int UserId { get; set; }

        public string Username { get; set; }

        public int Coins { get; set; }
    }
}
