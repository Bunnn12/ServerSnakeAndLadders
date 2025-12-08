using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Data;

namespace SnakesAndLadders.Data.Repositories
{
    public sealed class StatsRepository : IStatsRepository
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(StatsRepository));

        private const int MAX_ALLOWED_RESULTS = 100;
        private const int DEFAULT_RANKING_MAX_RESULTS = 50;
        private const int MIN_RESULTS = 1;
        private const int MIN_VALID_USER_ID = 1;

        private const int INDEX_NOT_FOUND = -1;
        private const int RANKING_POSITION_NOT_AVAILABLE = 0;

        private const int WINNER_FLAG_MIN_LENGTH = 1;
        private const int WINNER_FLAG_INDEX = 0;

        private const byte WINNER_FLAG = 0x01;

        private const int COMMAND_TIMEOUT_SECONDS = 30;

        private const decimal DEFAULT_WIN_PERCENTAGE = 0m;
        private const decimal WIN_PERCENTAGE_FACTOR = 100m;
        private const int WIN_PERCENTAGE_DECIMALS = 2;

        private const string ERROR_MAX_RESULTS_POSITIVE = "maxResults must be greater than zero.";
        private const string ERROR_USER_ID_POSITIVE = "userId must be positive.";
        private const string ERROR_RANKING_MAX_RESULTS_POSITIVE = "rankingMaxResults must be greater than zero.";

        private const string LOG_ERROR_GET_TOP_PLAYERS_BY_COINS = "Error getting top players by coins.";
        private const string LOG_ERROR_GET_PLAYER_STATS = "Error getting player stats.";

        private readonly Func<SnakeAndLaddersDBEntities1> _contextFactory;

        public StatsRepository(Func<SnakeAndLaddersDBEntities1> contextFactory = null)
        {
            _contextFactory = contextFactory ?? (() => new SnakeAndLaddersDBEntities1());
        }

        public IList<PlayerRankingItemDto> GetTopPlayersByCoins(int maxResults)
        {
            if (maxResults < MIN_RESULTS)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxResults),
                    ERROR_MAX_RESULTS_POSITIVE);
            }

            if (maxResults > MAX_ALLOWED_RESULTS)
            {
                maxResults = MAX_ALLOWED_RESULTS;
            }

            try
            {
                using (SnakeAndLaddersDBEntities1 dbContext = _contextFactory())
                {
                    ConfigureContext(dbContext);

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
                        .Take(maxResults)
                        .ToList();

                    return rankingItems;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(LOG_ERROR_GET_TOP_PLAYERS_BY_COINS, ex);
                throw;
            }
        }

        public PlayerStatsDto GetPlayerStatsByUserId(int userId, int rankingMaxResults)
        {
            if (userId < MIN_VALID_USER_ID)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(userId),
                    ERROR_USER_ID_POSITIVE);
            }

            if (rankingMaxResults < MIN_RESULTS)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rankingMaxResults),
                    ERROR_RANKING_MAX_RESULTS_POSITIVE);
            }

            if (rankingMaxResults > MAX_ALLOWED_RESULTS)
            {
                rankingMaxResults = MAX_ALLOWED_RESULTS;
            }

            try
            {
                using (SnakeAndLaddersDBEntities1 dbContext = _contextFactory())
                {
                    ConfigureContext(dbContext);

                    Usuario userEntity = dbContext.Usuario
                        .AsNoTracking()
                        .SingleOrDefault(u => u.IdUsuario == userId);

                    if (userEntity == null)
                    {
                        return CreateDefaultPlayerStats(userId);
                    }

                    int matchesPlayed = GetMatchesPlayed(dbContext, userId);
                    int matchesWon = GetMatchesWon(dbContext, userId);
                    decimal winPercentage = CalculateWinPercentage(matchesPlayed, matchesWon);
                    int rankingPosition = GetRankingPosition(dbContext, userId, rankingMaxResults);

                    PlayerStatsDto stats = new PlayerStatsDto
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
            catch (Exception ex)
            {
                _logger.Error(LOG_ERROR_GET_PLAYER_STATS, ex);
                throw;
            }
        }

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

            int matchesWon = winnerFlags.Count(IsWinnerFlagSet);

            return matchesWon;
        }

        private static bool IsWinnerFlagSet(byte[] winnerFlag)
        {
            if (winnerFlag == null
                || winnerFlag.Length < WINNER_FLAG_MIN_LENGTH)
            {
                return false;
            }

            return winnerFlag[WINNER_FLAG_INDEX] == WINNER_FLAG;
        }

        private static decimal CalculateWinPercentage(int matchesPlayed, int matchesWon)
        {
            if (matchesPlayed < MIN_RESULTS || matchesWon < MIN_RESULTS)
            {
                return DEFAULT_WIN_PERCENTAGE;
            }

            decimal winRatio = (decimal)matchesWon / matchesPlayed;
            decimal percentage = Math.Round(
                winRatio * WIN_PERCENTAGE_FACTOR,
                WIN_PERCENTAGE_DECIMALS);

            return percentage;
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

            int effectiveRankingMaxResults = rankingMaxResults < MIN_RESULTS
                ? DEFAULT_RANKING_MAX_RESULTS
                : rankingMaxResults;

            List<int> orderedUserIds = dbContext.Usuario
                .AsNoTracking()
                .OrderByDescending(u => u.Monedas)
                .ThenBy(u => u.NombreUsuario)
                .Take(effectiveRankingMaxResults)
                .Select(u => u.IdUsuario)
                .ToList();

            int index = orderedUserIds.IndexOf(userId);

            if (index == INDEX_NOT_FOUND)
            {
                return RANKING_POSITION_NOT_AVAILABLE;
            }

            int rankingPosition = index + MIN_RESULTS;

            return rankingPosition;
        }

        private static PlayerStatsDto CreateDefaultPlayerStats(int userId)
        {
            PlayerStatsDto stats = new PlayerStatsDto
            {
                UserId = userId,
                Username = string.Empty,
                Coins = 0,
                MatchesPlayed = 0,
                MatchesWon = 0,
                WinPercentage = DEFAULT_WIN_PERCENTAGE,
                RankingPosition = RANKING_POSITION_NOT_AVAILABLE
            };

            return stats;
        }

        private static void ConfigureContext(SnakeAndLaddersDBEntities1 context)
        {
            ((IObjectContextAdapter)context).ObjectContext.CommandTimeout = COMMAND_TIMEOUT_SECONDS;
        }
    }
}
