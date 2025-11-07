using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
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

        private readonly IStatsAppService app;

        public StatsService(IStatsAppService app)
        {
            this.app = app ?? throw new ArgumentNullException(nameof(app));
        }

        public IList<PlayerRankingItemDto> GetTopPlayersByCoins(int maxResults)
        {
            try
            {
                return app.GetTopPlayersByCoins(maxResults);
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
    }
}
