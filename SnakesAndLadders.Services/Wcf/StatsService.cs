using System;
using System.Collections.Generic;
using System.ServiceModel;
using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakeAndLadders.Contracts.Services;

namespace SnakesAndLadders.Services.Wcf
{
    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.Single,
        ConcurrencyMode = ConcurrencyMode.Multiple,
        IncludeExceptionDetailInFaults = false)]
    public sealed class StatsService : IStatsService
    {
        private const string ERROR_VALIDATION = "VALIDATION_ERROR";
        private const string ERROR_UNEXPECTED = "UNEXPECTED_ERROR";

        private static readonly ILog Logger = LogManager.GetLogger(typeof(StatsService));

        private readonly IStatsAppService statsAppService;

        public StatsService(IStatsAppService statsAppService)
        {
            this.statsAppService = statsAppService
                ?? throw new ArgumentNullException(nameof(statsAppService));
        }

        public IList<PlayerRankingItemDto> GetTopPlayersByCoins(int maxResults)
        {
            try
            {
                return statsAppService.GetTopPlayersByCoins(maxResults);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Logger.Warn("Validation error in GetTopPlayersByCoins.", ex);
                throw new FaultException(ERROR_VALIDATION);
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error in GetTopPlayersByCoins.", ex);
                throw new FaultException(ERROR_UNEXPECTED);
            }
        }

        public PlayerStatsDto GetPlayerStatsByUserId(int userId)
        {
            try
            {
                return statsAppService.GetPlayerStatsByUserId(userId);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Logger.Warn("Validation error in GetPlayerStatsByUserId.", ex);
                throw new FaultException(ERROR_VALIDATION);
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error in GetPlayerStatsByUserId.", ex);
                throw new FaultException(ERROR_UNEXPECTED);
            }
        }
    }
}
