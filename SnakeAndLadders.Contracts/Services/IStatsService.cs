using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using SnakeAndLadders.Contracts.Dtos;

namespace SnakeAndLadders.Contracts.Services
{
    [ServiceContract]
    public interface IStatsService
    {
        [OperationContract]
        IList<PlayerRankingItemDto> GetTopPlayersByCoins(int maxResults);
    }
}
