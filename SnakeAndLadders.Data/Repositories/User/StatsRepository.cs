using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Data.Constants;
using SnakesAndLadders.Data.Helpers;

namespace SnakesAndLadders.Data.Repositories
{
    public sealed class StatsRepository : IStatsRepository
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(StatsRepository));

        private readonly Func<SnakeAndLaddersDBEntities1> _contextFactory;

        public StatsRepository(Func<SnakeAndLaddersDBEntities1> contextFactory = null)
        {
            _contextFactory = contextFactory ?? (() => new SnakeAndLaddersDBEntities1());
        }

        public IList<PlayerRankingItemDto> GetTopPlayersByCoins(int maxResults)
        {
            int normalizedMaxResults = StatsRepositoryHelper.NormalizeMaxResults(maxResults);

            try
            {
                using (SnakeAndLaddersDBEntities1 dbContext = _contextFactory())
                {
                    StatsRepositoryHelper.ConfigureContext(dbContext);

                    IQueryable<PlayerRankingItemDto> query = dbContext.Usuario
                        .AsNoTracking()
                        .OrderByDescending(user => user.Monedas)
                        .ThenBy(user => user.NombreUsuario)
                        .Select(user => new PlayerRankingItemDto
                        {
                            UserId = user.IdUsuario,
                            Username = user.NombreUsuario,
                            Coins = user.Monedas
                        });

                    List<PlayerRankingItemDto> rankingItems = query
                        .Take(normalizedMaxResults)
                        .ToList();

                    return rankingItems;
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.Error(StatsRepositoryConstants.LOG_ERROR_GET_TOP_PLAYERS_BY_COINS, ex);
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(StatsRepositoryConstants.LOG_ERROR_GET_TOP_PLAYERS_BY_COINS, ex);
                throw;
            }
        }

        public PlayerStatsDto GetPlayerStatsByUserId(int userId, int rankingMaxResults)
        {
            StatsRepositoryHelper.ValidateUserId(
                userId,
                nameof(userId),
                StatsRepositoryConstants.ERROR_USER_ID_POSITIVE);

            int normalizedRankingMaxResults =
                StatsRepositoryHelper.NormalizeRankingMaxResults(rankingMaxResults);

            try
            {
                using (SnakeAndLaddersDBEntities1 dbContext = _contextFactory())
                {
                    StatsRepositoryHelper.ConfigureContext(dbContext);

                    Usuario userEntity = dbContext.Usuario
                        .AsNoTracking()
                        .SingleOrDefault(u => u.IdUsuario == userId);

                    if (userEntity == null)
                    {
                        return CreateDefaultPlayerStats(userId);
                    }

                    int matchesPlayed = GetMatchesPlayed(dbContext, userId);
                    int matchesWon = GetMatchesWon(dbContext, userId);
                    decimal winPercentage = StatsRepositoryHelper.CalculateWinPercentage(
                        matchesPlayed,
                        matchesWon);

                    int rankingPosition = GetRankingPosition(
                        dbContext,
                        userId,
                        normalizedRankingMaxResults);

                    var stats = new PlayerStatsDto
                    {
                        UserId = userEntity.IdUsuario,
                        Username = userEntity.NombreUsuario,
                        Coins = userEntity.Monedas,
                        MatchesPlayed = matchesPlayed,
                        MatchesWon = matchesWon,
                        WinPercentage = winPercentage,
                        RankingPosition = rankingPosition
                    };

                    return stats;
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.Error(StatsRepositoryConstants.LOG_ERROR_GET_PLAYER_STATS, ex);
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(StatsRepositoryConstants.LOG_ERROR_GET_PLAYER_STATS, ex);
                throw;
            }
        }

        // --------------------------------------------------------------------
        //  Helpers privados de consulta (dependen de DbContext)
        // --------------------------------------------------------------------

        private static int GetMatchesPlayed(SnakeAndLaddersDBEntities1 dbContext, int userId)
        {
            if (dbContext == null)
            {
                throw new ArgumentNullException(nameof(dbContext));
            }

            int matchesPlayed = dbContext.UsuarioHasPartida
                .Count(link =>
                    link.UsuarioIdUsuario == userId &&
                    link.Partida != null &&
                    link.Partida.FechaTermino != null);

            return matchesPlayed;
        }

        private static int GetMatchesWon(SnakeAndLaddersDBEntities1 dbContext, int userId)
        {
            if (dbContext == null)
            {
                throw new ArgumentNullException(nameof(dbContext));
            }

            List<byte[]> winnerFlags = dbContext.UsuarioHasPartida
                .Where(link =>
                    link.UsuarioIdUsuario == userId &&
                    link.Partida != null &&
                    link.Partida.FechaTermino != null)
                .Select(link => link.Ganador)
                .ToList();

            int matchesWon = winnerFlags.Count(StatsRepositoryHelper.IsWinnerFlagSet);

            return matchesWon;
        }

        private static int GetRankingPosition(
            SnakeAndLaddersDBEntities1 dbContext,
            int userId,
            int rankingMaxResults)
        {
            if (dbContext == null)
            {
                throw new ArgumentNullException(nameof(dbContext));
            }

            int effectiveRankingMaxResults = rankingMaxResults < StatsRepositoryConstants.MIN_RESULTS
                ? StatsRepositoryConstants.DEFAULT_RANKING_MAX_RESULTS
                : rankingMaxResults;

            List<int> orderedUserIds = dbContext.Usuario
                .AsNoTracking()
                .OrderByDescending(u => u.Monedas)
                .ThenBy(u => u.NombreUsuario)
                .Take(effectiveRankingMaxResults)
                .Select(u => u.IdUsuario)
                .ToList();

            int index = orderedUserIds.IndexOf(userId);

            if (index == StatsRepositoryConstants.INDEX_NOT_FOUND)
            {
                return StatsRepositoryConstants.RANKING_POSITION_NOT_AVAILABLE;
            }

            int rankingPosition = index + StatsRepositoryConstants.MIN_RESULTS;

            return rankingPosition;
        }

        private static PlayerStatsDto CreateDefaultPlayerStats(int userId)
        {
            var stats = new PlayerStatsDto
            {
                UserId = userId,
                Username = string.Empty,
                Coins = StatsRepositoryConstants.DEFAULT_COINS,
                MatchesPlayed = StatsRepositoryConstants.DEFAULT_MATCHES_PLAYED,
                MatchesWon = StatsRepositoryConstants.DEFAULT_MATCHES_WON,
                WinPercentage = StatsRepositoryConstants.DEFAULT_WIN_PERCENTAGE,
                RankingPosition = StatsRepositoryConstants.RANKING_POSITION_NOT_AVAILABLE
            };

            return stats;
        }
    }
}
