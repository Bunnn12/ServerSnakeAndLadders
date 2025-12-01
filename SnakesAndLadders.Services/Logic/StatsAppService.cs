using System;
using System.Collections.Generic;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;

namespace SnakesAndLadders.Services.Logic
{
    public sealed class StatsAppService : IStatsAppService
    {
        private const int DEFAULT_MAX_RESULTS = 50;
        private const int DEFAULT_STATS_RANKING_MAX_RESULTS = 50;

        private readonly IStatsRepository statsRepository;

        public StatsAppService(IStatsRepository statsRepository)
        {
            this.statsRepository = statsRepository
                ?? throw new ArgumentNullException(nameof(statsRepository));
        }

        public IList<PlayerRankingItemDto> GetTopPlayersByCoins(int maxResults)
        {
            int effectiveMaxResults = maxResults <= 0
                ? DEFAULT_MAX_RESULTS
                : maxResults;

            return statsRepository.GetTopPlayersByCoins(effectiveMaxResults);
        }

        public PlayerStatsDto GetPlayerStatsByUserId(int userId)
        {
            if (userId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            return statsRepository.GetPlayerStatsByUserId(
                userId,
                DEFAULT_STATS_RANKING_MAX_RESULTS);
        }
    }
}
