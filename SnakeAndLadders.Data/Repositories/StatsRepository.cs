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
 
    public sealed class StatsRepository : IStatsRepository
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(StatsRepository));

        private const int MAX_ALLOWED_RESULTS = 100;
        private const int COMMAND_TIMEOUT_SECONDS = 30;
        private const int DEFAULT_RANKING_MAX_RESULTS = 50;

        private const byte WINNER_FLAG = 0x01;
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

        public PlayerStatsDto GetPlayerStatsByUserId(int userId, int rankingMaxResults)
        {
            if (userId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            if (rankingMaxResults <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rankingMaxResults));
            }

            if (rankingMaxResults > MAX_ALLOWED_RESULTS)
            {
                rankingMaxResults = MAX_ALLOWED_RESULTS;
            }

            try
            {
                using (var dbContext = new SnakeAndLaddersDBEntities1())
                {
                    ((IObjectContextAdapter)dbContext).ObjectContext.CommandTimeout = COMMAND_TIMEOUT_SECONDS;

                    var userEntity = dbContext.Usuario
                        .AsNoTracking()
                        .SingleOrDefault(u => u.IdUsuario == userId);

                    if (userEntity == null)
                    {
                        return null;
                    }

                    int matchesPlayed = GetMatchesPlayed(dbContext, userId);
                    int matchesWon = GetMatchesWon(dbContext, userId);
                    decimal winPercentage = CalculateWinPercentage(matchesPlayed, matchesWon);
                    int? rankingPosition = GetRankingPosition(dbContext, userId, rankingMaxResults);

                    return new PlayerStatsDto
                    {
                        UserId = userEntity.IdUsuario,
                        Username = userEntity.NombreUsuario,
                        Coins = userEntity.Monedas,
                        MatchesPlayed = matchesPlayed,
                        MatchesWon = matchesWon,
                        WinPercentage = winPercentage,
                        RankingPosition = rankingPosition
                    };
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error getting player stats.", ex);
                throw;
            }
        }


        private static int GetMatchesPlayed(SnakeAndLaddersDBEntities1 dbContext, int userId)
        {
            if (dbContext == null)
            {
                throw new ArgumentNullException(nameof(dbContext));
            }

            return dbContext.UsuarioHasPartida
                .Count(link =>
                    link.UsuarioIdUsuario == userId &&
                    link.Partida != null &&
                    link.Partida.FechaTermino != null);
        }

        private static int GetMatchesWon(SnakeAndLaddersDBEntities1 dbContext, int userId)
        {
            if (dbContext == null)
            {
                throw new ArgumentNullException(nameof(dbContext));
            }

            var winnerFlags = dbContext.UsuarioHasPartida
                .Where(link =>
                    link.UsuarioIdUsuario == userId &&
                    link.Partida != null &&
                    link.Partida.FechaTermino != null)
                .Select(link => link.Ganador)
                .ToList();

            return winnerFlags.Count(IsWinnerFlagSet);
        }

        private static bool IsWinnerFlagSet(byte[] winnerFlag)
        {
            if (winnerFlag == null || winnerFlag.Length == 0)
            {
                return false;
            }

            return winnerFlag[0] == WINNER_FLAG;
        }

        private static decimal CalculateWinPercentage(int matchesPlayed, int matchesWon)
        {
            if (matchesPlayed <= 0 || matchesWon <= 0)
            {
                return 0m;
            }

            decimal winRatio = (decimal)matchesWon / matchesPlayed;
            return Math.Round(winRatio * 100m, 2);
        }

        private static int? GetRankingPosition(
            SnakeAndLaddersDBEntities1 dbContext,
            int userId,
            int rankingMaxResults)
        {
            if (dbContext == null)
            {
                throw new ArgumentNullException(nameof(dbContext));
            }

            int effectiveRankingMaxResults = rankingMaxResults <= 0
                ? DEFAULT_RANKING_MAX_RESULTS
                : rankingMaxResults;

            var orderedUserIds = dbContext.Usuario
                .AsNoTracking()
                .OrderByDescending(u => u.Monedas)
                .ThenBy(u => u.NombreUsuario)
                .Take(effectiveRankingMaxResults)
                .Select(u => u.IdUsuario)
                .ToList();

            int index = orderedUserIds.IndexOf(userId);

            if (index < 0)
            {
                return null;
            }

            return index + 1;
        }
    }
}
