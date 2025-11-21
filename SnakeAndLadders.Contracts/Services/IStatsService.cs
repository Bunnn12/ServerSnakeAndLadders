using System;
using System.Collections.Generic;
using System.ServiceModel;
using SnakeAndLadders.Contracts.Dtos;

namespace SnakeAndLadders.Contracts.Services
{
    [ServiceContract]
    public interface IStatsService
    {
        [OperationContract]
        IList<PlayerRankingItemDto> GetTopPlayersByCoins(int maxResults);

        [OperationContract]
        PlayerStatsDto GetPlayerStatsByUserId(int userId);
    }
}
