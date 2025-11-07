using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;

namespace SnakesAndLadders.Services.Logic
{
    public sealed class StatsAppService : IStatsAppService
    {
        private const int DEFAULT_MAX_RESULTS = 50;

        private readonly IStatsRepository statsRepository;

        public StatsAppService(IStatsRepository statsRepository)
        {
            this.statsRepository = statsRepository
                ?? throw new ArgumentNullException(nameof(statsRepository));
        }

        public IList<PlayerRankingItemDto> GetTopPlayersByCoins(int maxResults)
        {
            var effectiveMaxResults = maxResults <= 0
                ? DEFAULT_MAX_RESULTS
                : maxResults;

            return statsRepository.GetTopPlayersByCoins(effectiveMaxResults);
        }
    }
}
