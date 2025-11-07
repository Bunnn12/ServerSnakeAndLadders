using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnakeAndLadders.Contracts.Dtos;

namespace SnakeAndLadders.Contracts.Interfaces
{
    public interface IStatsRepository
    {
        IList<PlayerRankingItemDto> GetTopPlayersByCoins(int maxResults);
    }
}
