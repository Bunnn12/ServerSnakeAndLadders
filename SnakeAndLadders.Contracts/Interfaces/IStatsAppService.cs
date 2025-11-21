using System.Collections.Generic;
using SnakeAndLadders.Contracts.Dtos;

namespace SnakeAndLadders.Contracts.Interfaces
{
    public interface IStatsAppService
    {
        IList<PlayerRankingItemDto> GetTopPlayersByCoins(int maxResults);

        PlayerStatsDto GetPlayerStatsByUserId(int userId);
    }
}
