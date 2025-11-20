using System;
using System.Collections.Generic;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;

namespace SnakesAndLadders.Services.Logic
{
    /// <summary>
    /// Application service for statistics and rankings.
    /// </summary>
    public sealed class StatsAppService : IStatsAppService
    {
        private const int DEFAULT_MAX_RESULTS = 50;

        private readonly IStatsRepository _statsRepository;

        public StatsAppService(IStatsRepository statsRepository)
        {
            _statsRepository = statsRepository
                ?? throw new ArgumentNullException(nameof(statsRepository));
        }

        public IList<PlayerRankingItemDto> GetTopPlayersByCoins(int maxResults)
        {
            int effectiveMaxResults = maxResults <= 0
                ? DEFAULT_MAX_RESULTS
                : maxResults;

            return _statsRepository.GetTopPlayersByCoins(effectiveMaxResults);
        }
    }
}
