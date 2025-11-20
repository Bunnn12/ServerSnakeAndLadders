using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Data;

namespace SnakesAndLadders.Data.Repositories
{
    /// <summary>
    /// Repository for statistics and ranking-related queries.
    /// </summary>
    public sealed class StatsRepository : IStatsRepository
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(StatsRepository));

        private const int MAX_ALLOWED_RESULTS = 100;
        private const int COMMAND_TIMEOUT_SECONDS = 30;

        /// <summary>
        /// Gets the top players ordered by coins (descending) and username (ascending).
        /// </summary>
        public IList<PlayerRankingItemDto> GetTopPlayersByCoins(int maxResults)
        {
            if (maxResults <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxResults),
                    "maxResults must be greater than zero.");
            }

            if (maxResults > MAX_ALLOWED_RESULTS)
            {
                maxResults = MAX_ALLOWED_RESULTS;
            }

            try
            {
                using (var dbContext = new SnakeAndLaddersDBEntities1())
                {
                    ((IObjectContextAdapter)dbContext).ObjectContext.CommandTimeout = COMMAND_TIMEOUT_SECONDS;

                    var query = dbContext.Usuario
                        .OrderByDescending(user => user.Monedas)
                        .ThenBy(user => user.NombreUsuario)
                        .Select(user => new PlayerRankingItemDto
                        {
                            UserId = user.IdUsuario,
                            Username = user.NombreUsuario,
                            Coins = user.Monedas
                        });

                    return query
                        .Take(maxResults)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error getting top players by coins.", ex);
                throw;
            }
        }
    }
}
